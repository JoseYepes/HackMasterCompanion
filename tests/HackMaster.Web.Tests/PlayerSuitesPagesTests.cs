using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Los conjuntos del jugador en la lista y en la ficha de conjuntos (FR-014, aclaración de la spec).</summary>
public class PlayerSuitesPagesTests : BunitContext
{
    private readonly PlayerSuites _suites;

    public PlayerSuitesPagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var data = TestData.Create();
        Services.AddSingleton(data);
        var store = new BrowserStore(JSInterop.JSRuntime);
        Services.AddSingleton(store);
        _suites = new PlayerSuites(data, store);
        Services.AddSingleton(_suites);
        _suites.SaveAsync("Naturalista", [new(ElementKind.Skill, "botanica"), new(ElementKind.Skill, "zoologia")], [Ability.INT], null, DateTimeOffset.Now)
            .GetAwaiter().GetResult();
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void List_Shows_Player_Suites_Among_The_Sheet_Ones_With_The_Same_Look__FR014()
    {
        Navigation.NavigateTo("conjuntos");
        var cut = Render<Suites>();

        var custom = cut.Find("[data-testid=custom-suites]");
        var names = custom.QuerySelectorAll("[data-testid=suite-row] .suite-name").Select(name => name.TextContent.Trim()).ToList();
        Assert.Equal(["Cartógrafo", "Explorador", "Herbalista", "Naturalista"], names);
        Assert.Contains("(4)", custom.QuerySelector("h2")!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void List_Search_Also_Finds_Player_Suites__FR014()
    {
        Navigation.NavigateTo("conjuntos?q=natural");
        var cut = Render<Suites>();

        Assert.Single(cut.Find("[data-testid=custom-suites]").QuerySelectorAll("[data-testid=suite-row]"));
    }

    [Fact]
    public void List_Offers_To_Create_A_Suite__FR021()
    {
        Navigation.NavigateTo("conjuntos");
        var cut = Render<Suites>();

        Assert.Equal("conjuntos/nuevo", cut.Find("[data-testid=suite-create]").GetAttribute("href"));
    }

    [Fact]
    public void Player_Suite_Page_Shows_Calculated_Values_And_Can_Be_Edited__FR014()
    {
        var cut = Render<SuiteDetail>(parameters => parameters.Add(page => page.Slug, "naturalista"));

        Assert.Equal("Naturalista", cut.Find("h1").TextContent.Trim());
        Assert.Contains("385", cut.Find(".fields").TextContent, StringComparison.Ordinal);
        Assert.Equal("conjuntos/naturalista/editar", cut.Find("[data-testid=suite-edit]").GetAttribute("href"));
    }

    [Fact]
    public void Sheet_Suite_Page_Cannot_Be_Edited__FR014()
    {
        var cut = Render<SuiteDetail>(parameters => parameters.Add(page => page.Slug, "herbalista"));

        Assert.Equal("Herbalista", cut.Find("h1").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid=suite-edit]"));
        Assert.Empty(cut.FindAll("[data-testid=suite-delete]"));
    }

    [Fact]
    public void Player_Suite_Can_Be_Deleted_From_Its_Page_After_Confirming__FR014()
    {
        var cut = Render<SuiteDetail>(parameters => parameters.Add(page => page.Slug, "naturalista"));

        cut.Find("[data-testid=suite-delete]").Click();
        Assert.NotNull(_suites.Find("naturalista"));
        cut.Find("[data-testid=suite-delete-confirm]").Click();

        cut.WaitForAssertion(() => Assert.Null(_suites.Find("naturalista")));
        Assert.EndsWith("/conjuntos", Navigation.Uri, StringComparison.Ordinal);
    }
}
