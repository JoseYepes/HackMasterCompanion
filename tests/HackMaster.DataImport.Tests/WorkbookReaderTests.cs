namespace HackMaster.DataImport.Tests;

public class WorkbookReaderTests
{
    private static WorkbookReadResult Read(TestWorkbook book)
    {
        using var stream = book.ToStream();
        return WorkbookReader.Read(stream);
    }

    [Fact]
    public void Reads_The_Four_Sheets_With_Their_Real_Row_Numbers__FR001()
    {
        using var book = TestWorkbook.Valid();

        var result = Read(book);

        Assert.Empty(result.Errors);
        var workbook = result.Workbook!;
        Assert.Equal([2, 3], workbook.Skills.Select(row => row.RowNumber));
        Assert.Equal("Botánica", workbook.Skills[0]["A"]);
        Assert.Equal("Herbalismo", workbook.Skills[1]["A"]);
        Assert.Equal("Botánica", workbook.Skills[1]["F"]);
        Assert.Equal([2, 3], workbook.Descriptions.Select(row => row.RowNumber));
        Assert.Equal("Resumen de Botánica.", workbook.Descriptions[0]["C"]);
        Assert.Equal([2, 3, 4, 5], workbook.Suites.Select(row => row.RowNumber));
        Assert.Equal("Herbalista", workbook.Suites[0]["B"]);
        Assert.Equal(2, Assert.Single(workbook.Doubts).RowNumber);
        Assert.Equal(SheetNames.Skills, workbook.Skills[0].Sheet);
    }

    [Fact]
    public void Header_Rows_Are_Not_Returned_As_Data__FR001()
    {
        using var book = TestWorkbook.Valid();

        var workbook = Read(book).Workbook!;

        Assert.DoesNotContain(workbook.Skills, row => row["A"] == "HABILIDADES");
        Assert.DoesNotContain(workbook.Descriptions, row => row["A"] == "Habilidad");
        Assert.DoesNotContain(workbook.Doubts, row => row["A"] == "Habilidad");
    }

    [Fact]
    public void Numbers_Are_Read_As_Invariant_Text__FR008()
    {
        using var book = TestWorkbook.Valid();
        book.AddSkill(4, "Cerrajería", difficulty: 0.1);
        book.AddSkill(5, "Colocar trampas avanzadas", difficulty: -0.05);

        var workbook = Read(book).Workbook!;

        Assert.Equal("1", workbook.Skills[0]["C"]);
        Assert.Equal("250", workbook.Skills[0]["G"]);
        Assert.Equal("0.1", workbook.Skills[2]["H"]);
        Assert.Equal("-0.05", workbook.Skills[3]["H"]);
    }

    [Fact]
    public void Number_Shown_As_Die_Is_Read_As_Its_Number_Not_As_A_Date__FR008()
    {
        // La hoja real guarda el dado como el número 6 con el formato «\dGeneral», que se ve «d6».
        // Ese formato contiene una «d» y la librería lo toma por una fecha (6 de enero de 1900).
        using var book = TestWorkbook.Valid();
        var cell = book.Skills.Cell(2, 5);
        cell.Value = 6;
        cell.Style.NumberFormat.Format = @"\dGeneral";

        var workbook = Read(book).Workbook!;

        Assert.Equal("6", workbook.Skills[0]["E"]);
    }

    [Fact]
    public void Formula_Cell_Gives_Its_Calculated_Value__FR001()
    {
        using var book = TestWorkbook.Valid();
        book.Suites.Cell(2, 3).FormulaA1 = "SUM(C4:C5)";

        var workbook = Read(book).Workbook!;

        Assert.Equal("7", workbook.Suites[0]["C"]);
    }

    [Fact]
    public void Empty_Rows_Are_Skipped_And_Numbering_Is_Preserved__FR001()
    {
        using var book = TestWorkbook.Valid();
        book.AddSkill(9, "Zoología");

        var workbook = Read(book).Workbook!;

        Assert.Equal([2, 3, 9], workbook.Skills.Select(row => row.RowNumber));
    }

    [Fact]
    public void Missing_Sheet_Is_A_Structure_Error__FR006()
    {
        using var book = TestWorkbook.Valid();
        book.Doubts.Delete();

        var result = Read(book);

        Assert.Null(result.Workbook);
        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Doubts, error.Sheet);
    }

    [Fact]
    public void Missing_Expected_Column_Is_A_Structure_Error__FR006()
    {
        using var book = TestWorkbook.Valid();
        book.Skills.Cell(1, 7).Value = "Otra cosa";

        var result = Read(book);

        Assert.Null(result.Workbook);
        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Skills, error.Sheet);
        Assert.Contains("Precio", error.Text);
    }

    [Fact]
    public void Manual_Text_Column_Is_Never_Read__FR004()
    {
        using var book = TestWorkbook.Valid();

        var workbook = Read(book).Workbook!;

        var everyCell = workbook.Skills.Concat(workbook.Descriptions).Concat(workbook.Suites).Concat(workbook.Doubts)
            .SelectMany(row => row.Cells.Values);
        Assert.DoesNotContain(everyCell, value => value.Contains(TestWorkbook.ManualTextSentinel, StringComparison.Ordinal));
        Assert.All(workbook.Descriptions, row => Assert.DoesNotContain("D", row.Cells.Keys));
    }
}
