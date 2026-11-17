namespace HackMaster.DataImport.Tests;

public sealed class CliTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hackmaster-tests-" + Guid.NewGuid().ToString("N"));
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public CliTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    private string DataDirectory => Path.Combine(_directory, "data");

    private string DoubtsDocument => Path.Combine(_directory, "docs", "dudas.md");

    private string Save(TestWorkbook book)
    {
        var path = Path.Combine(_directory, "hoja.xlsx");
        book.SaveTo(path);
        return path;
    }

    private int Run(params string[] args) => Cli.Run(args, _output, _error);

    [Theory]
    [InlineData]
    [InlineData("solo-uno.xlsx")]
    [InlineData("a.xlsx", "data", "dudas.md", "sobra")]
    public void Wrong_Number_Of_Arguments_Shows_Usage_And_Returns_2__FR001(params string[] args)
    {
        Assert.Equal(Cli.UsageError, Run(args));
        Assert.Contains("Uso:", _error.ToString());
    }

    [Fact]
    public void Missing_Workbook_Returns_2_And_Says_How_To_Point_To_It__FR002()
    {
        var code = Run(Path.Combine(_directory, "no-existe.xlsx"), DataDirectory, DoubtsDocument);

        Assert.Equal(Cli.UsageError, code);
        Assert.Contains("no-existe.xlsx", _error.ToString());
        Assert.Contains("HACKMASTER_EXCEL", _error.ToString());
        Assert.False(Directory.Exists(DataDirectory));
    }

    [Fact]
    public void Workbook_Without_An_Expected_Sheet_Returns_2__FR006()
    {
        using var book = TestWorkbook.Valid();
        book.Descriptions.Delete();

        var code = Run(Save(book), DataDirectory, DoubtsDocument);

        Assert.Equal(Cli.UsageError, code);
        Assert.Contains(SheetNames.Descriptions, _error.ToString());
        Assert.False(Directory.Exists(DataDirectory));
    }

    [Fact]
    public void Data_Errors_Return_1_List_Every_Error_And_Change_No_File__FR005()
    {
        using var book = TestWorkbook.Valid();
        book.AddSkill(4, "Sigilo urbano", requirement: "Disfraz");
        book.AddSkill(5, "Orientación", requirement: "Mapa mental");
        Directory.CreateDirectory(DataDirectory);
        var existing = Path.Combine(DataDirectory, DataWriter.SkillsFileName);
        File.WriteAllText(existing, "antiguo");

        var code = Run(Save(book), DataDirectory, DoubtsDocument);

        Assert.Equal(Cli.DataErrors, code);
        var error = _error.ToString();
        Assert.Contains("La importación ha fallado: 2 errores. No se ha modificado ningún archivo.", error);
        Assert.Contains("[Habilidades, fila 4] «Sigilo urbano»: el requisito «Disfraz»", error);
        Assert.Contains("[Habilidades, fila 5] «Orientación»: el requisito «Mapa mental»", error);
        Assert.Equal("antiguo", File.ReadAllText(existing));
        Assert.False(File.Exists(DoubtsDocument));
    }

    [Fact]
    public void Valid_Workbook_Returns_0_Writes_Files_And_Prints_Counts__FR010()
    {
        using var book = TestWorkbook.Valid();

        var code = Run(Save(book), DataDirectory, DoubtsDocument);

        Assert.Equal(Cli.Success, code);
        Assert.Equal(string.Empty, _error.ToString());
        var output = _output.ToString();
        Assert.Contains("Importación correcta.", output);
        Assert.Contains("Habilidades: 2", output);
        Assert.Contains("Conjuntos de habilidades: 1", output);
        Assert.Contains("Conjuntos oficiales: 0", output);
        Assert.Contains("Estilos de combate: 0", output);
        Assert.Contains("Dudas: 1 de la hoja, 0 detectadas", output);
        Assert.True(File.Exists(Path.Combine(DataDirectory, DataWriter.SkillsFileName)));
        Assert.True(File.Exists(Path.Combine(DataDirectory, DataWriter.SuitesFileName)));
        Assert.True(File.Exists(Path.Combine(DataDirectory, DataWriter.StylesFileName)));
        Assert.True(File.Exists(Path.Combine(DataDirectory, DataWriter.DoubtsFileName)));
        Assert.True(File.Exists(DoubtsDocument));
    }

    [Fact]
    public void Warnings_Are_Listed_After_The_Counts__FR010()
    {
        using var book = TestWorkbook.Valid();
        book.Skills.Cell(3, 5).Value = 5;

        var code = Run(Save(book), DataDirectory, DoubtsDocument);

        Assert.Equal(Cli.Success, code);
        var output = _output.ToString();
        Assert.Contains("Avisos (1):", output);
        Assert.Contains("[Habilidades, fila 3] «Herbalismo»:", output);
        Assert.Contains("Dudas: 1 de la hoja, 1 detectadas", output);
    }

    [Fact]
    public void Importing_The_Same_Workbook_Twice_Gives_Identical_Files__FR010()
    {
        using var book = TestWorkbook.Valid();
        var path = Save(book);

        Run(path, DataDirectory, DoubtsDocument);
        var first = Directory.GetFiles(DataDirectory).Order().Select(File.ReadAllBytes).ToList();
        Run(path, DataDirectory, DoubtsDocument);
        var second = Directory.GetFiles(DataDirectory).Order().Select(File.ReadAllBytes).ToList();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Generated_Files_Never_Contain_The_Manual_Text__FR004()
    {
        using var book = TestWorkbook.Valid();

        Run(Save(book), DataDirectory, DoubtsDocument);

        var written = Directory.GetFiles(DataDirectory).Append(DoubtsDocument).Select(File.ReadAllText);
        Assert.All(written, content => Assert.DoesNotContain(TestWorkbook.ManualTextSentinel, content));
    }
}
