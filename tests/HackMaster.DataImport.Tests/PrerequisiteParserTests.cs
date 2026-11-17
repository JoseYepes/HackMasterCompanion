using HackMaster.Domain;

namespace HackMaster.DataImport.Tests;

public class PrerequisiteParserTests
{
    private static readonly Dictionary<string, (ElementKind Kind, string Slug)> Known = new()
    {
        ["botanica"] = (ElementKind.Skill, "botanica"),
        ["herbalismo"] = (ElementKind.Skill, "herbalismo"),
        ["sung hoy"] = (ElementKind.Skill, "sung-hoy"),
        ["curlamo"] = (ElementKind.Skill, "curlamo"),
        ["bilodo"] = (ElementKind.Skill, "bilodo"),
        ["gung-fu"] = (ElementKind.Skill, "gung-fu"),
        ["tasacion: gemas"] = (ElementKind.Skill, "tasacion-gemas"),
        ["arte de la seduccion, el"] = (ElementKind.Skill, "arte-de-la-seduccion-el"),
        ["vandalismo y profanacion"] = (ElementKind.Skill, "vandalismo-y-profanacion"),
        ["trabajar el metal"] = (ElementKind.Skill, "trabajar-el-metal"),
        ["movimiento sigiloso"] = (ElementKind.Skill, "movimiento-sigiloso"),
        ["rastrear"] = (ElementKind.Skill, "rastrear"),
        ["cartografo"] = (ElementKind.Suite, "cartografo"),
        ["conjunto de habilidades de primeros auxilios"] = (ElementKind.OfficialSuite, "conjunto-de-habilidades-de-primeros-auxilios"),
    };

    private static PrerequisiteParseResult Parse(string? text) =>
        PrerequisiteParser.Parse(text, name => Known.TryGetValue(TextNormalizer.Normalize(name), out var found) ? found : null);

    private static string Rebuild(PrerequisiteParseResult result) =>
        string.Concat(result.Prerequisite.Segments.Select(segment => segment.Text));

