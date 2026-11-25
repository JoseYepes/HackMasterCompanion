using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HackMaster.Domain.Sync;
using Microsoft.JSInterop;

namespace HackMaster.Web.Account;

/// <summary>Cuenta con la que se ha iniciado sesión: su identificador (<c>oid</c>) y su nombre.</summary>
public sealed record AccountUser(string AccountId, string Name);

/// <summary>
/// Lo que la sincronización necesita del inicio de sesión (MSAL en la aplicación, un falso en los
/// tests): si está configurado, quién ha iniciado sesión, el token de acceso y entrar o salir.
/// </summary>
public sealed record AccountAuth(
    bool IsConfigured,
    Func<Task<AccountUser?>> CurrentUserAsync,
    Func<Task<string?>> AccessTokenAsync,
    Action SignIn,
    Action SignOut,
    Action<Action>? OnUserChanged = null)
{
    public static AccountAuth NotConfigured { get; } =
        new(false, () => Task.FromResult<AccountUser?>(null), () => Task.FromResult<string?>(null), () => { }, () => { });
}

/// <summary>Estado de la sesión (contracts/ui.md).</summary>
public enum AccountState
{
    /// <summary>La aplicación no tiene configurado el inicio de sesión: funciona como antes de F006.</summary>
    NotConfigured,
    SignedOut,
    Connecting,
    SignedIn,

    /// <summary>La cuenta no está invitada (FR-003): se sigue sin sesión.</summary>
    NotInvited,
}

/// <summary>Estado de la sincronización (contracts/ui.md).</summary>
public enum SyncStatus
{
    Synced,

    /// <summary>Hay cambios guardados en el dispositivo que aún no han llegado a la cuenta (sin conexión).</summary>
    Pending,

    /// <summary>El servicio no responde o la sesión ha caducado; nada se pierde.</summary>
    Failed,
}

/// <summary>Una invitación, para la página de administración.</summary>
public sealed record InvitationInfo(string Email, DateTimeOffset InvitedAt);

/// <summary>Sesión y sincronización con la cuenta (F006; ADR 0001). La usa la interfaz; los tests usan un falso.</summary>
public interface IAccountSync
{
    AccountState State { get; }

    SyncStatus Status { get; }

    string? Name { get; }

    /// <summary>Identificador de la cuenta con la que se ha entrado (también sin invitación, para dárselo al administrador).</summary>
    string? AccountId { get; }

    bool IsAdmin { get; }

    /// <summary>Primera sesión en este dispositivo con datos sin sesión: hay que preguntar qué hacer (US4).</summary>
    bool NeedsDeviceChoice { get; }

    /// <summary>La cuenta ya tenía datos al iniciar sesión.</summary>
    bool AccountHasData { get; }

    event Action? Changed;

    event Action<string>? Warning;

    Task InitializeAsync();

    /// <summary>Reintenta cada poco lo pendiente mientras no esté todo sincronizado (FR-005).</summary>
    void StartRetries();

    Task SignInAsync();

    Task SignOutAsync();

    Task SyncNowAsync();

    Task ThemeChangedAsync(string mode);

    Task UploadDeviceDataAsync();

    Task KeepDeviceDataAsync();

    Task<string?> DownloadAccountDataAsync();

    Task<string?> DeleteAccountAsync();

    Task<IReadOnlyList<InvitationInfo>?> ListInvitationsAsync();

    Task<string?> InviteAsync(string email);

    Task<string?> UninviteAsync(string email);
}

