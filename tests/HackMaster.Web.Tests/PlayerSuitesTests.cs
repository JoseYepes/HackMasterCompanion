using Bunit;
using HackMaster.Domain;
using Microsoft.JSInterop;

namespace HackMaster.Web.Tests;

/// <summary>Conjuntos del jugador: guardar, listar con los de la hoja, nombres, borrar (FR-011, FR-014).</summary>
public class PlayerSuitesTests : BunitContext
{
    private static readonly DateTimeOffset At = new(2026, 11, 18, 12, 0, 0, TimeSpan.FromHours(1));

    private static readonly SuiteComponentRef Botanica = new(ElementKind.Skill, "botanica");
    private static readonly SuiteComponentRef Zoologia = new(ElementKind.Skill, "zoologia");
    private static readonly SuiteComponentRef Rastrear = new(ElementKind.Skill, "rastrear");
    private static readonly SuiteComponentRef Lucha = new(ElementKind.Skill, "lucha");

    public PlayerSuitesTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private PlayerSuites NewService() => new(TestData.Create(), new BrowserStore(JSInterop.JSRuntime));

    private static async Task<PlayerSuite> SaveOk(PlayerSuites service, string name, params SuiteComponentRef[] components)
    {
        var result = await service.SaveAsync(name, components, [Ability.INT], originalSlug: null, At);
        Assert.Null(result.Problem);
        return result.Saved!;
    }

