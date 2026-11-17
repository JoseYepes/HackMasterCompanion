using AngleSharp.Dom;
using Bunit;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

public class DoubtsPageTests : BunitContext
{
    public DoubtsPageTests() => Services.AddSingleton(TestData.Create());

    private static IElement Row(IRenderedComponent<Doubts> cut, string question) =>
        cut.FindAll("[data-testid=doubt-row]").Single(row => row.TextContent.Contains(question));

    [Fact]
    public void Lists_Every_Doubt_With_Subject_Source_Question_And_Provisional_Decision__FR022()
    {
        var cut = Render<Doubts>();

        Assert.Equal(TestData.Doubts.Count, cut.FindAll("[data-testid=doubt-row]").Count);
        var row = Row(cut, "Prueba: ¿la dificultad es 25 o 30?");
        Assert.Contains("Cerrajería", row.TextContent);
        Assert.Contains("MJ 130", row.TextContent);
        Assert.Contains("Prueba: se dejó 25.", row.TextContent);
    }

    [Fact]
    public void Separates_Sheet_Doubts_From_Detected_Ones__FR023()
    {
        var cut = Render<Doubts>();

        var sheet = cut.Find("[data-testid=sheet-doubts]");
        var detected = cut.Find("[data-testid=import-doubts]");
        Assert.Equal(3, sheet.QuerySelectorAll("[data-testid=doubt-row]").Length);
        var imported = Assert.Single(detected.QuerySelectorAll("[data-testid=doubt-row]"));
        Assert.Contains("Liderazgo básico", imported.TextContent);
    }

    [Theory]
    [InlineData("Prueba: ¿la dificultad es 25 o 30?", "habilidades/cerrajeria")]
    [InlineData("Prueba: ¿qué habilidades incluye?", "conjuntos/oficiales/cartografo")]
    [InlineData("Dado de maestría «5» no es un dado existente.", "habilidades/liderazgo-basico")]
    public void Each_Doubt_Links_To_The_Page_Of_Its_Element__FR022(string question, string href)
    {
        var cut = Render<Doubts>();

        Assert.Equal(href, Row(cut, question).QuerySelector(".doubt-subject a")?.GetAttribute("href"));
    }

    [Fact]
    public void General_Doubt_Is_Shown_As_General_Without_Link__FR022()
    {
        var cut = Render<Doubts>();

        var row = Row(cut, "Prueba: falta una tabla.");
        Assert.Equal("General", row.QuerySelector(".doubt-subject")?.TextContent.Trim());
        Assert.Empty(row.QuerySelectorAll("a"));
        Assert.Contains("CG", row.TextContent);
    }

    [Fact]
    public void Without_Doubts_Says_So__FR022()
    {
        Services.AddSingleton(new GameData(TestData.Skills, TestData.Suites, TestData.Styles, []));

        var cut = Render<Doubts>();

        Assert.Empty(cut.FindAll("[data-testid=doubt-row]"));
        Assert.Contains("No hay dudas abiertas", cut.Markup);
    }
}
