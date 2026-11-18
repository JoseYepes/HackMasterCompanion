using AngleSharp.Dom;
using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Creador de conjuntos (US2: FR-010 a FR-014).</summary>
public class SuiteEditorTests : BunitContext
{
    private readonly PlayerSuites _suites;

    public SuiteEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var data = TestData.Create();
        Services.AddSingleton(data);
        var store = new BrowserStore(JSInterop.JSRuntime);
        Services.AddSingleton(store);
        _suites = new PlayerSuites(data, store);
        Services.AddSingleton(_suites);
    }

    private static void Add(IRenderedComponent<SuiteEditor> cut, string search, string name)
    {
        cut.Find("[data-testid=suite-search]").Input(search);
        cut.FindAll("[data-testid=suite-result]").Single(result => result.TextContent.Contains(name, StringComparison.Ordinal))
            .QuerySelector("button")!.Click();
    }

    private static IElement Component(IRenderedComponent<SuiteEditor> cut, string name) =>
        cut.FindAll("[data-testid=suite-component]").Single(row => row.TextContent.Contains(name, StringComparison.Ordinal));

    private static string Summary(IRenderedComponent<SuiteEditor> cut) => cut.Find("[data-testid=suite-summary]").TextContent;

    [Fact]
    public void Searching_Without_Accents_Adds_A_Component__FR011()
    {
        var cut = Render<SuiteEditor>();

        Add(cut, "botanica", "Botánica");

        Assert.Single(cut.FindAll("[data-testid=suite-component]"));
    }

    [Fact]
    public void Sheet_Suites_Can_Be_Found_And_Added__FR011()
    {
        var cut = Render<SuiteEditor>();

        Add(cut, "herbal", "Herbalista");

        Assert.Contains("Herbalista", Component(cut, "Herbalista").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Each_Component_Shows_Its_Cost_Price_Difficulty_And_Die__US2()
    {
        var cut = Render<SuiteEditor>();

        Add(cut, "zoolog", "Zoología");

        var row = Component(cut, "Zoología").TextContent;
        Assert.Contains("2 PC", row, StringComparison.Ordinal);
        Assert.Contains("100 po", row, StringComparison.Ordinal);
        Assert.Contains("0 %", row, StringComparison.Ordinal);
        Assert.Contains("d8", row, StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_A_Component_Twice_Does_Not_Repeat_It_And_Warns__FR011()
    {
        var cut = Render<SuiteEditor>();
        Add(cut, "botanica", "Botánica");

        Add(cut, "botanica", "Botánica");

        Assert.Single(cut.FindAll("[data-testid=suite-component]"));
        Assert.Contains("ya está", cut.Find("[data-testid=suite-notice]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Summary_Is_Recalculated_On_Every_Change__FR009()
    {
        var cut = Render<SuiteEditor>();
        Add(cut, "botanica", "Botánica");
        Add(cut, "zoologia", "Zoología");
        Assert.Contains("4 PC", Summary(cut), StringComparison.Ordinal);
        Assert.Contains("385 po", Summary(cut), StringComparison.Ordinal);

        Add(cut, "lucha", "Lucha");

        Assert.Contains("5 PC", Summary(cut), StringComparison.Ordinal);
        Assert.Contains("440 po", Summary(cut), StringComparison.Ordinal);
    }

    [Fact]
    public void Pending_Values_Are_Marked__FR013()
    {
        var cut = Render<SuiteEditor>();
        Add(cut, "liderazgo", "Liderazgo básico");
        Add(cut, "lucha", "Lucha");

        Assert.NotEmpty(cut.FindAll("[data-testid=suite-summary] [data-testid=pending-mark]"));
    }

    [Fact]
    public void Prerequisite_Says_Those_Of_Its_Skills_And_Lists_Each__FR012()
    {
        var cut = Render<SuiteEditor>();
        Add(cut, "herbalismo", "Herbalismo");
        Add(cut, "lucha", "Lucha");

        var prerequisites = cut.Find("[data-testid=suite-prerequisites]").TextContent;
        Assert.Contains(PlayerSuites.PrerequisiteText, prerequisites, StringComparison.Ordinal);
        Assert.Contains("Herbalismo:", prerequisites, StringComparison.Ordinal);
        Assert.Contains("Sabiduría 12+", prerequisites, StringComparison.Ordinal);
        Assert.Contains("Lucha:", prerequisites, StringComparison.Ordinal);
    }

    [Fact]
    public void Abilities_Can_Only_Be_Chosen_Among_Those_Of_The_Components__FR012()
    {
        var cut = Render<SuiteEditor>();
        Add(cut, "botanica", "Botánica");

        var offered = cut.FindAll("[data-testid=suite-ability] input").Select(input => input.GetAttribute("value"));

        Assert.Equal(["INT"], offered);
    }

    [Fact]
    public void Cannot_Save_Explains_Why__FR010()
    {
        var cut = Render<SuiteEditor>();
        Add(cut, "botanica", "Botánica");
        cut.Find("[data-testid=suite-name]").Input("Mío");

        Assert.Contains("entre 2 y 5", cut.Find("[data-testid=suite-cannot-save]").TextContent, StringComparison.Ordinal);
        Assert.True(cut.Find("[data-testid=suite-save]").HasAttribute("disabled"));
    }

    [Fact]
    public void Saving_Stores_The_Suite_And_Opens_Its_Page__FR014()
    {
        var cut = Render<SuiteEditor>();
        Add(cut, "botanica", "Botánica");
        Add(cut, "zoologia", "Zoología");
        cut.Find("[data-testid=suite-name]").Input("Naturalista");
        cut.Find("[data-testid=suite-ability] input").Change(true);

        cut.Find("[data-testid=suite-save]").Click();

        cut.WaitForAssertion(() => Assert.EndsWith("/conjuntos/naturalista", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal));
        var saved = _suites.Find("naturalista")!;
        Assert.Equal([Ability.INT], saved.Abilities);
    }

    [Fact]
    public async Task Edit_Address_Loads_A_Player_Suite_And_Delete_Asks_First__FR014()
    {
        await _suites.SaveAsync("Naturalista", [new(ElementKind.Skill, "botanica"), new(ElementKind.Skill, "zoologia")], [], null, DateTimeOffset.Now);
        var cut = Render<SuiteEditor>(parameters => parameters.Add(page => page.Slug, "naturalista"));

        Assert.Equal("Naturalista", cut.Find("[data-testid=suite-name]").GetAttribute("value"));
        Assert.Equal(2, cut.FindAll("[data-testid=suite-component]").Count);

        cut.Find("[data-testid=suite-delete]").Click();
        Assert.NotNull(_suites.Find("naturalista"));
        cut.Find("[data-testid=suite-delete-confirm]").Click();

        cut.WaitForAssertion(() => Assert.Null(_suites.Find("naturalista")));
    }

    [Fact]
    public async Task Delete_Warns_About_The_Suites_That_Use_It__FR014()
    {
        await _suites.SaveAsync("Naturalista", [new(ElementKind.Skill, "botanica"), new(ElementKind.Skill, "zoologia")], [], null, DateTimeOffset.Now);
        await _suites.SaveAsync("Explorador mayor", [new(ElementKind.Suite, "naturalista"), new(ElementKind.Skill, "rastrear")], [], null, DateTimeOffset.Now);
        var cut = Render<SuiteEditor>(parameters => parameters.Add(page => page.Slug, "naturalista"));

        cut.Find("[data-testid=suite-delete]").Click();

        Assert.Contains("Explorador mayor", cut.Find("[data-testid=suite-delete-warning]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Edit_Address_Of_A_Sheet_Suite_Shows_Not_Found__FR014()
    {
        var cut = Render<SuiteEditor>(parameters => parameters.Add(page => page.Slug, "herbalista"));

        Assert.DoesNotContain("Crear conjunto", cut.Find("h1").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=suite-name]"));
    }
}
