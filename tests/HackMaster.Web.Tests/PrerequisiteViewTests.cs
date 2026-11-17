using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Components;

namespace HackMaster.Web.Tests;

public class PrerequisiteViewTests : BunitContext
{
    private IRenderedComponent<PrerequisiteView> RenderWith(params PrerequisiteSegment[] segments) =>
        Render<PrerequisiteView>(parameters => parameters.Add(view => view.Prerequisite, new Prerequisite { Segments = segments }));

    [Fact]
    public void Skills_And_Suites_Are_Links_To_Their_Page__FR020()
    {
        var cut = RenderWith(
            new PrerequisiteSegment("Botánica", PrerequisiteSegmentKind.Skill, "botanica"),
            new PrerequisiteSegment(" o ", PrerequisiteSegmentKind.Text),
            new PrerequisiteSegment("Herbalista", PrerequisiteSegmentKind.Suite, "herbalista"),
            new PrerequisiteSegment(", ", PrerequisiteSegmentKind.Text),
            new PrerequisiteSegment("Cartógrafo", PrerequisiteSegmentKind.OfficialSuite, "cartografo"));

        var links = cut.FindAll("a");
        Assert.Equal(["habilidades/botanica", "conjuntos/herbalista", "conjuntos/oficiales/cartografo"], links.Select(link => link.GetAttribute("href")));
        Assert.Equal(["Botánica", "Herbalista", "Cartógrafo"], links.Select(link => link.TextContent));
        Assert.Equal("Botánica o Herbalista, Cartógrafo", cut.Find(".prerequisite").TextContent);
    }

    [Fact]
    public void Text_And_Conditions_Are_Plain_Text__FR020()
    {
        var cut = RenderWith(
            new PrerequisiteSegment("Sabiduría 14+", PrerequisiteSegmentKind.Condition),
            new PrerequisiteSegment(" y ", PrerequisiteSegmentKind.Text),
            new PrerequisiteSegment("Competencia con arco", PrerequisiteSegmentKind.Condition));

        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("Sabiduría 14+ y Competencia con arco", cut.Find(".prerequisite").TextContent);
    }

    [Fact]
    public void Unverified_Segment_Carries_The_Pending_Mark__FR027()
    {
        var cut = RenderWith(new PrerequisiteSegment("Rastreo (sin verificar)", PrerequisiteSegmentKind.Unverified));

        var mark = cut.Find("[data-testid=pending-mark]");
        Assert.Contains("Rastreo (sin verificar)", mark.TextContent);
        Assert.Contains("pendiente de verificar", mark.TextContent);
    }

    [Fact]
    public void No_Segments_Shows_None__FR020()
    {
        var cut = RenderWith();

        Assert.Equal("Ninguno", cut.Find(".prerequisite").TextContent.Trim());
    }
}
