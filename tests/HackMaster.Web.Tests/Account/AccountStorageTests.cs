using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Account;
using Microsoft.JSInterop;

namespace HackMaster.Web.Tests.Account;

/// <summary>Cada cuenta guarda en sus propias claves del dispositivo (F006 US3: FR-009, FR-012).</summary>
public class AccountStorageTests : BunitContext
{
    private static readonly DateTimeOffset At = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly AccountSession _session = new();

    public AccountStorageTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static string SuitesWith(string name) =>
        StoredData.Write(new PlayerSuitesFile(StoredData.SuitesVersion, [new PlayerSuite { Slug = TextNormalizer.ToSlug(name), Name = name, CreatedAt = At, UpdatedAt = At }]));

    [Fact]
    public async Task Without_Session_The_Usual_Keys_Are_Used__F006_FR012()
    {
        var store = new BrowserStore(JSInterop.JSRuntime, _session);

        await store.SetAsync("hackmaster.suites", "{}");

        Assert.Equal("hackmaster.suites", JSInterop.Invocations["localStorage.setItem"].Single().Arguments[0]);
    }

    [Fact]
    public async Task With_Session_Browser_Keys_Carry_The_Account__F006_FR009()
    {
        _session.SetAccount("cuenta-a");
        var store = new BrowserStore(JSInterop.JSRuntime, _session);

        await store.SetAsync("hackmaster.rolls", "{}");
        await store.GetAsync("hackmaster.rolls");

        Assert.Equal("hackmaster.u.cuenta-a.rolls", JSInterop.Invocations["localStorage.setItem"].Single().Arguments[0]);
        Assert.Equal("hackmaster.u.cuenta-a.rolls", JSInterop.Invocations["localStorage.getItem"].Single().Arguments[0]);
    }

    [Fact]
    public async Task With_Session_Indexed_Keys_Carry_The_Account__F006_FR009()
    {
        var module = JSInterop.SetupModule(IndexedStore.ModulePath);
        _session.SetAccount("cuenta-a");
        var store = new IndexedStore(JSInterop.JSRuntime, _session);

        await store.SetAsync("hackmaster.characters", "{}");

        Assert.Equal("hackmaster.u.cuenta-a.characters", module.Invocations["set"].Single().Arguments[0]);
    }

    [Fact]
    public async Task Another_Account_Does_Not_See_The_First_Ones_Suites_Even_From_Memory__F006_US3()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.u.cuenta-a.suites").SetResult(SuitesWith("De A"));
        _session.SetAccount("cuenta-a");
        var store = new BrowserStore(JSInterop.JSRuntime, _session);
        var suites = new PlayerSuites(TestData.Create(), store, _session);
        await suites.LoadAsync();
        Assert.Equal(["De A"], suites.Suites.Select(s => s.Name));

        _session.SetAccount("cuenta-b");
        await suites.LoadAsync();

        Assert.Empty(suites.Suites);
    }

    [Fact]
    public async Task Another_Account_Does_Not_See_The_First_Ones_Characters__F006_US3()
    {
        var module = JSInterop.SetupModule(IndexedStore.ModulePath);
        module.Setup<string?>("get", "hackmaster.u.cuenta-a.characters").SetResult(
            StoredData.Write(new CharactersFile(StoredData.CharactersVersion, [new Character { Id = "c1", Name = "De A", CreatedAt = At, UpdatedAt = At }])));
        _session.SetAccount("cuenta-a");
        var characters = new PlayerCharacters(new IndexedStore(JSInterop.JSRuntime, _session), _session);
        await characters.LoadAsync();
        Assert.Single(characters.Characters);

        _session.SetAccount("cuenta-b");
        await characters.LoadAsync();

        Assert.Empty(characters.Characters);
    }

    [Fact]
    public async Task Writes_Of_Synced_Keys_Are_Announced_With_The_Base_Key__F006_FR005()
    {
        var store = new BrowserStore(JSInterop.JSRuntime, _session);
        var written = new List<(string Key, string Value)>();
        store.Written += (key, value) =>
        {
            written.Add((key, value));
            return Task.CompletedTask;
        };
        _session.SetAccount("cuenta-a");

        await store.SetAsync("hackmaster.suites", "{}");
        await store.SetLocalAsync("hackmaster.suites", "{\"de\":\"la nube\"}");

        Assert.Equal([("hackmaster.suites", "{}")], written);
    }

    [Fact]
    public async Task Without_Session_Writes_Are_Not_Announced__F006_FR012()
    {
        var store = new BrowserStore(JSInterop.JSRuntime, _session);
        var written = 0;
        store.Written += (_, _) =>
        {
            written++;
            return Task.CompletedTask;
        };

        await store.SetAsync("hackmaster.suites", "{}");

        Assert.Equal(0, written);
    }

    [Fact]
    public void Session_Rejects_Invalid_Account_Ids__F006_FR009()
    {
        Assert.Throws<ArgumentException>(() => _session.SetAccount("../otra"));
    }
}
