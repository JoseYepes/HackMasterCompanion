using AngleSharp.Dom;
using Bunit;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

public class SkillDetailTests : BunitContext
{
    public SkillDetailTests() => Services.AddSingleton(TestData.Create());

    private IRenderedComponent<SkillDetail> RenderSkill(string slug) =>
        Render<SkillDetail>(parameters => parameters.Add(page => page.Slug, slug));

    /// <summary>Valor mostrado junto a una etiqueta de la ficha.</summary>
    private static IElement Field(IRenderedComponent<SkillDetail> cut, string label) =>
        cut.FindAll(".fields dt").Single(term => term.TextContent.Trim() == label).NextElementSibling!;

    [Fact]
    public void Shows_Every_Field_Of_The_Skill__FR019()
    {
        var cut = RenderSkill("herbalismo");

        Assert.Equal("Herbalismo", cut.Find("h1").TextContent.Trim());
        Assert.Equal("Técnicas", Field(cut, "Tipo").TextContent.Trim());
        Assert.Equal("5", Field(cut, "Coste (PC)").TextContent.Trim());
        Assert.Equal("INT", Field(cut, "Características").TextContent.Trim());
        Assert.Equal("d8", Field(cut, "Dado de maestría").TextContent.Trim());
        Assert.Contains("Botánica", Field(cut, "Requisitos").TextContent);
        Assert.Equal("Grupo de clérigos", Field(cut, "Clases permitidas").TextContent.Trim());
    }

    [Fact]
    public void Price_Has_No_Thousands_Separator_And_Difficulty_Has_Percent__FR025()
    {
        var cut = RenderSkill("herbalismo");

        Assert.Equal("1403", Field(cut, "Precio (po)").TextContent.Trim());
        Assert.Equal("-5 %", Field(cut, "Dificultad").TextContent.Trim());
    }

    [Fact]
    public void Books_Show_Acronym_And_Page__FR025()
    {
        var cut = RenderSkill("cerrajeria");

        Assert.Equal("MJ 130 / GMG 71", Field(cut, "Libro").TextContent.Trim());
    }

    [Fact]
    public void Shows_The_Own_Summary__FR019()
    {
        var cut = RenderSkill("herbalismo");

        Assert.Contains("Prueba: remedios con botánica y hierbas.", cut.Find(".summary").TextContent);
    }

    [Fact]
    public void Missing_Summary_Is_Shown_As_Pending_Without_Invented_Text__FR027()
    {
        var cut = RenderSkill("rastrear");

        var summary = cut.Find(".summary");
        Assert.NotNull(summary.QuerySelector("[data-testid=pending-mark]"));
        Assert.Equal("pendiente de verificar", summary.TextContent.Trim());
    }

    [Fact]
    public void Pending_Field_Shows_The_Mark_Next_To_The_Original_Value__FR027()
    {
        var cut = RenderSkill("liderazgo-basico");

        var die = Field(cut, "Dado de maestría");
        Assert.NotNull(die.QuerySelector("[data-testid=pending-mark]"));
        Assert.Contains("«5»", die.TextContent);
    }

    [Fact]
    public void Prerequisite_Skills_Are_Links__FR020()
    {
        var cut = RenderSkill("herbalismo");

        Assert.Equal("habilidades/botanica", Field(cut, "Requisitos").QuerySelector("a")?.GetAttribute("href"));
    }

    [Fact]
    public void Lists_The_Suites_That_Contain_It_With_Links__FR019()
    {
        var cut = RenderSkill("botanica");

        var link = Assert.Single(cut.FindAll(".suites a"));
        Assert.Equal("Herbalista", link.TextContent);
        Assert.Equal("conjuntos/herbalista", link.GetAttribute("href"));
    }

    [Fact]
    public void Says_So_When_It_Is_In_No_Suite__FR019()
    {
        var cut = RenderSkill("karate");

        Assert.Empty(cut.FindAll(".suites a"));
        Assert.Contains("ningún conjunto", cut.Find(".suites").TextContent);
    }

    [Fact]
    public void Doubt_Notice_Shows_Question_Source_And_Provisional_Decision__FR019()
    {
        var cut = RenderSkill("cerrajeria");

        var notice = cut.Find("[data-testid=doubt-notice]");
        Assert.Contains("Prueba: ¿la dificultad es 25 o 30?", notice.TextContent);
        Assert.Contains("MJ 130", notice.TextContent);
        Assert.Contains("Prueba: se dejó 25.", notice.TextContent);
    }

    [Fact]
    public void Skill_Without_Doubts_Has_No_Notice__FR019()
    {
        var cut = RenderSkill("botanica");

        Assert.Empty(cut.FindAll("[data-testid=doubt-notice]"));
    }

    [Fact]
    public void Unknown_Slug_Shows_The_Not_Found_Page__FR019()
    {
        var cut = RenderSkill("no-existe");

        Assert.Equal("Página no encontrada", cut.Find("h1").TextContent.Trim());
    }
}
