namespace HackMaster.DataImport.Tests;

/// <summary>Construye filas en bruto como las que entrega el lector de la hoja.</summary>
internal static class Sheet
{
    public const string SuiteRequirementText = "Las de las habilidades que lo componen";

    public static RawRow Row(string sheet, int row, params (string Column, string Value)[] cells) =>
        new(sheet, row, cells.Where(cell => cell.Value.Length > 0).ToDictionary(cell => cell.Column, cell => cell.Value));

    public static RawRow Skill(
        int row,
        string name,
        string type = "Académicas",
        string cost = "1",
        string abilities = "INT",
        string die = "8",
        string requirement = "N",
        string price = "100",
        string difficulty = "5",
        string book = "MJ 100",
        string classes = "Todas") =>
        Row(
            SheetNames.Skills,
            row,
            ("A", name),
            ("B", type),
            ("C", cost),
            ("D", abilities),
            ("E", die),
            ("F", requirement),
            ("G", price),
            ("H", difficulty),
            ("I", book),
            ("J", classes));

    public static RawRow Description(int row, string name, string summary = "Resumen propio.") =>
        Row(SheetNames.Descriptions, row, ("A", name), ("B", "MJ 100"), ("C", summary));

    public static RawRow Doubt(
        int row,
        string subject,
        string source = "GrG 65",
        string question = "La tabla y el texto discrepan.",
        string provisional = "Se dejó la tabla.") =>
        Row(SheetNames.Doubts, row, ("A", subject), ("B", source), ("C", question), ("D", provisional));

    public static RawRow CustomTitle(
        int row,
        string name,
        string cost = "5",
        string abilities = "INT",
        string die = "",
        string price = "500",
        string difficulty = "15",
        string book = "HJ 4.05",
        string classes = "Todas") =>
        Row(
            SheetNames.Suites,
            row,
            ("B", name),
            ("C", cost),
            ("D", abilities),
            ("E", die),
            ("F", SuiteRequirementText),
            ("G", price),
            ("H", difficulty),
            ("I", book),
            ("J", classes));

    public static RawRow CustomHeader(int row) =>
        Row(
            SheetNames.Suites,
            row,
            ("B", "Habilidad"),
            ("C", "PC"),
            ("D", "Caracteristica Relevante"),
            ("E", "dado de maestria"),
            ("F", "Requisito?"),
            ("G", "Precio"),
            ("H", "Dificultad"));

    public static RawRow Component(int row, string name) =>
        Row(SheetNames.Suites, row, ("B", name), ("C", "1"), ("D", "INT"), ("E", "8"), ("F", "N"), ("G", "100"), ("H", "5"));

    public static RawRow SectionTitle(int row, string title) => Row(SheetNames.Suites, row, ("B", title));

    public static RawRow SectionHeader(int row) =>
        Row(
            SheetNames.Suites,
            row,
            ("B", "Habilidad"),
            ("C", "Tipo"),
            ("D", "PC"),
            ("E", "Caracteristica Relevante"),
            ("F", "dado de maestria"),
            ("G", "Requisito?"),
            ("H", "Precio"),
            ("I", "Dificultad"),
            ("J", "Libro"));

    public static RawRow Official(
        int row,
        string name,
        string cost = "7",
        string abilities = "INT",
        string die = "8",
        string requirement = SuiteRequirementText,
        string price = "935",
        string difficulty = "15",
        string book = "GMG 71") =>
        Row(
            SheetNames.Suites,
            row,
            ("B", name),
            ("C", "Conjunto de Habilidades"),
            ("D", cost),
            ("E", abilities),
            ("F", die),
            ("G", requirement),
            ("H", price),
            ("I", difficulty),
            ("J", book));

    public static RawRow Style(
        int row,
        string name,
        string abilities = "FUE, DES",
        string die = "4",
        string requirement = "N",
        string price = "400",
        string difficulty = "15",
        string book = "HJ8 10") =>
        Row(
            SheetNames.Suites,
            row,
            ("B", name),
            ("C", "Estilos de combate"),
            ("D", "NA"),
            ("E", abilities),
            ("F", die),
            ("G", requirement),
            ("H", price),
            ("I", difficulty),
            ("J", book));

    /// <summary>
    /// Libro en bruto; si no se dan descripciones, cada nombre de habilidad distinto (sin
    /// distinguir acentos ni mayúsculas) recibe una fila de resumen.
    /// </summary>
    public static RawWorkbook Workbook(
        IReadOnlyList<RawRow> skills,
        IReadOnlyList<RawRow>? descriptions = null,
        IReadOnlyList<RawRow>? suites = null,
        IReadOnlyList<RawRow>? doubts = null) =>
        new(
            skills,
            descriptions ??
            [
                .. skills
                    .DistinctBy(skill => HackMaster.Domain.TextNormalizer.Normalize(skill["A"]))
                    .Select(skill => Description(skill.RowNumber, skill["A"])),
            ],
            suites ?? [],
            doubts ?? []);
}