    [Theory]
    [InlineData("N")]
    [InlineData("n")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void No_Requirement_Has_No_Segments__FR003(string? text)
    {
        var result = Parse(text);

        Assert.Empty(result.Prerequisite.Segments);
        Assert.Empty(result.Unresolved);
        Assert.False(result.HasUnverified);
    }

    [Fact]
    public void Single_Skill_Is_Linked__FR020()
    {
        var result = Parse("Botánica");

        var segment = Assert.Single(result.Prerequisite.Segments);
        Assert.Equal(new PrerequisiteSegment("Botánica", PrerequisiteSegmentKind.Skill, "botanica"), segment);
        Assert.Empty(result.Unresolved);
    }

    [Fact]
    public void Skill_Name_Matches_Without_Accents_Or_Case__FR007()
    {
        var result = Parse("botanica");

        Assert.Equal("botanica", Assert.Single(result.Prerequisite.Segments).Target);
    }

    [Theory]
    [InlineData("Botánica, Herbalismo", ", ")]
    [InlineData("Sung Hoy o Curlamo", " o ")]
    [InlineData("Botánica y Herbalismo", " y ")]
    public void Two_Skills_Are_Linked_With_A_Text_Separator__FR020(string text, string separator)
    {
        var result = Parse(text);

        Assert.Collection(
            result.Prerequisite.Segments,
            first => Assert.Equal(PrerequisiteSegmentKind.Skill, first.Kind),
            middle => Assert.Equal(new PrerequisiteSegment(separator, PrerequisiteSegmentKind.Text), middle),
            last => Assert.Equal(PrerequisiteSegmentKind.Skill, last.Kind));
        Assert.Empty(result.Unresolved);
        Assert.Equal(text, Rebuild(result));
    }

    [Fact]
    public void Three_Skills_With_Mixed_Separators_Are_Linked__FR020()
    {
        var result = Parse("Sung Hoy, Bilodo o Curlamo");

        Assert.Equal(
            ["sung-hoy", "bilodo", "curlamo"],
            result.Prerequisite.Segments.Where(segment => segment.Kind == PrerequisiteSegmentKind.Skill).Select(segment => segment.Target));
        Assert.Equal("Sung Hoy, Bilodo o Curlamo", Rebuild(result));
    }

    [Theory]
    [InlineData("Tasación: gemas", "tasacion-gemas")]
    [InlineData("Arte de la seducción, el", "arte-de-la-seduccion-el")]
    [InlineData("Vandalismo y profanación", "vandalismo-y-profanacion")]
    public void Skill_Name_Containing_A_Separator_Is_One_Link__FR020(string text, string slug)
    {
        var result = Parse(text);

        var segment = Assert.Single(result.Prerequisite.Segments);
        Assert.Equal(PrerequisiteSegmentKind.Skill, segment.Kind);
        Assert.Equal(slug, segment.Target);
    }

    [Fact]
    public void Skill_Name_Containing_A_Separator_Is_Found_Inside_A_List__FR020()
    {
        var result = Parse("Tasación: gemas, Trabajar el metal");

        Assert.Equal(
            ["tasacion-gemas", "trabajar-el-metal"],
            result.Prerequisite.Segments.Where(segment => segment.Kind == PrerequisiteSegmentKind.Skill).Select(segment => segment.Target));
        Assert.Empty(result.Unresolved);
    }

    [Fact]
    public void Suite_Reference_Is_Linked_As_Suite__FR020()
    {
        var segment = Assert.Single(Parse("Cartógrafo").Prerequisite.Segments);

        Assert.Equal(PrerequisiteSegmentKind.Suite, segment.Kind);
        Assert.Equal("cartografo", segment.Target);
    }

    [Fact]
    public void Official_Suite_Reference_Is_Linked_As_Official_Suite__FR020()
    {
        var segment = Assert.Single(Parse("Conjunto de Habilidades de primeros auxilios").Prerequisite.Segments);

        Assert.Equal(PrerequisiteSegmentKind.OfficialSuite, segment.Kind);
    }

    [Theory]
    [InlineData("Sabiduría 14+")]
    [InlineData("Fuerza 17+")]
    [InlineData("Competencia básica en el arma")]
    [InlineData("otra maniobra especial al 101%+")]
    [InlineData("2 más al 51%+")]
    [InlineData("NA")]
    public void Recognized_Condition_Is_Not_An_Error__FR005(string text)
    {
        var result = Parse(text);

        var segment = Assert.Single(result.Prerequisite.Segments);
        Assert.Equal(new PrerequisiteSegment(text, PrerequisiteSegmentKind.Condition), segment);
        Assert.Empty(result.Unresolved);
    }

    [Fact]
    public void At_Least_N_Of_Is_A_Condition_Followed_By_Linked_Skills__FR005()
    {
        var result = Parse("Al menos dos de: Movimiento sigiloso, Rastrear");

        Assert.Equal(PrerequisiteSegmentKind.Condition, result.Prerequisite.Segments[0].Kind);
        Assert.Equal("Al menos dos de", result.Prerequisite.Segments[0].Text);
        Assert.Equal(2, result.Prerequisite.Segments.Count(segment => segment.Kind == PrerequisiteSegmentKind.Skill));
        Assert.Empty(result.Unresolved);
        Assert.Equal("Al menos dos de: Movimiento sigiloso, Rastrear", Rebuild(result));
    }

    [Fact]
    public void Conditions_And_Skills_Can_Be_Mixed__FR005()
    {
        var result = Parse("Gung-fu, otra maniobra especial al 101%+ y 2 más al 51%+");

        Assert.Equal(
            [PrerequisiteSegmentKind.Skill, PrerequisiteSegmentKind.Text, PrerequisiteSegmentKind.Condition, PrerequisiteSegmentKind.Text, PrerequisiteSegmentKind.Condition],
            result.Prerequisite.Segments.Select(segment => segment.Kind));
        Assert.Empty(result.Unresolved);
    }

    [Theory]
    [InlineData("Montar criatura terrestre (sin verificar)")]
    [InlineData("?")]
    [InlineData("S")]
    public void Unverified_Requirement_Is_Marked_And_Is_Not_An_Error__FR009(string text)
    {
        var result = Parse(text);

        var segment = Assert.Single(result.Prerequisite.Segments);
        Assert.Equal(new PrerequisiteSegment(text, PrerequisiteSegmentKind.Unverified), segment);
        Assert.True(result.HasUnverified);
        Assert.Empty(result.Unresolved);
    }

    [Fact]
    public void Unknown_Name_Is_Reported_As_Unresolved__FR005()
    {
        var result = Parse("Disfraz");

        Assert.Equal(["Disfraz"], result.Unresolved);
    }

    [Fact]
    public void Every_Unknown_Name_In_A_List_Is_Reported__FR005()
    {
        var result = Parse("Botánica, Disfraz o Religión");

        Assert.Equal(["Disfraz", "Religión"], result.Unresolved);
        Assert.Equal("Botánica, Disfraz o Religión", Rebuild(result));
    }

    [Theory]
    [InlineData("Botánica, Herbalismo")]
    [InlineData("Al menos dos de: Movimiento sigiloso, Rastrear, Disfraz")]
    [InlineData("Sabiduría 14+, Competencia básica en el arma")]
    [InlineData("Montar criatura terrestre (sin verificar)")]
    public void Segments_Rebuild_The_Original_Text__FR020(string text)
    {
        Assert.Equal(text, Rebuild(Parse(text)));
    }
}
