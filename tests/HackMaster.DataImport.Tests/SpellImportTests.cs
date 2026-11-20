using ClosedXML.Excel;
using HackMaster.Domain;

namespace HackMaster.DataImport.Tests;

/// <summary>Importación de los conjuros desde Hackconjuros.xlsx (F009, FR-006 y FR-007).</summary>
public sealed class SpellImportTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hackmaster-spells-" + Guid.NewGuid().ToString("N"));
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public SpellImportTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    private static RawRow Spell(
        int row,
        string name = "Agrandar",
        string spellClass = "Mago",
        string level = "1",
        string schools = "Alteración",
        string reverse = "Reducir",
        string range = "5 yardas por nivel",
        string components = "V, S, M",
        string duration = "5 asaltos por nivel",
        string castingTime = "1 segmento",
        string area = "Una criatura u objeto",
        string savingThrow = "Niega",
        string book = "MJ 175",
        string summary = "Hace más grande a una criatura u objeto.") =>
        Sheet.Row(SpellSheetNames.Spells, row, ("A", name), ("B", spellClass), ("C", level), ("D", schools), ("E", reverse), ("F", range), ("G", components),
            ("H", duration), ("I", castingTime), ("J", area), ("K", savingThrow), ("L", book), ("M", summary));

    private static RawRow Rule(int row, string title = "Libros de conjuros", string summary = "Resumen propio de la regla.", string book = "MJ 123") =>
        Sheet.Row(SpellSheetNames.Rules, row, ("A", title), ("B", summary), ("C", book));

    private static SpellImportResult Import(params RawRow[] spells) => SpellImporter.Import(spells, [Rule(2)]);

    [Fact]
    public void A_Complete_Row_Becomes_A_Spell_With_All_Its_Fields__FR006()
    {
        var result = Import(Spell(2));

        Assert.Empty(result.Errors);
        var spell = Assert.Single(result.File!.Spells);
        Assert.Equal(("agrandar", "Agrandar", "agrandar", SpellClass.Mage, 1), (spell.Slug, spell.Name, spell.SearchName, spell.Class, spell.Level));
        Assert.Equal(["Alteración"], spell.Schools);
        Assert.Equal(("Reducir", "5 yardas por nivel", "V, S, M", "5 asaltos por nivel"), (spell.Reverse, spell.Range, spell.Components, spell.Duration));
        Assert.Equal(("1 segmento", "Una criatura u objeto", "Niega"), (spell.CastingTime, spell.AreaOfEffect, spell.SavingThrow));
        Assert.Equal([new BookRef("MJ", "175")], spell.Books);
        Assert.Equal("Hace más grande a una criatura u objeto.", spell.Summary);
        Assert.Empty(spell.Pending);
        var rule = Assert.Single(result.File.Rules);
        Assert.Equal(("Libros de conjuros", "Resumen propio de la regla."), (rule.Title, rule.Summary));
        Assert.Equal([new BookRef("MJ", "123")], rule.Books);
    }

    [Fact]
    public void Several_Schools_And_Books_Are_Split__FR006()
    {
        var spell = Assert.Single(Import(Spell(2, schools: "Abjuración, Evocación", book: "MJ 175; HM-FullErrata 12")).File!.Spells);

        Assert.Equal(["Abjuración", "Evocación"], spell.Schools);
        Assert.Equal([new BookRef("MJ", "175"), new BookRef("HM-FullErrata", "12")], spell.Books);
    }

    [Theory]
    [InlineData("Clérigo", SpellClass.Cleric)]
    [InlineData("clerigo", SpellClass.Cleric)]
    [InlineData("MAGO", SpellClass.Mage)]
    public void The_Class_Is_Read_Without_Accents_Or_Capitals__FR006(string text, SpellClass expected)
    {
        Assert.Equal(expected, Assert.Single(Import(Spell(2, spellClass: text)).File!.Spells).Class);
    }

    [Fact]
    public void A_Question_Mark_Leaves_The_Field_Pending__Constitucion_I()
    {
        var spell = Assert.Single(Import(Spell(2, range: "?", savingThrow: "?")).File!.Spells);

        Assert.Null(spell.Range);
        Assert.Null(spell.SavingThrow);
        Assert.Equal(["range", "savingThrow"], spell.Pending);
    }

    [Fact]
    public void A_Spell_Without_Summary_Is_Pending_And_Warned__Constitucion_I()
    {
        var result = Import(Spell(2, summary: ""));

        Assert.Equal(["summary"], Assert.Single(result.File!.Spells).Pending);
        Assert.Contains(result.Warnings, warning => warning.Text.Contains("resumen", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("", "Mago", "1", "MJ 175", "falta el nombre")]
    [InlineData("Agrandar", "Druida", "1", "MJ 175", "clase")]
    [InlineData("Agrandar", "Mago", "uno", "MJ 175", "nivel")]
    [InlineData("Agrandar", "Mago", "10", "MJ 175", "nivel")]
    [InlineData("Agrandar", "Clérigo", "0", "MJ 175", "nivel")]
    [InlineData("Agrandar", "Clérigo", "8", "MJ 175", "nivel")]
    [InlineData("Agrandar", "Mago", "1", "", "libro")]
    [InlineData("Agrandar", "Mago", "1", "MJ", "libro")]
    public void A_Row_Without_Required_Data_Fails_The_Import__FR007(string name, string spellClass, string level, string book, string message)
    {
        var result = Import(Spell(2, name: name, spellClass: spellClass, level: level, book: book));

        Assert.Null(result.File);
        Assert.Contains(result.Errors, error => error.Text.Contains(message, StringComparison.Ordinal) && error.Rows.SequenceEqual([2]));
    }

    [Fact]
    public void Two_Spells_With_The_Same_Name_In_The_Same_Class_Fail__FR007()
    {
        var result = Import(Spell(2), Spell(3, name: "agrandar"));

        Assert.Null(result.File);
        Assert.Contains(result.Errors, error => error.Rows.SequenceEqual([2, 3]));
    }

    [Fact]
    public void The_Same_Name_In_Mage_And_Cleric_Is_Allowed__Spec()
    {
        var result = Import(Spell(2), Spell(3, spellClass: "Clérigo"));

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.File!.Spells.Count);
    }

    [Fact]
    public void A_Rule_Without_Summary_Or_Book_Fails__FR005()
    {
        var result = SpellImporter.Import([Spell(2)], [Rule(2, summary: ""), Rule(3, book: "")]);

        Assert.Null(result.File);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void The_Command_Imports_The_Workbook_And_Writes_Spells_Json__FR006()
    {
        var path = Path.Combine(_directory, "conjuros.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var spells = workbook.AddWorksheet(SpellSheetNames.Spells);
            TestWorkbook.SetRow(spells, 1, "Nombre", "Clase", "Nivel", "Escuela o esfera", "Reversible", "Alcance", "Componentes", "Duración", "Tiempo de lanzamiento", "Área de efecto", "Tirada de salvación", "Libro", "Resumen propio");
            TestWorkbook.SetRow(spells, 2, "Agrandar", "Mago", 1, "Alteración", "Reducir", "5 yardas por nivel", "V, S, M", "5 asaltos por nivel", "1 segmento", "Una criatura u objeto", "Niega", "MJ 175", "Hace más grande a una criatura u objeto.");
            var rules = workbook.AddWorksheet(SpellSheetNames.Rules);
            TestWorkbook.SetRow(rules, 1, "Regla", "Resumen propio", "Libro");
            TestWorkbook.SetRow(rules, 2, "Libros de conjuros", "Resumen.", "MJ 123");
            workbook.SaveAs(path);
        }

        var code = Cli.Run(["conjuros", path, Path.Combine(_directory, "data")], _output, _error);

        Assert.Equal(Cli.Success, code);
        var json = File.ReadAllText(Path.Combine(_directory, "data", "spells.json"));
        Assert.Contains("\"name\": \"Agrandar\"", json, StringComparison.Ordinal);
        Assert.Contains("Conjuros: 1", _output.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_directory, "data", "skills.json")));
    }

    [Fact]
    public void A_Workbook_Without_The_Expected_Columns_Is_Rejected__FR006()
    {
        var path = Path.Combine(_directory, "mal.xlsx");
        using (var workbook = new XLWorkbook())
        {
            TestWorkbook.SetRow(workbook.AddWorksheet(SpellSheetNames.Spells), 1, "Otra cosa");
            workbook.AddWorksheet(SpellSheetNames.Rules);
            workbook.SaveAs(path);
        }

        var code = Cli.Run(["conjuros", path, Path.Combine(_directory, "data")], _output, _error);

        Assert.Equal(Cli.UsageError, code);
        Assert.Contains("Nombre", _error.ToString(), StringComparison.Ordinal);
    }
}
