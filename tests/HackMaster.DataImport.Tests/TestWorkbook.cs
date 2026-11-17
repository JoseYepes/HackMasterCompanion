using ClosedXML.Excel;

namespace HackMaster.DataImport.Tests;

/// <summary>Crea en memoria una hoja de cálculo mínima con la estructura de la real.</summary>
internal sealed class TestWorkbook : IDisposable
{
    public const string ManualTextSentinel = "TEXTO-LITERAL-DE-MANUAL-QUE-NO-DEBE-SALIR";

    public XLWorkbook Workbook { get; } = new();

    public IXLWorksheet Skills { get; }

    public IXLWorksheet Suites { get; }

    public IXLWorksheet Doubts { get; }

    public IXLWorksheet Descriptions { get; }

    public TestWorkbook()
    {
        Skills = Workbook.AddWorksheet(SheetNames.Skills);
        Suites = Workbook.AddWorksheet(SheetNames.Suites);
        Doubts = Workbook.AddWorksheet(SheetNames.Doubts);
        Descriptions = Workbook.AddWorksheet(SheetNames.Descriptions);

        SetRow(Skills, 1, "HABILIDADES", "Tipo", "PC", "Caracteristica Relevante", "dado de maestria", "Requisito?", "Precio", "Dificultad", "Libro", "Clases permitidas");
        SetRow(Doubts, 1, "Habilidad", "Fuente", "Duda", "Qué he hecho");
        SetRow(Descriptions, 1, "Habilidad", "Libro", "Descripción (resumen propio)", "Texto del manual (pégalo aquí)");
    }

    /// <summary>Libro válido con dos habilidades, un conjunto y una duda.</summary>
    public static TestWorkbook Valid()
    {
        var book = new TestWorkbook();
        book.AddSkill(2, "Botánica", requirement: "N");
        book.AddSkill(3, "Herbalismo", requirement: "Botánica");
        SetRow(book.Suites, 2, null, "Herbalista", 7, "INT", null, "Las de las habilidades que lo componen", 550, 15, "HJ 4.05", "Todas");
        SetRow(book.Suites, 3, null, "Habilidad", "PC", "Caracteristica Relevante", "dado de maestria", "Requisito?", "Precio", "Dificultad");
        SetRow(book.Suites, 4, null, "Botánica", 1, "INT", 8, "N", 250, 10);
        SetRow(book.Suites, 5, null, "Herbalismo", 6, "INT", 6, "Botánica", 250, 5);
        SetRow(book.Doubts, 2, "Herbalismo", "MJ 100", "El coste no está claro.", "Se dejó 6 PC.");
        return book;
    }

    public void AddSkill(int row, string name, string requirement = "N", object? difficulty = null)
    {
        SetRow(Skills, row, name, "Académicas", 1, "INT", 8, requirement, 250, difficulty ?? 10, "MJ 100", "Todas");
        SetRow(Descriptions, row, name, "MJ 100", $"Resumen de {name}.", ManualTextSentinel);
    }

    public static void SetRow(IXLWorksheet sheet, int row, params object?[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            var cell = sheet.Cell(row, index + 1);
            switch (values[index])
            {
                case null:
                    break;
                case int number:
                    cell.Value = number;
                    break;
                case double number:
                    cell.Value = number;
                    break;
                default:
                    cell.Value = values[index]!.ToString();
                    break;
            }
        }
    }

    public MemoryStream ToStream()
    {
        var stream = new MemoryStream();
        Workbook.SaveAs(stream, new SaveOptions { EvaluateFormulasBeforeSaving = true });
        stream.Position = 0;
        return stream;
    }

    public void SaveTo(string path)
    {
        using var stream = ToStream();
        File.WriteAllBytes(path, stream.ToArray());
    }

    public void Dispose() => Workbook.Dispose();
}