    [Fact]
    public async Task Saved_Suite_Appears_With_The_Sheet_Suites_Sorted_By_Name__FR014()
    {
        var service = NewService();

        var saved = await SaveOk(service, "Naturalista", Botanica, Zoologia);

        Assert.Equal("naturalista", saved.Slug);
        Assert.Equal(["Cartógrafo", "Explorador", "Herbalista", "Naturalista"], service.CustomSuites.Select(suite => suite.Name));
        Assert.Contains("\"name\": \"Naturalista\"", (string)JSInterop.Invocations["localStorage.setItem"].Last().Arguments[1]!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Player_Suite_Looks_Like_A_Sheet_Suite_With_Calculated_Values__FR014()
    {
        var service = NewService();
        await SaveOk(service, "Naturalista", Botanica, Zoologia);

        var suite = service.CustomSuites.Single(suite => suite.Slug == "naturalista");

        Assert.Equal(SuiteKind.Custom, suite.Kind);
        Assert.Equal((4, 385, 15, "d8"), (suite.Cost, suite.Price, suite.Difficulty, suite.MasteryDie));
        Assert.Equal(["Todas"], suite.Classes);
        Assert.Equal(["Botánica", "Zoología"], suite.Components.Select(component => component.Name));
        Assert.Equal("Las de las habilidades que lo componen", Assert.Single(suite.Prerequisite.Segments).Text);
        Assert.True(service.IsPlayerSuite("naturalista"));
        Assert.False(service.IsPlayerSuite("herbalista"));
    }

    [Fact]
    public async Task Stored_Suites_Are_Loaded_From_The_Device__FR017()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.suites").SetResult("""
            { "schemaVersion": 1, "suites": [ { "slug": "mio", "name": "Mío",
              "components": [ { "kind": "Skill", "slug": "botanica" }, { "kind": "Skill", "slug": "lucha" } ],
              "abilities": ["INT"], "createdAt": "2026-11-18T10:00:00+01:00", "updatedAt": "2026-11-18T10:00:00+01:00" } ] }
            """);
        var service = NewService();

        await service.LoadAsync();

        Assert.Equal("Mío", Assert.Single(service.Suites).Name);
        Assert.Null(service.LoadError);
    }

    [Theory]
    [InlineData("Herbalista")]
    [InlineData("herbalísta")]
    [InlineData("Nuevo")]
    [InlineData("Oficiales")]
    [InlineData("  ")]
    public async Task Name_Taken_Reserved_Or_Empty_Is_Rejected__FR014(string name)
    {
        var service = NewService();

        var result = await service.SaveAsync(name, [Botanica, Zoologia], [], originalSlug: null, At);

        Assert.Null(result.Saved);
        Assert.False(string.IsNullOrEmpty(result.Problem));
        Assert.Empty(service.Suites);
    }

    [Fact]
    public async Task Name_Of_Another_Player_Suite_Is_Rejected_But_Keeping_Its_Own_Is_Not__FR014()
    {
        var service = NewService();
        await SaveOk(service, "Naturalista", Botanica, Zoologia);

        var duplicate = await service.SaveAsync("Naturalista", [Rastrear, Lucha], [], originalSlug: null, At);
        var edit = await service.SaveAsync("Naturalista", [Botanica, Zoologia, Rastrear], [], originalSlug: "naturalista", At);

        Assert.NotNull(duplicate.Problem);
        Assert.Null(edit.Problem);
        Assert.Equal(3, Assert.Single(service.Suites).Components.Count);
    }

    [Fact]
    public async Task Composition_Problems_Prevent_Saving__FR010()
    {
        var service = NewService();

        var result = await service.SaveAsync("Solo uno", [Botanica], [], originalSlug: null, At);

        Assert.Contains("entre 2 y 5", result.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_Player_Suite_Can_Be_A_Component_And_Contributes_Its_Values__FR011()
    {
        var service = NewService();
        await SaveOk(service, "Naturalista", Botanica, Zoologia);

        await SaveOk(service, "Explorador mayor", new SuiteComponentRef(ElementKind.Suite, "naturalista"), Rastrear);

        var suite = service.CustomSuites.Single(suite => suite.Slug == "explorador-mayor");
        Assert.Equal(4 + 3 - 1, suite.Cost);
        Assert.Equal(["Naturalista", "Rastrear"], suite.Components.Select(component => component.Name));
    }

    [Fact]
    public async Task A_Suite_Cannot_Include_Itself_Through_Another__FR011()
    {
        var service = NewService();
        await SaveOk(service, "Naturalista", Botanica, Zoologia);
        await SaveOk(service, "Explorador mayor", new SuiteComponentRef(ElementKind.Suite, "naturalista"), Rastrear);

        var result = await service.SaveAsync("Naturalista", [Botanica, new SuiteComponentRef(ElementKind.Suite, "explorador-mayor")], [], "naturalista", At);

        Assert.Contains("a sí mismo", result.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Renaming_Updates_The_Suites_That_Use_It__FR014()
    {
        var service = NewService();
        await SaveOk(service, "Naturalista", Botanica, Zoologia);
        await SaveOk(service, "Explorador mayor", new SuiteComponentRef(ElementKind.Suite, "naturalista"), Rastrear);

        await service.SaveAsync("Naturalista experto", [Botanica, Zoologia], [], "naturalista", At);

        Assert.Contains(new SuiteComponentRef(ElementKind.Suite, "naturalista-experto"), service.Find("explorador-mayor")!.Components);
    }

    [Fact]
    public async Task Deleting_Lists_The_Suites_That_Use_It_And_Removes_It__FR014()
    {
        var service = NewService();
        await SaveOk(service, "Naturalista", Botanica, Zoologia);
        await SaveOk(service, "Explorador mayor", new SuiteComponentRef(ElementKind.Suite, "naturalista"), Rastrear);

        Assert.Equal(["Explorador mayor"], service.UsedBy("naturalista"));
        await service.DeleteAsync("naturalista");

        Assert.Null(service.Find("naturalista"));
        Assert.DoesNotContain(service.CustomSuites, suite => suite.Slug == "naturalista");
    }

    [Fact]
    public async Task A_Component_That_No_Longer_Exists_Is_Pointed_Out__FR014()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.suites").SetResult("""
            { "schemaVersion": 1, "suites": [ { "slug": "viejo", "name": "Viejo",
              "components": [ { "kind": "Skill", "slug": "botanica" }, { "kind": "Skill", "slug": "ya-no-existe" } ],
              "abilities": [], "createdAt": "2026-11-18T10:00:00+01:00", "updatedAt": "2026-11-18T10:00:00+01:00" } ] }
            """);
        var service = NewService();
        await service.LoadAsync();

        var suite = service.CustomSuites.Single(suite => suite.Slug == "viejo");

        Assert.Equal([new SuiteComponentRef(ElementKind.Skill, "ya-no-existe")], service.MissingComponents(service.Find("viejo")!));
        Assert.Null(suite.Cost);
        Assert.Contains("components", suite.Pending);
    }

    [Fact]
    public async Task Damaged_Storage_Is_Reported_And_Not_Overwritten__FR019()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.suites").SetResult("{ dañado");
        var service = NewService();
        await service.LoadAsync();

        var result = await service.SaveAsync("Naturalista", [Botanica, Zoologia], [], null, At);

        Assert.NotNull(service.LoadError);
        Assert.NotNull(result.Problem);
        Assert.Empty(JSInterop.Invocations["localStorage.setItem"]);
    }
}
