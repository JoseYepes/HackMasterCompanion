using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>Pestañas de la hoja de conjuros (<c>Hackconjuros.xlsx</c>, F009).</summary>
public static class SpellSheetNames
{
    public const string Spells = "Conjuros";
    public const string Rules = "Reglas";
}

/// <summary>Resultado de importar los conjuros: el archivo de datos, o los errores.</summary>
public sealed record SpellImportResult(SpellsFile? File, IReadOnlyList<ImportMessage> Errors, IReadOnlyList<ImportMessage> Warnings);

/// <summary>
/// Importa los conjuros y las reglas del Grimorio (F009). La hoja la escribe el agente leyendo el MJ
/// y solo tiene datos de cabecera y resúmenes propios: no hay columna con el texto del manual.
/// </summary>
public static partial class SpellImporter
{
    public const string SpellsFileName = "spells.json";

    /// <summary>Lo que en una celda marca un dato que no se pudo leer con seguridad.</summary>
    private const string PendingMark = "?";

    private static readonly WorkbookReader.ExpectedColumn[] SpellColumns =
    [
        new("A", "nombre", "Nombre"),
        new("B", "clase", "Clase"),
        new("C", "nivel", "Nivel"),
        new("D", "escuela", "Escuela o esfera"),
        new("E", "reversible", "Reversible"),
        new("F", "alcance", "Alcance"),
        new("G", "componentes", "Componentes"),
        new("H", "duracion", "Duración"),
        new("I", "tiempo", "Tiempo de lanzamiento"),
        new("J", "area", "Área de efecto"),
        new("K", "tirada", "Tirada de salvación"),
        new("L", "libro", "Libro"),
        new("M", "resumen", "Resumen propio"),
    ];

    private static readonly WorkbookReader.ExpectedColumn[] RuleColumns =
    [
        new("A", "regla", "Regla"),
        new("B", "resumen", "Resumen propio"),
        new("C", "libro", "Libro"),
    ];

