using System.Globalization;
using ClosedXML.Excel;
using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>
/// Lee la hoja de cálculo y devuelve sus filas en bruto. De la pestaña "Descripciones" solo se leen
/// las columnas A a C: la columna D (texto literal del manual) no se abre nunca.
/// </summary>
public static class WorkbookReader
{
    private sealed record ExpectedColumn(string Letter, string Prefix, string Label);

    private static readonly ExpectedColumn[] SkillColumns =
    [
        new("A", "habilidad", "Habilidades"),
        new("B", "tipo", "Tipo"),
        new("C", "pc", "PC"),
        new("D", "caracteristica", "Característica relevante"),
        new("E", "dado", "Dado de maestría"),
        new("F", "requisito", "Requisito"),
        new("G", "precio", "Precio"),
        new("H", "dificultad", "Dificultad"),
        new("I", "libro", "Libro"),
        new("J", "clases", "Clases permitidas"),
    ];

    private static readonly ExpectedColumn[] DescriptionColumns =
    [
        new("A", "habilidad", "Habilidad"),
        new("C", "descripcion", "Descripción (resumen propio)"),
    ];

    private static readonly ExpectedColumn[] DoubtColumns =
    [
        new("A", "habilidad", "Habilidad"),
        new("B", "fuente", "Fuente"),
        new("C", "duda", "Duda"),
        new("D", "que", "Qué he hecho"),
    ];

    public static WorkbookReadResult Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var errors = new List<ImportMessage>();

        var skills = WithoutHeader(ReadSheet(workbook, SheetNames.Skills, lastColumn: 10, errors), SheetNames.Skills, SkillColumns, errors);
        var descriptions = WithoutHeader(ReadSheet(workbook, SheetNames.Descriptions, lastColumn: 3, errors), SheetNames.Descriptions, DescriptionColumns, errors);
        var suites = ReadSheet(workbook, SheetNames.Suites, lastColumn: 10, errors);
        var doubts = WithoutHeader(ReadSheet(workbook, SheetNames.Doubts, lastColumn: 4, errors), SheetNames.Doubts, DoubtColumns, errors);

        if (errors.Count > 0 || skills is null || descriptions is null || suites is null || doubts is null)
        {
            return new WorkbookReadResult(null, errors);
        }

        return new WorkbookReadResult(new RawWorkbook(skills, descriptions, suites, doubts), errors);
    }

    private static List<RawRow>? ReadSheet(XLWorkbook workbook, string name, int lastColumn, List<ImportMessage> errors)
    {
        if (!workbook.TryGetWorksheet(name, out var sheet))
        {
            errors.Add(new ImportMessage(name, [], null, "falta la pestaña en la hoja de cálculo."));
            return null;
        }

        var rows = new List<RawRow>();
        foreach (var row in sheet.RowsUsed())
        {
            var cells = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var column = 1; column <= lastColumn; column++)
            {
                var text = CellText(row.Cell(column));
                if (text.Length > 0)
                {
                    cells[((char)('A' + column - 1)).ToString()] = text;
                }
            }

            if (cells.Count > 0)
            {
                rows.Add(new RawRow(name, row.RowNumber(), cells));
            }
        }

        return rows;
    }

    private static List<RawRow>? WithoutHeader(List<RawRow>? rows, string sheet, ExpectedColumn[] expected, List<ImportMessage> errors)
    {
        if (rows is null)
        {
            return null;
        }

        if (rows.Count == 0)
        {
            errors.Add(new ImportMessage(sheet, [], null, "la pestaña está vacía; falta la fila de cabecera."));
            return null;
        }

        var header = rows[0];
        foreach (var column in expected)
        {
            if (!TextNormalizer.Normalize(header[column.Letter]).StartsWith(column.Prefix, StringComparison.Ordinal))
            {
                errors.Add(new ImportMessage(
                    sheet,
                    header.RowNumber,
                    null,
                    $"falta la columna «{column.Label}» (se esperaba en la columna {column.Letter})."));
            }
        }

        return rows.GetRange(1, rows.Count - 1);
    }

    /// <summary>
    /// Número que guarda la hoja para una fecha. Excel cuenta el 29 de febrero de 1900, que no existió,
    /// así que antes del 1 de marzo de 1900 su número es uno menos que el de .NET.
    /// </summary>
    private static double ExcelSerial(DateTime date)
    {
        var serial = date.ToOADate();
        return date < new DateTime(1900, 3, 1) ? serial - 1 : serial;
    }

    /// <summary>Texto de una celda. De una fórmula se toma el valor guardado, sin recalcular.</summary>
    private static string CellText(IXLCell cell)
    {
        var value = cell.HasFormula ? cell.CachedValue : cell.Value;

        if (value.IsBlank)
        {
            return string.Empty;
        }

        if (value.IsText)
        {
            return value.GetText().Trim();
        }

        // La hoja no tiene fechas: un número con un formato que contiene «d» (como «\dGeneral», que
        // muestra «d6») se lee como fecha. Se recupera el número que guarda la celda.
        if (value.IsDateTime)
        {
            return ExcelSerial(value.GetDateTime()).ToString(CultureInfo.InvariantCulture);
        }

        return value.IsNumber
            ? value.GetNumber().ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture).Trim();
    }
}
