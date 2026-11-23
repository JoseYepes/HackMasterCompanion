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
    public void Compound_Schools_Stay_Whole_And_Commas_Separate__Review_MJ124()
    {
        var spell = Assert.Single(Import(Spell(2, schools: "Encantamiento/Hechizo, Alteración")).File!.Spells);

        Assert.Equal(["Encantamiento/Hechizo", "Alteración"], spell.Schools);
    }

    [Theory]
    [InlineData("Reducir", true, "Reducir")]
    [InlineData("Sí", true, null)]
    [InlineData("si", true, null)]
    [InlineData("", false, null)]
    public void Reversible_Is_Yes_Or_The_Name_Of_The_Reverse__Review(string cell, bool reversible, string? reverse)
    {
        var spell = Assert.Single(Import(Spell(2, reverse: cell)).File!.Spells);

        Assert.Equal((reversible, reverse), (spell.Reversible, spell.Reverse));
    }

    [Fact]
    public void A_Pending_Reverse_Or_School_Is_Marked__Review()
    {
        var spell = Assert.Single(Import(Spell(2, reverse: "?", schools: "?")).File!.Spells);

        Assert.Equal(["schools", "reverse"], spell.Pending);
        Assert.Empty(spell.Schools);
    }

    [Theory]
    [InlineData("?", "Mago", "1", "MJ 175")]
    [InlineData("Agrandar", "?", "1", "MJ 175")]
    [InlineData("Agrandar", "Mago", "?", "MJ 175")]
    [InlineData("Agrandar", "Mago", "1", "?")]
    [InlineData("¡¿!", "Mago", "1", "MJ 175")]
    public void Required_Data_Cannot_Be_Pending_Or_Unusable__Review(string name, string spellClass, string level, string book)
    {
        var result = Import(Spell(2, name: name, spellClass: spellClass, level: level, book: book));

        Assert.Null(result.File);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Two_Names_With_The_Same_Address_In_The_Same_Class_Fail__Review()
    {
        var result = Import(Spell(2, name: "Luz"), Spell(3, name: "Luz."));

        Assert.Null(result.File);
        Assert.Contains(result.Errors, error => error.Rows.SequenceEqual([2, 3]));
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

        // El cuarto argumento lleva el listado de dudas a la carpeta temporal (FR-009); sin él iría a docs/.
        var code = Cli.Run(["conjuros", path, Path.Combine(_directory, "data"), Path.Combine(_directory, "dudas-conjuros.md")], _output, _error);

        Assert.Equal(Cli.Success, code);
        var json = File.ReadAllText(Path.Combine(_directory, "data", "spells.json"));
        Assert.Contains("\"name\": \"Agrandar\"", json, StringComparison.Ordinal);
        Assert.Contains("Conjuros: 1", _output.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(_directory, "data", "skills.json")));
    }

    private static RawRow Doubt(int row, string spell = "Agrandar", string spellClass = "Mago", string source = "MJ 175", string question = "La cabecera no da la escuela.", string provisional = "Se deja pendiente.") =>
        Sheet.Row(SpellSheetNames.Doubts, row, ("A", spell), ("B", spellClass), ("C", source), ("D", question), ("E", provisional));

    [Fact]
    public void A_Doubt_Row_Points_To_Its_Spell__FR009()
    {
        var result = SpellImporter.Import([Spell(2)], [Rule(2)], [Doubt(2)]);

        Assert.Empty(result.Errors);
        var doubt = Assert.Single(result.File!.Doubts!);
        Assert.Equal(("Agrandar", SpellClass.Mage, "agrandar"), (doubt.Subject, doubt.Class, doubt.Slug));
        Assert.Equal(("MJ 175", "La cabecera no da la escuela.", "Se deja pendiente."), (doubt.Source, doubt.Question, doubt.Provisional));
    }

    [Fact]
    public void A_Doubt_About_A_Spell_That_Does_Not_Exist_Fails__FR009()
    {
        var result = SpellImporter.Import([Spell(2)], [Rule(2)], [Doubt(2, spell: "Inexistente"), Doubt(3, spellClass: "Clérigo")]);

        Assert.Null(result.File);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("«Inexistente»", result.Errors[0].ToString(), StringComparison.Ordinal);
        Assert.Contains("fila 3", result.Errors[1].ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Magoo", "Agrandar", "la clase «Magoo» no es")]
    [InlineData("", "Agrandar", "falta la clase")]
    [InlineData("Mago", "", "una duda general no lleva clase")]
    public void A_Doubt_With_A_Wrong_Or_Missing_Class_Fails_And_Says_Why__FR009(string spellClass, string spell, string message)
    {
        var result = SpellImporter.Import([Spell(2)], [Rule(2)], [Doubt(2, spell: spell, spellClass: spellClass)]);

        Assert.Null(result.File);
        Assert.Contains(message, Assert.Single(result.Errors).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_Command_Rejects_Too_Many_Arguments_And_An_Unwritable_Doubts_Path__FR009()
    {
        var tooMany = Cli.Run(["conjuros", "a.xlsx", "data", "dudas.md", "sobra"], _output, _error);
        var folder = Cli.Run(["conjuros", "a.xlsx", "data", _directory], _output, _error);

        Assert.Equal(Cli.UsageError, tooMany);
        Assert.Equal(Cli.UsageError, folder);
        Assert.Contains("es una carpeta", _error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_Doubt_Without_Spell_Is_General_And_One_Without_Question_Fails__FR009()
    {
        var general = SpellImporter.Import([Spell(2)], [Rule(2)], [Doubt(2, spell: "", spellClass: "")]);
        var empty = SpellImporter.Import([Spell(2)], [Rule(2)], [Doubt(2, question: "")]);

        var doubt = Assert.Single(general.File!.Doubts!);
        Assert.Null(doubt.Class);
        Assert.Null(doubt.Slug);
        Assert.Null(empty.File);
    }

    [Fact]
    public void The_Command_Reads_The_Doubts_Tab_And_Writes_The_Doubts_Document__FR009()
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
            var doubts = workbook.AddWorksheet(SpellSheetNames.Doubts);
            TestWorkbook.SetRow(doubts, 1, "Conjuro", "Clase", "Fuente", "Duda", "Decisión provisional");
            TestWorkbook.SetRow(doubts, 2, "Agrandar", "Mago", "MJ 175", "La cabecera no da la escuela.", "Se deja pendiente.");
            workbook.SaveAs(path);
        }

        var document = Path.Combine(_directory, "dudas-conjuros.md");
        var code = Cli.Run(["conjuros", path, Path.Combine(_directory, "data"), document], _output, _error);

        Assert.Equal(Cli.Success, code);
        Assert.Contains("\"question\": \"La cabecera no da la escuela.\"", File.ReadAllText(Path.Combine(_directory, "data", "spells.json")), StringComparison.Ordinal);
        var text = File.ReadAllText(document);
        Assert.Contains("### Agrandar (mago)", text, StringComparison.Ordinal);
        Assert.Contains("- **Decisión provisional**: Se deja pendiente.", text, StringComparison.Ordinal);
        Assert.Contains("Dudas: 1", _output.ToString(), StringComparison.Ordinal);
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
