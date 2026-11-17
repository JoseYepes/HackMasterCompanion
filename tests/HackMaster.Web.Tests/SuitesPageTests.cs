using AngleSharp.Dom;
using Bunit;
using HackMaster.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

public class SuitesPageTests : BunitContext
{
    public SuitesPageTests() => Services.AddSingleton(TestData.Create());

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<Suites> RenderList(string relativeUri = "conjuntos")
    {
        Navigation.NavigateTo(relativeUri);
        return Render<Suites>();
    }

    private static IReadOnlyList<string> Names(IElement section) =>
        section.QuerySelectorAll("[data-testid=suite-row] .suite-name").Select(name => name.TextContent.Trim()).ToList();

    private static IElement Field<T>(IRenderedComponent<T> cut, string label)
        where T : IComponent =>
        cut.FindAll(".fields dt").Single(term => term.TextContent.Trim() == label).NextElementSibling!;

    private IRenderedComponent<SuiteDetail> RenderCustom(string slug) =>
        Render<SuiteDetail>(parameters => parameters.Add(page => page.Slug, slug));

    private IRenderedComponent<OfficialSuiteDetail> RenderOfficial(string slug) =>
        Render<OfficialSuiteDetail>(parameters => parameters.Add(page => page.Slug, slug));

    [Fact]
    public void List_Separates_Custom_And_Official_Suites__FR021()
    {
        var cut = RenderList();

        Assert.Equal(["Cartógrafo", "Explorador", "Herbalista"], Names(cut.Find("[data-testid=custom-suites]")));
        Assert.Equal(["Cartógrafo"], Names(cut.Find("[data-testid=official-suites]")));
    }

    [Fact]
    public void Each_Row_Shows_Cost_Price_Difficulty_And_Book__FR021()
    {
        var cut = RenderList();

        var row = cut.FindAll("[data-testid=suite-row]").Single(row => row.TextContent.Contains("Herbalista"));
        Assert.Contains("6", row.QuerySelector(".suite-cost")!.TextContent);
        Assert.Contains("400", row.QuerySelector(".suite-price")!.TextContent);
        Assert.Contains("10 %", row.QuerySelector(".suite-difficulty")!.TextContent);
        Assert.Contains("HJ 4.05", row.QuerySelector(".suite-book")!.TextContent);
        Assert.Equal("conjuntos/herbalista", row.QuerySelector("a")?.GetAttribute("href"));
    }

    [Fact]
    public void Official_Rows_Link_To_The_Official_Page_And_Mark_Missing_Values__FR027()
    {
        var cut = RenderList();

        var row = cut.Find("[data-testid=official-suites] [data-testid=suite-row]");
        Assert.Equal("conjuntos/oficiales/cartografo", row.QuerySelector("a")?.GetAttribute("href"));
        Assert.NotNull(row.QuerySelector(".suite-price [data-testid=pending-mark]"));
    }

    [Theory]
    [InlineData("HERBAL")]
    [InlineData("cartógrafo")]
    public void Search_By_Name_Ignores_Accents_And_Case__FR007(string text)
    {
        var cut = RenderList();

        cut.Find("[data-testid=search-input]").Input(text);

        var expected = TextNormalizer(text);
        Assert.All(cut.FindAll("[data-testid=suite-row] .suite-name"), name => Assert.Contains(expected, TextNormalizer(name.TextContent)));
        Assert.NotEmpty(cut.FindAll("[data-testid=suite-row]"));
        Assert.Contains("q=", Navigation.Uri);
    }

    [Fact]
    public void Search_From_The_Address_Is_Restored__US3()
    {
        var cut = RenderList("conjuntos?q=explo");

        Assert.Equal("explo", cut.Find("[data-testid=search-input]").GetAttribute("value"));
        Assert.Equal(["Explorador"], Names(cut.Find("[data-testid=custom-suites]")));
        Assert.Empty(Names(cut.Find("[data-testid=official-suites]")));
    }

    [Fact]
    public void Detail_Lists_Components_With_Links_To_Skills_And_Suites__FR020()
    {
        var cut = RenderCustom("explorador");

        var links = cut.FindAll(".components a");
        Assert.Equal(["habilidades/rastrear", "conjuntos/herbalista"], links.Select(link => link.GetAttribute("href")));
        Assert.Equal(["Rastrear", "Herbalista"], links.Select(link => link.TextContent));
    }

    [Fact]
    public void Same_Name_Has_Two_Different_Pages_For_Custom_And_Official__FR021()
    {
        var custom = RenderCustom("cartografo");
        var official = RenderOfficial("cartografo");

        Assert.Equal("350", Field(custom, "Precio (po)").TextContent.Trim());
        Assert.Contains("Zoología", custom.Find(".components").TextContent);
        Assert.Contains("oficial", official.Find(".suite-kind").TextContent);
        Assert.DoesNotContain("oficial", custom.Find(".suite-kind").TextContent);
    }

    [Fact]
    public void Official_Without_Components_Or_Values_Shows_Pending__FR027()
    {
        var cut = RenderOfficial("cartografo");

        Assert.NotNull(cut.Find(".components").QuerySelector("[data-testid=pending-mark]"));
        Assert.Contains("«?»", Field(cut, "Precio (po)").TextContent);
        Assert.NotNull(Field(cut, "Precio (po)").QuerySelector("[data-testid=pending-mark]"));
    }

    [Fact]
    public void Detail_Shows_The_Doubts_Of_The_Suite__FR022()
    {
        var official = RenderOfficial("cartografo");
        var custom = RenderCustom("cartografo");

        Assert.Contains("Prueba: ¿qué habilidades incluye?", official.Find("[data-testid=doubt-notice]").TextContent);
        Assert.Empty(custom.FindAll("[data-testid=doubt-notice]"));
    }

    [Fact]
    public void Unknown_Slug_Shows_The_Not_Found_Page__FR021()
    {
        Assert.Equal("Página no encontrada", RenderCustom("no-existe").Find("h1").TextContent.Trim());
        Assert.Equal("Página no encontrada", RenderOfficial("herbalista").Find("h1").TextContent.Trim());
    }

    private static string TextNormalizer(string text) => HackMaster.Domain.TextNormalizer.Normalize(text);
}