/// <summary>
/// Sincroniza los datos del dispositivo con la cuenta (research.md R4): cada cambio se guarda
/// primero en el dispositivo, queda pendiente y se sube; al abrir se traen los cambios de la nube.
/// Las reglas de fusión están en <see cref="SyncMerge"/> y <see cref="LocalDocs"/>.
/// </summary>
public sealed class AccountSync : IAccountSync, IDisposable
{
    private const string SyncKey = "hackmaster.sync";
    private const string Unreachable = "No se ha podido conectar con la cuenta. Inténtalo más tarde.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AccountAuth _auth;
    private readonly HttpClient _http;
    private readonly AccountSession _session;
    private readonly BrowserStore _browser;
    private readonly IndexedStore _indexed;
    private readonly PlayerSuites _suites;
    private readonly IJSRuntime _js;
    private readonly Func<DateTimeOffset> _now;
    private readonly Dictionary<string, string?> _snapshot = new(StringComparer.Ordinal);

    private SyncState _state = SyncState.Empty;
    private Task _tail = Task.CompletedTask;
    private bool _online;
    private CancellationTokenSource? _retries;

    public AccountSync(
        AccountAuth auth,
        HttpClient http,
        AccountSession session,
        BrowserStore browser,
        IndexedStore indexed,
        PlayerSuites suites,
        IJSRuntime js,
        Func<DateTimeOffset>? now = null)
    {
        _auth = auth;
        _http = http;
        _session = session;
        _browser = browser;
        _indexed = indexed;
        _suites = suites;
        _js = js;
        _now = now ?? (() => DateTimeOffset.UtcNow);
        State = auth.IsConfigured ? AccountState.SignedOut : AccountState.NotConfigured;
        _browser.Written += OnWrittenAsync;
        _indexed.Written += OnWrittenAsync;
        auth.OnUserChanged?.Invoke(() => _ = InitializeAsync());
    }

    public AccountState State { get; private set; } = AccountState.SignedOut;

    public SyncStatus Status { get; private set; } = SyncStatus.Synced;

    public string? Name { get; private set; }

    public string? AccountId { get; private set; }

    public bool IsAdmin { get; private set; }

    public bool NeedsDeviceChoice { get; private set; }

    public bool AccountHasData { get; private set; }

    public event Action? Changed;

    public event Action<string>? Warning;

    public async Task InitializeAsync()
    {
        if (!_auth.IsConfigured)
        {
            State = AccountState.NotConfigured;
            _session.SetAccount(null);
            RaiseChanged();
            return;
        }

        var user = await _auth.CurrentUserAsync();
        if (user is null || !AccountKeys.IsValidAccountId(user.AccountId))
        {
            State = AccountState.SignedOut;
            Name = null;
            AccountId = null;
            IsAdmin = false;
            _session.SetAccount(null);
            RaiseChanged();
            return;
        }

        // La cuenta se fija antes de preguntar a la nube: así no se ven un instante los datos sin sesión.
        State = AccountState.Connecting;
        Name = user.Name;
        AccountId = user.AccountId;
        _session.SetAccount(user.AccountId);
        RaiseChanged();

        var me = await SendAsync(HttpMethod.Get, "api/me");
        if (me.Status == HttpStatusCode.Forbidden)
        {
            State = AccountState.NotInvited;
            _session.SetAccount(null);
            RaiseChanged();
            return;
        }

        if (me.Status == HttpStatusCode.OK && me.Json is { } info)
        {
            Name = info.TryGetProperty("name", out var name) ? name.GetString() ?? user.Name : user.Name;
            IsAdmin = info.TryGetProperty("isAdmin", out var admin) && admin.GetBoolean();
        }

        State = AccountState.SignedIn;
        await Enqueue(LoadLocalAsync);
        _online = me.Status == HttpStatusCode.OK;
        if (!_online)
        {
            SetStatus(me.Status is null ? SyncStatus.Pending : SyncStatus.Failed);
            RaiseChanged();
            return;
        }

        await Enqueue(PullAsync);
        NeedsDeviceChoice = !_state.DeviceChoiceMade && await DeviceHasDataAsync();
        AccountHasData = _state.Known.Count > 0;
        await Enqueue(PushAsync);
        RaiseChanged();
    }

    public void StartRetries()
    {
        if (_retries is not null)
        {
            return;
        }

        _retries = new CancellationTokenSource();
        _ = RetryLoopAsync(_retries.Token);
    }