    /// <summary>Lee las dos pestañas; nulo con los errores si falta alguna o una columna.</summary>
    public static (IReadOnlyList<RawRow>? Spells, IReadOnlyList<RawRow>? Rules, IReadOnlyList<ImportMessage> Errors) Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var errors = new List<ImportMessage>();
        var spells = WorkbookReader.WithoutHeader(WorkbookReader.ReadSheet(workbook, SpellSheetNames.Spells, lastColumn: 13, errors), SpellSheetNames.Spells, SpellColumns, errors);
        var rules = WorkbookReader.WithoutHeader(WorkbookReader.ReadSheet(workbook, SpellSheetNames.Rules, lastColumn: 3, errors), SpellSheetNames.Rules, RuleColumns, errors);
        return errors.Count > 0 ? (null, null, errors) : (spells, rules, errors);
    }

    public static SpellImportResult Import(IReadOnlyList<RawRow> spellRows, IReadOnlyList<RawRow> ruleRows)
    {
        var errors = new List<ImportMessage>();
        var warnings = new List<ImportMessage>();
        var spells = new List<(Spell Spell, int Row)>();

        foreach (var row in spellRows)
        {
            if (ReadSpell(row, errors, warnings) is { } spell)
            {
                spells.Add((spell, row.RowNumber));
            }
        }

        foreach (var group in spells.GroupBy(entry => (entry.Spell.Class, entry.Spell.Slug)).Where(group => group.Count() > 1))
        {
            errors.Add(new ImportMessage(SpellSheetNames.Spells, [.. group.Select(entry => entry.Row)], group.First().Spell.Name,
                $"hay {group.Count()} conjuros de {SpellClasses.Name(group.Key.Class).ToLowerInvariant()} con el mismo nombre."));
        }

        var rules = new List<GrimoireRule>();
        foreach (var row in ruleRows)
        {
            var title = row["A"];
            var summary = row["B"];
            var books = ReadBooks(row["C"]);
            if (title.Length == 0 || summary.Length == 0 || books is null)
            {
                errors.Add(new ImportMessage(SpellSheetNames.Rules, row.RowNumber, title.Length == 0 ? null : title, "a la regla le falta el título, el resumen o el libro y página."));
                continue;
            }

            rules.Add(new GrimoireRule(title, summary, books));
        }

        return errors.Count > 0
            ? new SpellImportResult(null, errors, warnings)
            : new SpellImportResult(new SpellsFile(GameDataJson.CurrentSchemaVersion, [.. spells.Select(entry => entry.Spell).OrderBy(spell => spell.Class).ThenBy(spell => spell.Level).ThenBy(spell => spell.SearchName, StringComparer.Ordinal)], rules), errors, warnings);
    }

    private static Spell? ReadSpell(RawRow row, List<ImportMessage> errors, List<ImportMessage> warnings)
    {
        var name = row["A"];
        var element = name.Length == 0 ? null : name;
        var problems = new List<string>();
        if (name.Length == 0)
        {
            problems.Add("falta el nombre del conjuro.");
        }

        SpellClass? spellClass = TextNormalizer.Normalize(row["B"]) switch
        {
            "mago" => SpellClass.Mage,
            "clerigo" => SpellClass.Cleric,
            _ => null,
        };
        if (spellClass is null)
        {
            problems.Add($"la clase «{row["B"]}» no es «Mago» ni «Clérigo».");
        }

        var (minLevel, maxLevel) = spellClass == SpellClass.Cleric ? (1, 7) : (0, 9);
        if (!int.TryParse(row["C"], NumberStyles.None, CultureInfo.InvariantCulture, out var level) || level < minLevel || level > maxLevel)
        {
            problems.Add($"el nivel «{row["C"]}» no es válido (de {minLevel} a {maxLevel}).");
        }

        var books = ReadBooks(row["L"]);
        if (books is null)
        {
            problems.Add($"el libro «{row["L"]}» no tiene la forma «MJ 175».");
        }

        if (problems.Count > 0)
        {
            errors.AddRange(problems.Select(problem => new ImportMessage(SpellSheetNames.Spells, row.RowNumber, element, problem)));
            return null;
        }

        var pending = new List<string>();
        string? Text(string column, string field)
        {
            var value = row[column];
            if (value == PendingMark)
            {
                pending.Add(field);
                return null;
            }

            return value.Length == 0 ? null : value;
        }

        var schoolsText = Text("D", "schools");
        var spell = new Spell
        {
            Slug = TextNormalizer.ToSlug(name),
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Class = spellClass!.Value,
            Level = level,
            Schools = schoolsText is null ? [] : [.. schoolsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
            Reverse = Text("E", "reverse"),
            Range = Text("F", "range"),
            Components = Text("G", "components"),
            Duration = Text("H", "duration"),
            CastingTime = Text("I", "castingTime"),
            AreaOfEffect = Text("J", "areaOfEffect"),
            SavingThrow = Text("K", "savingThrow"),
            Books = books!,
            Summary = Text("M", "summary"),
        };

        if (spell.Summary is null && !pending.Contains("summary"))
        {
            pending.Add("summary");
            warnings.Add(new ImportMessage(SpellSheetNames.Spells, row.RowNumber, name, "falta el resumen propio; queda pendiente."));
        }

        return spell with { Pending = pending };
    }

    /// <summary>Libros y páginas separados por «;» («MJ 175; SSG 40»), o nulo si alguno no tiene página.</summary>
    private static List<BookRef>? ReadBooks(string text)
    {
        var parts = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var books = new List<BookRef>();
        foreach (var part in parts)
        {
            var match = BookPattern().Match(part);
            if (!match.Success)
            {
                return null;
            }

            books.Add(new BookRef(match.Groups[1].Value, match.Groups[2].Value));
        }

        return books.Count == 0 ? null : books;
    }

    /// <summary>Escribe <c>spells.json</c> en la carpeta de datos (UTF-8 sin BOM, LF).</summary>
    public static void Write(SpellsFile file, string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(
            Path.Combine(dataDirectory, SpellsFileName),
            JsonSerializer.Serialize(file, GameDataJson.Options) + "\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    [GeneratedRegex(@"^(\S+)\s+(\S.*)$")]
    private static partial Regex BookPattern();
}
