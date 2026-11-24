using System.Text.Json;

namespace HackMaster.Domain.Sync;

/// <summary>
/// Lo que el dispositivo sabe de la cuenta (data-model.md de F006): la versión de la nube de cada
/// documento que ha visto, los cambios que aún no han llegado a la nube, la hora de la nube de la
/// última lectura y si ya se eligió qué hacer con los datos del dispositivo (US4).
/// </summary>
public sealed record SyncState(
    int SchemaVersion,
    IReadOnlyDictionary<string, string> Known,
    IReadOnlyList<SyncDoc> Pending,
    DateTimeOffset? LastSync = null,
    bool DeviceChoiceMade = false)
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static SyncState Empty { get; } = new(CurrentVersion, new Dictionary<string, string>(), []);

    /// <summary>Anota la versión de la nube de un documento.</summary>
    public SyncState WithKnown(string id, string etag) =>
        this with { Known = new Dictionary<string, string>(Known) { [id] = etag } };

    /// <summary>Pone un cambio en cola, basado en la versión conocida; sustituye al anterior del mismo documento.</summary>
    public SyncState WithPending(SyncDoc change)
    {
        var based = change with { ETag = Known.TryGetValue(change.Id, out var etag) ? etag : null };
        return this with { Pending = [.. Pending.Where(p => p.Id != change.Id), based] };
    }

    /// <summary>
    /// La nube ha guardado <paramref name="saved"/> (con su nueva versión). Sale de la cola salvo
    /// que entretanto haya un cambio más reciente, que se queda basado en la nueva versión.
    /// </summary>
    public SyncState Sent(SyncDoc saved)
    {
        var known = saved.ETag is null ? this : WithKnown(saved.Id, saved.ETag);
        var pending = known.Pending
            .Where(p => p.Id != saved.Id || p.UpdatedAt > saved.UpdatedAt)
            .Select(p => p.Id == saved.Id ? p with { ETag = saved.ETag } : p)
            .ToList();
        return known with { Pending = pending };
    }

    /// <summary>Quita un cambio de la cola sin tocar la versión conocida.</summary>
    public SyncState WithoutPending(string id) => this with { Pending = [.. Pending.Where(p => p.Id != id)] };

    public static string Write(SyncState state) =>
        JsonSerializer.Serialize(state with { SchemaVersion = CurrentVersion }, JsonOptions);

    /// <summary>Lee el estado. Si no se puede leer se informa y no se devuelve uno vacío, para no perder la cola.</summary>
    public static StoredRead<SyncState> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new(Empty, null);
        }

        try
        {
            var state = JsonSerializer.Deserialize<SyncState>(json, JsonOptions);
            if (state is null || state.Known is null || state.Pending is null)
            {
                return new(null, "El estado de sincronización está incompleto.");
            }

            if (state.SchemaVersion != CurrentVersion)
            {
                return new(null, $"El estado de sincronización es de una versión desconocida ({state.SchemaVersion}).");
            }

            return new(state, null);
        }
        catch (JsonException ex)
        {
            return new(null, $"El estado de sincronización no se puede leer: {ex.Message}");
        }
    }
}