    private async Task RetryLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(token))
        {
            if (State == AccountState.SignedIn && Status != SyncStatus.Synced)
            {
                await SyncNowAsync();
            }
        }
    }

    public Task SignInAsync()
    {
        _auth.SignIn();
        return Task.CompletedTask;
    }

    public async Task SignOutAsync()
    {
        await WhenIdleAsync();
        _session.SetAccount(null);
        State = AccountState.SignedOut;
        Name = null;
        AccountId = null;
        IsAdmin = false;
        NeedsDeviceChoice = false;
        _snapshot.Clear();
        RaiseChanged();
        _auth.SignOut();
    }

    public async Task SyncNowAsync()
    {
        if (State != AccountState.SignedIn)
        {
            return;
        }

        await Enqueue(PullAsync);
        await Enqueue(PushAsync);
    }

    /// <summary>Espera a que terminen las subidas y lecturas en curso (para los tests y al cerrar sesión).</summary>
    public async Task WhenIdleAsync()
    {
        Task tail;
        do
        {
            tail = _tail;
            await tail;
        }
        while (tail != _tail);
    }

    public Task ThemeChangedAsync(string mode) =>
        _session.IsSynced(LocalDocs.ThemeKey) ? OnWrittenAsync(LocalDocs.ThemeKey, ThemeJson(mode)) : Task.CompletedTask;

    public async Task UploadDeviceDataAsync()
    {
        await WhenIdleAsync();
        foreach (var key in new[] { LocalDocs.CharactersKey, LocalDocs.SuitesKey })
        {
            await _session.DataGate.WaitAsync();
            try
            {
                var now = _now();
                var device = LocalDocs.Split(key, await ReadDeviceAsync(key), now);
                var accountJson = await ReadLocalAsync(key);
                var account = LocalDocs.Split(key, accountJson, now);
                if (device is null || account is null)
                {
                    RaiseWarning("Algunos datos no se han podido leer y no se han subido.");
                    continue;
                }

                var existing = account.ToDictionary(d => d.Id, StringComparer.Ordinal);
                var include = device.Where(d => !existing.TryGetValue(d.Id, out var mine) || d.UpdatedAt > mine.UpdatedAt).ToList();
                if (include.Count > 0)
                {
                    await WriteSyncedAsync(key, LocalDocs.Apply(key, accountJson, include)!);
                }
            }
            finally
            {
                _session.DataGate.Release();
            }
        }

        _suites.Invalidate();

        if (await ReadLocalAsync(LocalDocs.RollsKey) is null && await ReadDeviceAsync(LocalDocs.RollsKey) is { } rolls)
        {
            await WriteSyncedAsync(LocalDocs.RollsKey, rolls);
        }

        await KeepDeviceDataAsync();
    }

    public async Task KeepDeviceDataAsync()
    {
        await Enqueue(async () =>
        {
            _state = _state with { DeviceChoiceMade = true };
            await SaveStateAsync();
        });
        NeedsDeviceChoice = false;
        RaiseChanged();
    }

    public async Task<string?> DownloadAccountDataAsync()
    {
        var result = await SendAsync(HttpMethod.Get, "api/export");
        if (result.Status != HttpStatusCode.OK || result.Body is null)
        {
            return Unreachable;
        }

        await _indexed.DownloadAsync("hackmaster-cuenta.json", result.Body);
        return null;
    }

    public async Task<string?> DeleteAccountAsync()
    {
        await WhenIdleAsync();
        var result = await SendAsync(HttpMethod.Delete, "api/account");
        if (result.Status != HttpStatusCode.NoContent)
        {
            return Unreachable;
        }

        // La nube ya no tiene nada: se olvidan sus versiones. La copia del dispositivo se queda.
        await Enqueue(async () =>
        {
            _state = SyncState.Empty with { DeviceChoiceMade = true };
            await SaveStateAsync();
        });
        await SignOutAsync();
        return null;
    }

    public async Task<IReadOnlyList<InvitationInfo>?> ListInvitationsAsync()
    {
        var result = await SendAsync(HttpMethod.Get, "api/invitations");
        return result.Status == HttpStatusCode.OK && result.Body is not null
            ? JsonSerializer.Deserialize<List<InvitationInfo>>(result.Body, JsonOptions)
            : null;
    }

    public async Task<string?> InviteAsync(string email)
    {
        var result = await SendAsync(HttpMethod.Put, "api/invitations/" + Uri.EscapeDataString(email.Trim()));
        return result.Status switch
        {
            HttpStatusCode.NoContent => null,
            HttpStatusCode.BadRequest => "Ese correo no es válido.",
            _ => Unreachable,
        };
    }

    public async Task<string?> UninviteAsync(string email)
    {
        var result = await SendAsync(HttpMethod.Delete, "api/invitations/" + Uri.EscapeDataString(email));
        return result.Status == HttpStatusCode.NoContent ? null : Unreachable;
    }

    public void Dispose()
    {
        _retries?.Cancel();
        _retries?.Dispose();
        _browser.Written -= OnWrittenAsync;
        _indexed.Written -= OnWrittenAsync;
    }

    /// <summary>Un archivo del dispositivo ha cambiado: sus cambios quedan pendientes y se suben.</summary>
    /// <remarks>
    /// Los cambios se calculan en el momento, sin esperar a la cola: así no se comparan con lo que
    /// haya llegado de la nube entretanto, y guardar nunca espera a la red.
    /// </remarks>
    private Task OnWrittenAsync(string key, string json)
    {
        if (State != AccountState.SignedIn)
        {
            return Task.CompletedTask;
        }

        foreach (var change in LocalDocs.Changes(key, _snapshot.GetValueOrDefault(key), json, _now()))
        {
            _state = _state.WithPending(change);
        }

        _snapshot[key] = json;
        _ = Enqueue(SaveStateAsync);
        _ = Enqueue(PushAsync);
        return Task.CompletedTask;
    }

    private async Task LoadLocalAsync()
    {
        var raw = await _browser.GetAsync(SyncKey);
        var read = SyncState.Read(raw);
        _state = read.Value ?? SyncState.Empty with { DeviceChoiceMade = true };
        _snapshot.Clear();
        foreach (var key in LocalDocs.SyncedKeys)
        {
            _snapshot[key] = await ReadLocalAsync(key);
        }

        if (read.Error is not null)
        {
            // Se guarda una copia del estado estropeado y todo lo del dispositivo pasa a pendiente:
            // al leer la nube, cada elemento se resuelve como un conflicto (gana el más reciente) en
            // vez de sustituir sin más lo que se cambió aquí.
            await _browser.SetLocalAsync(SyncKey + ".roto", raw!);
            foreach (var key in new[] { LocalDocs.CharactersKey, LocalDocs.SuitesKey, LocalDocs.RollsKey })
            {
                foreach (var doc in LocalDocs.Split(key, _snapshot[key], _now()) ?? [])
                {
                    _state = _state.WithPending(doc);
                }
            }

            RaiseWarning("No se ha podido leer el estado de la sincronización; se ha guardado una copia y no se ha borrado nada.");
        }
    }

    private async Task PullAsync()
    {
        var since = _state.LastSync?.ToUnixTimeSeconds();
        var result = await SendAsync(HttpMethod.Get, since is null ? "api/data" : $"api/data?since={since}");
        if (result.Status != HttpStatusCode.OK || result.Json is not { } body)
        {
            Fail(result.Status);
            return;
        }

        var incoming = body.GetProperty("items").Deserialize<List<DocWire>>(JsonOptions)!.Select(d => d.ToDoc()).ToList();
        var merge = SyncMerge.ApplyIncoming(incoming, _state);
        _state = merge.State with { LastSync = DateTimeOffset.FromUnixTimeSeconds(body.GetProperty("serverTime").GetInt64()) };
        await ApplyLocalAsync(merge.Apply);
        await SaveStateAsync();
        foreach (var warning in merge.Warnings)
        {
            RaiseWarning(warning);
        }

        _online = true;
        SetStatus(_state.Pending.Count > 0 ? SyncStatus.Pending : SyncStatus.Synced);
    }

    private async Task PushAsync()
    {
        if (State != AccountState.SignedIn)
        {
            return;
        }

        var attempts = 0;
        while (_state.Pending.Count > 0 && attempts++ < 3 * (_state.Pending.Count + 1))
        {
            var change = _state.Pending[0];
            var result = await SendAsync(HttpMethod.Put, "api/data/" + Uri.EscapeDataString(change.Id), DocWire.From(change), change.ETag);
            if (result.Status == HttpStatusCode.OK && result.Json is { } saved)
            {
                _state = _state.Sent(saved.Deserialize<DocWire>(JsonOptions)!.ToDoc());
            }
            else if (result.Status == HttpStatusCode.Conflict)
            {
                await ResolveConflictAsync(change, result.Json);
            }
            else if (result.Status is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge)
            {
                RaiseWarning($"No se ha podido guardar «{change.Label}» en la cuenta; sigue en este dispositivo.");
                _state = _state.WithoutPending(change.Id);
            }
            else
            {
                await SaveStateAsync();
                Fail(result.Status);
                return;
            }

            await SaveStateAsync();
        }

        _online = true;
        SetStatus(_state.Pending.Count > 0 ? SyncStatus.Pending : SyncStatus.Synced);
    }

    private async Task ResolveConflictAsync(SyncDoc change, JsonElement? body)
    {
        if (body is not { } json || !json.TryGetProperty("id", out _))
        {
            // La nube no tiene ese documento (se borró la cuenta, por ejemplo): se crea de nuevo.
            _state = _state with { Pending = [.. _state.Pending.Select(p => p.Id == change.Id ? p with { ETag = null } : p)] };
            return;
        }

        var remote = json.Deserialize<DocWire>(JsonOptions)!.ToDoc();
        var conflict = SyncMerge.Resolve(change, remote);
        RaiseWarning(conflict.Warning);
        _state = _state.WithKnown(remote.Id, remote.ETag!);
        if (conflict.LocalWins)
        {
            _state = _state with { Pending = [.. _state.Pending.Select(p => p.Id == change.Id ? conflict.Winner : p)] };
        }
        else
        {
            _state = _state.WithoutPending(change.Id);
            await ApplyLocalAsync([remote]);
        }
    }

    /// <summary>Guarda en el dispositivo lo que llega de la nube, sin volver a subirlo.</summary>
    private async Task ApplyLocalAsync(IReadOnlyList<SyncDoc> docs)
    {
        foreach (var group in docs.GroupBy(d => LocalDocs.KeyForKind(d.Kind)))
        {
            if (group.Key is not { } key)
            {
                continue;
            }

            var gated = key is LocalDocs.CharactersKey or LocalDocs.SuitesKey;
            if (gated)
            {
                await _session.DataGate.WaitAsync();
            }

            try
            {
                await ApplyKeyAsync(key, group);
            }
            finally
            {
                if (gated)
                {
                    _session.DataGate.Release();
                }
            }
        }

        _suites.Invalidate();
    }

    private async Task ApplyKeyAsync(string key, IEnumerable<SyncDoc> docs)
    {
        {
            var current = await ReadLocalAsync(key);
            var updated = LocalDocs.Apply(key, current, docs);
            if (updated is null || updated == current)
            {
                return;
            }

            if (key == LocalDocs.ThemeKey)
            {
                var mode = JsonDocument.Parse(updated).RootElement.GetProperty("mode").GetString();
                try
                {
                    await _js.InvokeVoidAsync("hackmasterTheme.set", mode);
                }
                catch (JSException)
                {
                    // Sin el script del modo, el modo no cambia; el resto sigue.
                }
            }
            else if (key == LocalDocs.CharactersKey)
            {
                await _indexed.SetLocalAsync(key, updated);
            }
            else
            {
                await _browser.SetLocalAsync(key, updated);
            }

            _snapshot[key] = updated;
        }
    }

    private Task WriteSyncedAsync(string key, string json) =>
        key == LocalDocs.CharactersKey ? _indexed.SetAsync(key, json) : _browser.SetAsync(key, json);

    private async Task<string?> ReadLocalAsync(string key)
    {
        if (key == LocalDocs.ThemeKey)
        {
            try
            {
                return ThemeJson(await _js.InvokeAsync<string>("hackmasterTheme.current"));
            }
            catch (JSException)
            {
                return null;
            }
        }

        return key == LocalDocs.CharactersKey ? (await _indexed.GetAsync(key)).Value : await _browser.GetAsync(key);
    }

    private async Task<string?> ReadDeviceAsync(string key) =>
        key == LocalDocs.CharactersKey ? (await _indexed.GetDeviceAsync(key)).Value : await _browser.GetDeviceAsync(key);

    private async Task<bool> DeviceHasDataAsync()
    {
        var now = _now();
        foreach (var key in new[] { LocalDocs.CharactersKey, LocalDocs.SuitesKey })
        {
            if (LocalDocs.Split(key, await ReadDeviceAsync(key), now) is { Count: > 0 })
            {
                return true;
            }
        }

        return false;
    }

    private Task SaveStateAsync() => _browser.SetLocalAsync(SyncKey, SyncState.Write(_state));

    private static string ThemeJson(string? mode) => $$"""{"schemaVersion":1,"mode":"{{(mode == "dark" ? "dark" : "light")}}"}""";

    private Task Enqueue(Func<Task> work)
    {
        _tail = ChainAsync(_tail, work);
        return _tail;
    }

    private static async Task ChainAsync(Task previous, Func<Task> work)
    {
        try
        {
            await previous;
        }
        catch (Exception)
        {
            // Un fallo anterior ya se ha avisado; no impide lo siguiente.
        }

        await work();
    }

    private void Fail(HttpStatusCode? status) => SetStatus(status is null ? SyncStatus.Pending : SyncStatus.Failed);

    private void SetStatus(SyncStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke();

    private void RaiseWarning(string text) => Warning?.Invoke(text);

    private async Task<ApiResponse> SendAsync(HttpMethod method, string path, DocWire? body = null, string? etag = null)
    {
        try
        {
            var token = await _auth.AccessTokenAsync();
            if (token is null)
            {
                return new(HttpStatusCode.Unauthorized, null, null);
            }

            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation(etag is null ? "If-None-Match" : "If-Match", etag ?? "*");
            }

            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            JsonElement? json = null;
            if (text.Length > 0 && response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                try
                {
                    json = JsonDocument.Parse(text).RootElement.Clone();
                }
                catch (JsonException)
                {
                    json = null;
                }
            }

            return new(response.StatusCode, text, json);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JSException or InvalidOperationException)
        {
            // Sin conexión, o no se pudo obtener el token: se reintenta más tarde.
            return new(null, null, null);
        }
    }

    private sealed record ApiResponse(HttpStatusCode? Status, string? Body, JsonElement? Json);

    /// <summary>Documento tal como viaja por la API (contracts/api.md).</summary>
    private sealed record DocWire(
        string Id,
        string Kind,
        int SchemaVersion,
        DateTimeOffset UpdatedAt,
        bool Deleted,
        string? Payload,
        [property: JsonPropertyName("etag")] string? ETag)
    {
        public static DocWire From(SyncDoc doc) => new(doc.Id, doc.Kind, doc.SchemaVersion, doc.UpdatedAt, doc.Deleted, doc.Payload, null);

        public SyncDoc ToDoc() => new(Id, Kind, SchemaVersion, UpdatedAt, Deleted, Payload, ETag);
    }
}
