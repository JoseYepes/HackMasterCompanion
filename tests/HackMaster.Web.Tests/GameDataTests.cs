using System.Text.Json;
using HackMaster.Domain;

namespace HackMaster.Web.Tests;

public class GameDataTests
{
    [Fact]
    public void Embedded_Data_Files_Load_Without_Error__FR003()
    {
        var data = GameData.LoadEmbedded();

        Assert.Null(data.LoadError);
    }

    [Fact]
    public void Finds_Each_Element_By_Kind_And_Slug__FR019()
    {
        var data = TestData.Create();

        Assert.Equal("Botánica", data.FindSkill("botanica")?.Name);
        Assert.Equal("Herbalista", data.FindSuite(SuiteKind.Custom, "herbalista")?.Name);
        Assert.Equal("Cartógrafo", data.FindSuite(SuiteKind.Official, "cartografo")?.Name);
        Assert.Null(data.FindSuite(SuiteKind.Official, "herbalista"));
        Assert.NotSame(data.FindSuite(SuiteKind.Custom, "cartografo"), data.FindSuite(SuiteKind.Official, "cartografo"));
        Assert.Equal(SuiteKind.Custom, data.FindSuite(SuiteKind.Custom, "cartografo")?.Kind);
        Assert.Equal("Estilo del Yunque", data.FindStyle("estilo-del-yunque")?.Name);
        Assert.Null(data.FindSkill("no-existe"));
    }

    [Fact]
    public void Knows_The_Suites_That_Contain_A_Skill__FR019()
    {
        var data = TestData.Create();

        Assert.Equal(["Herbalista"], data.SuitesContaining("botanica").Select(suite => suite.Name));
        Assert.Empty(data.SuitesContaining("karate"));
    }

    [Fact]
    public void Knows_The_Doubts_Of_An_Element__FR019()
    {
        var data = TestData.Create();

        Assert.Equal(["sheet-skill-cerrajeria-1"], data.DoubtsFor(ElementKind.Skill, "cerrajeria").Select(doubt => doubt.Id));
        Assert.Empty(data.DoubtsFor(ElementKind.Skill, "botanica"));
    }

    [Fact]
    public void Filter_Options_Come_From_The_Loaded_Skills__FR017()
    {
        var data = TestData.Create();

        Assert.Contains("Artes marciales", data.SkillFacets.Types);
        Assert.Contains("GMG", data.SkillFacets.Books);
    }

    [Fact]
    public void Json_Files_Are_Read_Back_As_The_Same_Data__FR003()
    {
        var data = GameData.FromJson(
            Serialize(new SkillsFile(GameDataJson.CurrentSchemaVersion, TestData.Skills)),
            Serialize(new SuitesFile(GameDataJson.CurrentSchemaVersion, TestData.Suites)),
            Serialize(new StylesFile(GameDataJson.CurrentSchemaVersion, TestData.Styles)),
            Serialize(new DoubtsFile(GameDataJson.CurrentSchemaVersion, TestData.Doubts)));

        Assert.Null(data.LoadError);
        Assert.Equal(TestData.Skills.Count, data.Skills.Count);
        Assert.Equal(TestData.Suites.Count, data.Suites.Count);
        Assert.Single(data.Styles);
        Assert.Equal(TestData.Doubts.Count, data.Doubts.Count);
    }

    [Fact]
    public void Unexpected_Schema_Version_Gives_A_Clear_Error_And_No_Data__FR003()
    {
        var data = GameData.FromJson(
            Serialize(new SkillsFile(2, TestData.Skills)),
            Serialize(new SuitesFile(GameDataJson.CurrentSchemaVersion, TestData.Suites)),
            Serialize(new StylesFile(GameDataJson.CurrentSchemaVersion, TestData.Styles)),
            Serialize(new DoubtsFile(GameDataJson.CurrentSchemaVersion, TestData.Doubts)));

        Assert.NotNull(data.LoadError);
        Assert.Contains("versión", data.LoadError);
        Assert.Contains("skills.json", data.LoadError);
        Assert.Empty(data.Skills);
        Assert.Empty(data.Suites);
    }

    [Fact]
    public void Unreadable_File_Gives_A_Clear_Error__FR003()
    {
        var data = GameData.FromJson("no es json", "{}", "{}", "{}");

        Assert.NotNull(data.LoadError);
        Assert.Empty(data.Skills);
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, GameDataJson.Options);
}
