using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>Nombres de las pestañas de la hoja de cálculo fuente.</summary>
public static class SheetNames
{
    public const string Skills = "Habilidades";
    public const string Suites = "Conjuntos de habilidades";
    public const string Doubts = "Dudas";
    public const string Descriptions = "Descripciones";
}

/// <summary>Fila de la hoja tal como se lee: pestaña, número de fila real y celdas como texto.</summary>
public sealed record RawRow(string Sheet, int RowNumber, IReadOnlyDictionary<string, string> Cells)
{
    /// <summary>Texto de la celda de la columna indicada (A, B…), o vacío si no tiene valor.</summary>
    public string this[string column] => Cells.TryGetValue(column, out var value) ? value.Trim() : string.Empty;
}

/// <summary>
/// Contenido en bruto de la hoja. Las filas de cabecera de "Habilidades", "Descripciones" y "Dudas"
/// no se incluyen; "Conjuntos de habilidades" no tiene cabecera propia y va completa.
/// </summary>
public sealed record RawWorkbook(
    IReadOnlyList<RawRow> Skills,
    IReadOnlyList<RawRow> Descriptions,
    IReadOnlyList<RawRow> Suites,
    IReadOnlyList<RawRow> Doubts);

/// <summary>Resultado de leer la hoja: su contenido, o los errores de estructura.</summary>
public sealed record WorkbookReadResult(RawWorkbook? Workbook, IReadOnlyList<ImportMessage> Errors);

/// <summary>Datos de juego ya interpretados.</summary>
public sealed record GameModel(
    IReadOnlyList<Skill> Skills,
    IReadOnlyList<SkillSuite> Suites,
    IReadOnlyList<CombatStyle> Styles,
    IReadOnlyList<Doubt> Doubts);

/// <summary>Resultado de la importación. Si hay errores no hay modelo.</summary>
public sealed record ImportResult(
    GameModel? Model,
    IReadOnlyList<ImportMessage> Errors,
    IReadOnlyList<ImportMessage> Warnings);
