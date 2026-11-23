using AngleSharp.Dom;
using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

public class DoubtsPageTests : BunitContext
{
    public DoubtsPageTests()
    {
        Services.AddSingleton(TestData.Create());
        Services.AddSingleton(new GrimoireData([], []));
    }

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

    private static GrimoireData GrimoireWithDoubts() => new(
        [new Spell { Slug = "agrandar", Name = "Agrandar", SearchName = "agrandar", Class = SpellClass.Mage, Level = 1, Books = [new BookRef("MJ", "175")] }],
        [],
        [
            new SpellDoubt { Id = "conjuro-2", Subject = "Agrandar", Class = SpellClass.Mage, Slug = "agrandar", Source = "MJ 175", Question = "Prueba: ¿qué escuela?", Provisional = "Prueba: se deja pendiente." },
            new SpellDoubt { Id = "conjuro-3", Subject = "General", Question = "Prueba: duda general del Grimorio." },
        ]);

    [Fact]
    public void Spell_Doubts_Have_Their_Own_Section_With_A_Link_To_The_Spell__FR009_Grimorio()
    {
        Services.AddSingleton(GrimoireWithDoubts());

        var cut = Render<Doubts>();

        var section = cut.Find("[data-testid=spell-doubts]");
        var rows = section.QuerySelectorAll("[data-testid=spell-doubt-row]");
        Assert.Equal(2, rows.Length);
        Assert.Equal("grimorio/mago/agrandar", rows[0].QuerySelector(".doubt-subject a")?.GetAttribute("href"));
        Assert.Contains("Prueba: se deja pendiente.", rows[0].TextContent);
        Assert.Empty(rows[1].QuerySelectorAll("a"));
    }

    [Fact]
    public void With_Only_Spell_Doubts_The_Page_Shows_Just_That_Section__FR009_Grimorio()
    {
        Services.AddSingleton(new GameData(TestData.Skills, TestData.Suites, TestData.Styles, []));
        Services.AddSingleton(GrimoireWithDoubts());

        var cut = Render<Doubts>();

        Assert.DoesNotContain("No hay dudas abiertas", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll("[data-testid=spell-doubts]"));
        Assert.Empty(cut.FindAll("[data-testid=sheet-doubts]"));
        Assert.Empty(cut.FindAll("[data-testid=import-doubts]"));
    }

    [Fact]
    public void Without_Spell_Doubts_There_Is_No_Spell_Section__FR009_Grimorio()
    {
        var cut = Render<Doubts>();

        Assert.Empty(cut.FindAll("[data-testid=spell-doubts]"));
    }
}
