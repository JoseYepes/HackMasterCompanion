using HackMaster.Domain;
using HackMaster.Domain.Sync;
using HackMaster.Web.Account;

namespace HackMaster.Web.Tests.Account;

/// <summary>Sincronización con la cuenta (F006 US1, US2 y US4; contracts/api.md, contracts/ui.md).</summary>
public class AccountSyncTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeApi _api = new();
    private readonly FakeJs _js = new();
    private readonly AccountSession _session = new();
    private readonly BrowserStore _browser;
    private readonly IndexedStore _indexed;
    private readonly PlayerSuites _suites;
    private readonly PlayerCharacters _characters;
    private AccountUser? _user = new("cuenta-a", "Ana");
    private int _signIns;
    private int _signOuts;

    public AccountSyncTests()
    {
        _browser = new BrowserStore(_js, _session);
        _indexed = new IndexedStore(_js, _session);
        _suites = new PlayerSuites(TestData.Create(), _browser, _session);
        _characters = new PlayerCharacters(_indexed, _session);
    }

    private AccountSync NewSync(bool configured = true) => new(
        new AccountAuth(
            configured,
            () => Task.FromResult(_user),
            () => Task.FromResult<string?>("token-de-prueba"),
            () => _signIns++,
            () => _signOuts++),
        _api.Client(),
        _session,
        _browser,
        _indexed,
        _suites,
        _js,
        () => At);

    private static Character Pj(string id, string name, DateTimeOffset? updated = null) =>
        new() { Id = id, Name = name, CreatedAt = At, UpdatedAt = updated ?? At };

    private static string SuiteFile(string name, DateTimeOffset updated) =>
        StoredData.Write(new PlayerSuitesFile(StoredData.SuitesVersion, [new PlayerSuite { Slug = TextNormalizer.ToSlug(name), Name = name, CreatedAt = updated, UpdatedAt = updated }]));

    private async Task<SyncState> StateAsync() => SyncState.Read(await _browser.GetAsync("hackmaster.sync")).Value!;

    [Fact]
    public async Task Without_Configuration_There_Is_No_Session__F006_FR012()
    {
        var sync = NewSync(configured: false);

        await sync.InitializeAsync();

        Assert.Equal(AccountState.NotConfigured, sync.State);
        Assert.Null(_session.AccountId);
        Assert.Empty(_api.Requests);
    }

    [Fact]
    public async Task Signed_Out_Uses_The_Device_Data__F006_FR012()
    {
        _user = null;
        var sync = NewSync();

        await sync.InitializeAsync();

        Assert.Equal(AccountState.SignedOut, sync.State);
        Assert.Null(_session.AccountId);
    }

    [Fact]
    public async Task Signed_In_Shows_The_Name_And_Uses_The_Account_Keys__F006_FR001()
    {
        var sync = NewSync();

        await sync.InitializeAsync();

        Assert.Equal(AccountState.SignedIn, sync.State);
        Assert.Equal("Ana", sync.Name);
        Assert.Equal("cuenta-a", _session.AccountId);
        Assert.Equal("Bearer token-de-prueba", _api.LastAuthorization);
        Assert.Equal(SyncStatus.Synced, sync.Status);
    }

    [Fact]
    public async Task Not_Invited_Stays_Without_Session__F006_FR003()
    {
        _api.Invited = false;
        var sync = NewSync();

        await sync.InitializeAsync();

        Assert.Equal(AccountState.NotInvited, sync.State);
        Assert.Null(_session.AccountId);
    }

    [Fact]
    public async Task A_Local_Change_Goes_To_The_Account__F006_FR004()
    {
        var sync = NewSync();
        await sync.InitializeAsync();

        await _characters.SaveAsync(Pj("c1", "Brunilda"));
        await sync.WhenIdleAsync();

        var put = Assert.Single(_api.Puts);
        Assert.Equal("/api/data/character%3Ac1", put.RequestUri!.AbsolutePath);
        Assert.Contains("Brunilda", _api.Docs["character:c1"]["payload"]!.GetValue<string>());
        Assert.Equal(SyncStatus.Synced, sync.Status);
        Assert.Empty((await StateAsync()).Pending);
    }

    [Fact]
    public async Task Edits_And_Deletions_Use_The_Known_Version__F006_FR005()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        await _characters.SaveAsync(Pj("c1", "Brunilda"));
        await sync.WhenIdleAsync();

        await _characters.SaveAsync(Pj("c1", "Brunilda la Roja"));
        await sync.WhenIdleAsync();
        await _characters.DeleteAsync("c1");
        await sync.WhenIdleAsync();

        Assert.Equal(3, _api.Puts.Count());
        Assert.True(_api.Docs["character:c1"]["deleted"]!.GetValue<bool>());
        Assert.Equal(SyncStatus.Synced, sync.Status);
    }

    [Fact]
    public async Task Offline_Changes_Wait_And_Go_Up_When_The_Connection_Returns__F006_FR005()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        _api.Offline = true;

        await _characters.SaveAsync(Pj("c1", "Brunilda"));
        await sync.WhenIdleAsync();

        Assert.Equal(SyncStatus.Pending, sync.Status);
        Assert.Single((await StateAsync()).Pending);
        Assert.Single(_characters.Characters);

        _api.Offline = false;
        await sync.SyncNowAsync();

        Assert.Equal(SyncStatus.Synced, sync.Status);
        Assert.True(_api.Docs.ContainsKey("character:c1"));
        Assert.Empty((await StateAsync()).Pending);
    }

    [Fact]
    public async Task Service_Down_Shows_It_And_Loses_Nothing__F006_Edge_Unavailable()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        _api.Down = true;

        await _characters.SaveAsync(Pj("c1", "Brunilda"));
        await sync.WhenIdleAsync();

        Assert.Equal(SyncStatus.Failed, sync.Status);
        Assert.Single((await StateAsync()).Pending);
    }

    [Fact]
    public async Task Opening_Brings_The_Changes_From_Another_Device__F006_FR006()
    {
        _api.Seed("character:c9", SyncKinds.Character, StoredData.CharactersVersion, At,
            System.Text.Json.JsonSerializer.Serialize(Pj("c9", "Del móvil"), GameDataJson.Options));
        _api.Seed("suite:del-movil", SyncKinds.Suite, StoredData.SuitesVersion, At,
            System.Text.Json.JsonSerializer.Serialize(new PlayerSuite { Slug = "del-movil", Name = "Del móvil", CreatedAt = At, UpdatedAt = At }, GameDataJson.Options));
        var sync = NewSync();

        await sync.InitializeAsync();
        await _characters.LoadAsync();
        await _suites.LoadAsync();

        Assert.Equal(["Del móvil"], _characters.Characters.Select(c => c.Name));
        Assert.Equal(["Del móvil"], _suites.Suites.Select(s => s.Name));
        Assert.Empty(_api.Puts);
    }

    [Fact]
    public async Task Later_Opening_Asks_Only_For_What_Changed__F006_FR006()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        var since = (await StateAsync()).LastSync;

        await sync.SyncNowAsync();

        Assert.NotNull(since);
        Assert.Contains($"since={since!.Value.ToUnixTimeSeconds()}", _api.Requests.Last(r => r.RequestUri!.AbsolutePath == "/api/data").RequestUri!.Query);
    }

    [Fact]
    public async Task Conflict_Keeps_The_Newest_And_Warns__F006_FR007()
    {
        var sync = NewSync();
        var warnings = new List<string>();
        sync.Warning += warnings.Add;
        await sync.InitializeAsync();
        _api.Offline = true;
        await _characters.SaveAsync(Pj("c1", "Del ordenador"));
        await sync.WhenIdleAsync();
        _api.Offline = false;
        _api.Seed("character:c1", SyncKinds.Character, StoredData.CharactersVersion, At.AddMinutes(5),
            System.Text.Json.JsonSerializer.Serialize(Pj("c1", "Del móvil"), GameDataJson.Options));

        await sync.SyncNowAsync();
        await _characters.LoadAsync();

        Assert.Equal(["Del móvil"], _characters.Characters.Select(c => c.Name));
        Assert.Contains(warnings, w => w.Contains("Del móvil", StringComparison.Ordinal));
        Assert.Empty((await StateAsync()).Pending);
    }

    [Fact]
    public async Task Rejected_Push_Of_A_Newer_Local_Change_Is_Resent__F006_FR007()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        _api.Seed("character:c1", SyncKinds.Character, StoredData.CharactersVersion, At.AddMinutes(-5),
            System.Text.Json.JsonSerializer.Serialize(Pj("c1", "Vieja de la nube"), GameDataJson.Options));

        await _characters.SaveAsync(Pj("c1", "Nueva"));
        await sync.WhenIdleAsync();

        Assert.Contains("Nueva", _api.Docs["character:c1"]["payload"]!.GetValue<string>());
        Assert.Equal(SyncStatus.Synced, sync.Status);
    }

    [Fact]
    public async Task A_Save_During_A_Pull_Does_Not_Delete_What_The_Pull_Brings__F006_FR007()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        _api.Seed("suite:del-movil", SyncKinds.Suite, StoredData.SuitesVersion, At,
            System.Text.Json.JsonSerializer.Serialize(new PlayerSuite { Slug = "del-movil", Name = "Del movil", CreatedAt = At, UpdatedAt = At }, GameDataJson.Options));
        Task<SuiteSaveResult>? save = null;
        _api.DuringPull = () => save = _suites.SaveAsync("Del ordenador", [new SuiteComponentRef(ElementKind.Skill, "botanica"), new SuiteComponentRef(ElementKind.Skill, "zoologia")], [Ability.INT], null, At);

        await sync.SyncNowAsync();
        Assert.Null((await save!).Problem);
        await sync.WhenIdleAsync();
        await _suites.LoadAsync();

        Assert.False(_api.Docs["suite:del-movil"]["deleted"]!.GetValue<bool>());
        Assert.True(_api.Docs.ContainsKey("suite:del-ordenador"));
        Assert.Equal(["Del movil", "Del ordenador"], _suites.Suites.Select(s => s.Name).Order());
    }

    [Fact]
    public async Task Unreadable_Sync_State_Keeps_Local_Edits_And_Backs_Up_The_Old_One__F006_Edge_Unreadable()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        _api.Offline = true;
        await _characters.SaveAsync(Pj("c1", "Editada sin conexión", At.AddMinutes(10)));
        await sync.WhenIdleAsync();
        _api.Offline = false;
        _api.Seed("character:c1", SyncKinds.Character, StoredData.CharactersVersion, At,
            System.Text.Json.JsonSerializer.Serialize(Pj("c1", "Vieja de la nube"), GameDataJson.Options));
        _js.Local["hackmaster.u.cuenta-a.sync"] = "estropeado";

        var again = NewSync();
        await again.InitializeAsync();
        await again.WhenIdleAsync();
        await _characters.LoadAsync();

        Assert.Equal(["Editada sin conexión"], _characters.Characters.Select(c => c.Name));
        Assert.Contains("Editada sin conexión", _api.Docs["character:c1"]["payload"]!.GetValue<string>());
        Assert.Equal("estropeado", _js.Local["hackmaster.u.cuenta-a.sync.roto"]);
    }

    [Fact]
    public async Task Not_Invited_Shows_The_Account_Id_For_The_Admin__F006_FR003()
    {
        _api.Invited = false;
        var sync = NewSync();

        await sync.InitializeAsync();

        Assert.Equal("cuenta-a", sync.AccountId);
    }

    [Fact]
    public async Task Signing_Out_Goes_Back_To_The_Device_Data__F006_US1()
    {
        var sync = NewSync();
        await sync.InitializeAsync();

        await sync.SignOutAsync();

        Assert.Null(_session.AccountId);
        Assert.Equal(1, _signOuts);
    }

    [Fact]
    public async Task First_Session_Offers_To_Upload_The_Device_Data__F006_US4()
    {
        await _browser.SetAsync("hackmaster.suites", SuiteFile("Del dispositivo", At));
        var sync = NewSync();

        await sync.InitializeAsync();

        Assert.True(sync.NeedsDeviceChoice);
        Assert.False(sync.AccountHasData);
    }

    [Fact]
    public async Task Without_Device_Data_Nothing_Is_Asked__F006_US4()
    {
        var sync = NewSync();

        await sync.InitializeAsync();

        Assert.False(sync.NeedsDeviceChoice);
    }

    [Fact]
    public async Task Uploading_Adds_Device_Data_Keeping_The_Newest__F006_FR010()
    {
        await _browser.SetAsync("hackmaster.suites", SuiteFile("Del dispositivo", At));
        await _indexed.SetAsync("hackmaster.characters", StoredData.Write(new CharactersFile(StoredData.CharactersVersion, [Pj("c1", "Vieja del dispositivo", At.AddDays(-1)), Pj("c2", "Solo del dispositivo")])));
        _api.Seed("character:c1", SyncKinds.Character, StoredData.CharactersVersion, At,
            System.Text.Json.JsonSerializer.Serialize(Pj("c1", "De la cuenta"), GameDataJson.Options));
        var sync = NewSync();
        await sync.InitializeAsync();
        Assert.True(sync.AccountHasData);

        await sync.UploadDeviceDataAsync();
        await sync.WhenIdleAsync();
        await _characters.LoadAsync();
        await _suites.LoadAsync();

        Assert.Equal(["De la cuenta", "Solo del dispositivo"], _characters.Characters.Select(c => c.Name));
        Assert.Equal(["Del dispositivo"], _suites.Suites.Select(s => s.Name));
        Assert.True(_api.Docs.ContainsKey("suite:del-dispositivo"));
        Assert.False(sync.NeedsDeviceChoice);
    }

    [Fact]
    public async Task Keeping_Device_Data_Apart_Touches_Nothing_And_Is_Not_Asked_Again__F006_FR010()
    {
        await _browser.SetAsync("hackmaster.suites", SuiteFile("Del dispositivo", At));
        var sync = NewSync();
        await sync.InitializeAsync();

        await sync.KeepDeviceDataAsync();
        await sync.WhenIdleAsync();

        Assert.False(sync.NeedsDeviceChoice);
        Assert.Empty(_api.Puts);
        _session.SetAccount(null);
        await _suites.LoadAsync();
        Assert.Equal(["Del dispositivo"], _suites.Suites.Select(s => s.Name));

        var again = NewSync();
        await again.InitializeAsync();
        Assert.False(again.NeedsDeviceChoice);
    }

    [Fact]
    public async Task Theme_Change_Goes_To_The_Account_And_Comes_Back__F006_FR004()
    {
        var sync = NewSync();
        await sync.InitializeAsync();

        await sync.ThemeChangedAsync("dark");
        await sync.WhenIdleAsync();

        Assert.Contains("dark", _api.Docs["theme:main"]["payload"]!.GetValue<string>());

        _api.Seed("theme:main", SyncKinds.Theme, 1, At.AddHours(1), """{"schemaVersion":1,"mode":"light"}""");
        await sync.SyncNowAsync();
        Assert.Equal("light", _js.Theme);
    }

    [Fact]
    public async Task Export_Downloads_A_File_And_Delete_Clears_The_Cloud__F006_FR011()
    {
        var sync = NewSync();
        await sync.InitializeAsync();
        await _characters.SaveAsync(Pj("c1", "Brunilda"));
        await sync.WhenIdleAsync();

        Assert.Null(await sync.DownloadAccountDataAsync());
        Assert.Equal(["hackmaster-cuenta.json"], _js.Downloads);
        Assert.Null(await sync.DeleteAccountAsync());

        Assert.Empty(_api.Docs);
        Assert.Equal(1, _signOuts);
        _session.SetAccount("cuenta-a");
        await _characters.LoadAsync();
        Assert.Single(_characters.Characters);
    }

    [Fact]
    public async Task Admin_Manages_Invitations__F006_FR003()
    {
        _api.IsAdmin = true;
        var sync = NewSync();
        await sync.InitializeAsync();

        var list = await sync.ListInvitationsAsync();

        Assert.True(sync.IsAdmin);
        Assert.Equal(["bo@pruebas.example"], list!.Select(i => i.Email));
        Assert.Null(await sync.InviteAsync("cid@pruebas.example"));
        Assert.Null(await sync.UninviteAsync("bo@pruebas.example"));
    }
}
