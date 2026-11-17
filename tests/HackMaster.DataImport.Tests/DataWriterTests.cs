using System.Text.Json;
using HackMaster.Domain;

namespace HackMaster.DataImport.Tests;

public sealed class DataWriterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hackmaster-tests-" + Guid.NewGuid().ToString("N"));

    public DataWriterTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static GameModel Model()
    {
        var result = Importer.Import(Sheet.Workbook(
            [
                Sheet.Skill(2, "Zoología"),
                Sheet.Skill(3, "Botánica"),
                Sheet.Skill(4, "Liderazgo básico", die: "5"),
            ],
            suites:
            [
                Sheet.CustomTitle(2, "Herbalista"), Sheet.CustomHeader(3), Sheet.Component(4, "Botánica"),
                Sheet.SectionTitle(10, "Estilos de combate"), Sheet.SectionHeader(11), Sheet.Style(12, "Estilo del Yunque"),
            ],
            doubts: [Sheet.Doubt(2, "Botánica", source: "MJ 100", question: "¿Coste correcto?", provisional: "Se dejó 1 PC.")]));
        Assert.Empty(result.Errors);
        return result.Model!;
    }

    [Fact]
    public void Renders_The_Four_Data_Files_And_The_Doubts_Document__FR001()
    {
        var files = DataWriter.Render(Model());

        Assert.Equal(
            ["doubts.json", "dudas.md", "skills.json", "styles.json", "suites.json"],
            files.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Data_Files_Carry_The_Schema_Version_And_Read_Back_As_The_Same_Model__FR003()
    {
        var model = Model();
        var files = DataWriter.Render(model);

        var skills = JsonSerializer.Deserialize<SkillsFile>(files[DataWriter.SkillsFileName], GameDataJson.Options)!;
        var suites = JsonSerializer.Deserialize<SuitesFile>(files[DataWriter.SuitesFileName], GameDataJson.Options)!;
        var styles = JsonSerializer.Deserialize<StylesFile>(files[DataWriter.StylesFileName], GameDataJson.Options)!;
        var doubts = JsonSerializer.Deserialize<DoubtsFile>(files[DataWriter.DoubtsFileName], GameDataJson.Options)!;

        Assert.Equal(GameDataJson.CurrentSchemaVersion, skills.SchemaVersion);
        Assert.Equal(model.Skills.Select(skill => skill.Name), skills.Skills.Select(skill => skill.Name));
        Assert.Equal("Herbalista", Assert.Single(suites.Suites).Name);
        Assert.Equal(model.Suites[0].Components, suites.Suites[0].Components);
        Assert.Equal("Estilo del Yunque", Assert.Single(styles.Styles).Name);
        Assert.Equal(model.Doubts.Select(doubt => doubt.Id), doubts.Doubts.Select(doubt => doubt.Id));
        var pending = skills.Skills.Single(skill => skill.Slug == "liderazgo-basico");
        Assert.Equal(["masteryDie"], pending.Pending);
        Assert.Equal("5", pending.Raw["masteryDie"]);
    }

    [Fact]
    public void Skills_Are_Written_In_Search_Name_Order__FR010()
    {
        var json = DataWriter.Render(Model())[DataWriter.SkillsFileName];

        Assert.True(json.IndexOf("\"botanica\"", StringComparison.Ordinal) < json.IndexOf("\"liderazgo-basico\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"liderazgo-basico\"", StringComparison.Ordinal) < json.IndexOf("\"zoologia\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Two_Renders_Of_The_Same_Sheet_Are_Identical__FR010()
    {
        var first = DataWriter.Render(Model());
        var second = DataWriter.Render(Model());

        Assert.Equal(first.Keys.Order(), second.Keys.Order());
        Assert.All(first, file => Assert.Equal(file.Value, second[file.Key]));
    }

    [Fact]
    public void Files_Use_Line_Feeds_Readable_Accents_And_A_Final_New_Line__FR010()
    {
        var files = DataWriter.Render(Model());

        Assert.All(files.Values, content => Assert.DoesNotContain('\r', content));
        Assert.All(files.Values, content => Assert.EndsWith("\n", content, StringComparison.Ordinal));
        Assert.Contains("Botánica", files[DataWriter.SkillsFileName]);
        Assert.Contains("\"schemaVersion\": 1", files[DataWriter.SkillsFileName]);
    }

    [Fact]
    public void Doubts_Document_Says_It_Is_Generated_And_Separates_Sheet_From_Detected__FR023()
    {
        var document = DataWriter.Render(Model())[DataWriter.DoubtsDocument];

        Assert.Contains("generado", document);
        var sheetSection = document.IndexOf("## Dudas de la hoja", StringComparison.Ordinal);
        var importSection = document.IndexOf("## Dudas detectadas en la importación", StringComparison.Ordinal);
        Assert.True(sheetSection >= 0 && importSection > sheetSection);
        Assert.InRange(document.IndexOf("¿Coste correcto?", StringComparison.Ordinal), sheetSection, importSection);
        Assert.Contains("Se dejó 1 PC.", document);
        Assert.True(document.IndexOf("Liderazgo básico", StringComparison.Ordinal) > importSection);
    }

    [Fact]
    public void Doubts_Document_Names_Every_Doubt_Subject__FR023()
    {
        var model = Model();
        var document = DataWriter.Render(model)[DataWriter.DoubtsDocument];

        Assert.All(model.Doubts, doubt => Assert.Contains(doubt.Subject, document));
    }

    [Fact]
    public void Write_Creates_Utf8_Files_Without_Bom__FR010()
    {
        var data = Path.Combine(_directory, "data");
        var document = Path.Combine(_directory, "docs", "dudas.md");

        DataWriter.Write(Model(), data, document);

        var rendered = DataWriter.Render(Model());
        foreach (var name in new[] { DataWriter.SkillsFileName, DataWriter.SuitesFileName, DataWriter.StylesFileName, DataWriter.DoubtsFileName })
        {
            var bytes = File.ReadAllBytes(Path.Combine(data, name));
            Assert.NotEqual(0xEF, bytes[0]);
            Assert.Equal(rendered[name], File.ReadAllText(Path.Combine(data, name)));
        }

        Assert.Equal(rendered[DataWriter.DoubtsDocument], File.ReadAllText(document));
    }

    [Fact]
    public void Write_Replaces_Previous_Files__FR001()
    {
        var data = Path.Combine(_directory, "data");
        var document = Path.Combine(_directory, "dudas.md");
        Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data, DataWriter.SkillsFileName), "antiguo");

        DataWriter.Write(Model(), data, document);

        Assert.DoesNotContain("antiguo", File.ReadAllText(Path.Combine(data, DataWriter.SkillsFileName)));
    }

    [Fact]
    public void Write_That_Cannot_Finish_Leaves_The_Destination_Unchanged__FR005()
    {
        var data = Path.Combine(_directory, "data");
        Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data, DataWriter.SkillsFileName), "antiguo");
        var blocked = Path.Combine(_directory, "bloqueado");
        File.WriteAllText(blocked, "es un archivo, no una carpeta");

        Assert.ThrowsAny<IOException>(() => DataWriter.Write(Model(), data, Path.Combine(blocked, "dudas.md")));

        Assert.Equal("antiguo", File.ReadAllText(Path.Combine(data, DataWriter.SkillsFileName)));
        Assert.Single(Directory.GetFiles(data));
    }
}
