using Bunit;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

public class StylesPageTests : BunitContext
{
    public StylesPageTests() => Services.AddSingleton(TestData.Create());

    [Fact]
    public void List_Shows_Each_Style_With_Abilities_Die_Price_Difficulty_And_Book__FR024()
    {
        var cut = Render<Styles>();

        var row = Assert.Single(cut.FindAll("[data-testid=style-row]"));
        Assert.Equal("Estilo del Yunque", row.QuerySelector(".style-name")?.TextContent.Trim());
        Assert.Equal("estilos/estilo-del-yunque", row.QuerySelector("a")?.GetAttribute("href"));
        Assert.Contains("FUE, CON", row.TextContent);
        Assert.Contains("d10", row.TextContent);
        Assert.Contains("800", row.TextContent);
        Assert.Contains("30 %", row.TextContent);
        Assert.Contains("CG 70", row.TextContent);
    }

    [Fact]
    public void List_Has_No_Search_Box__FR024()
    {
        var cut = Render<Styles>();

        Assert.Empty(cut.FindAll("[data-testid=search-input]"));
    }

    [Fact]
    public void Detail_Shows_Values_And_Linked_Prerequisite__FR024()
    {
        var cut = Render<StyleDetail>(parameters => parameters.Add(page => page.Slug, "estilo-del-yunque"));

        Assert.Equal("Estilo del Yunque", cut.Find("h1").TextContent.Trim());
        Assert.Contains("d10", cut.Find(".fields").TextContent);
        Assert.Contains("800", cut.Find(".fields").TextContent);
        Assert.Equal("habilidades/lucha", cut.Find(".fields .prerequisite a").GetAttribute("href"));
    }

    [Fact]
    public void Unknown_Slug_Shows_The_Not_Found_Page__FR024()
    {
        var cut = Render<StyleDetail>(parameters => parameters.Add(page => page.Slug, "no-existe"));

        Assert.Equal("Página no encontrada", cut.Find("h1").TextContent.Trim());
    }
}
