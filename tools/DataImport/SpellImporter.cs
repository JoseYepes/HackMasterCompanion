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
    public const string Doubts = "Dudas";
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

    private static readonly WorkbookReader.ExpectedColumn[] DoubtColumns =
    [
        new("A", "conjuro", "Conjuro"),
        new("B", "clase", "Clase"),
        new("C", "fuente", "Fuente"),
        new("D", "duda", "Duda"),
        new("E", "decision", "Decisión provisional"),
    ];

    /// <summary>
    /// Lee las pestañas; nulo con los errores si falta «Conjuros» o «Reglas», o una columna. La de
    /// «Dudas» es opcional (FR-009): si no está, no hay dudas.
    /// </summary>
    public static (IReadOnlyList<RawRow>? Spells, IReadOnlyList<RawRow>? Rules, IReadOnlyList<RawRow> Doubts, IReadOnlyList<ImportMessage> Errors) Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var errors = new List<ImportMessage>();
        var spells = WorkbookReader.WithoutHeader(WorkbookReader.ReadSheet(workbook, SpellSheetNames.Spells, lastColumn: 13, errors), SpellSheetNames.Spells, SpellColumns, errors);
        var rules = WorkbookReader.WithoutHeader(WorkbookReader.ReadSheet(workbook, SpellSheetNames.Rules, lastColumn: 3, errors), SpellSheetNames.Rules, RuleColumns, errors);
        var doubts = workbook.TryGetWorksheet(SpellSheetNames.Doubts, out _)
            ? WorkbookReader.WithoutHeader(WorkbookReader.ReadSheet(workbook, SpellSheetNames.Doubts, lastColumn: 5, errors), SpellSheetNames.Doubts, DoubtColumns, errors)
            : [];
        return errors.Count > 0 ? (null, null, [], errors) : (spells, rules, doubts ?? [], errors);
    }

    public static SpellImportResult Import(IReadOnlyList<RawRow> spellRows, IReadOnlyList<RawRow> ruleRows) => Import(spellRows, ruleRows, []);

    public static SpellImportResult Import(IReadOnlyList<RawRow> spellRows, IReadOnlyList<RawRow> ruleRows, IReadOnlyList<RawRow> doubtRows)
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

        var doubts = ReadDoubts(doubtRows, [.. spells.Select(entry => entry.Spell)], errors);

        return errors.Count > 0
            ? new SpellImportResult(null, errors, warnings)
            : new SpellImportResult(new SpellsFile(GameDataJson.CurrentSchemaVersion, [.. spells.Select(entry => entry.Spell).OrderBy(spell => spell.Class).ThenBy(spell => spell.Level).ThenBy(spell => spell.SearchName, StringComparer.Ordinal)], rules, doubts), errors, warnings);
    }

    /// <summary>Lee la pestaña «Dudas» (FR-009): cada duda nombra un conjuro que existe, o ninguno si es general.</summary>
    private static List<SpellDoubt> ReadDoubts(IReadOnlyList<RawRow> rows, IReadOnlyList<Spell> spells, List<ImportMessage> errors)
    {
        var doubts = new List<SpellDoubt>();
        foreach (var row in rows)
        {
            var name = row["A"];
            var question = row["D"];
            if (question.Length == 0)
            {
                errors.Add(new ImportMessage(SpellSheetNames.Doubts, row.RowNumber, name.Length == 0 ? null : name, "a la duda le falta el texto de la duda."));
                continue;
            }

            var classText = row["B"];
            Spell? spell = null;
            if (name.Length == 0 && classText.Length > 0)
            {
                errors.Add(new ImportMessage(SpellSheetNames.Doubts, row.RowNumber, null, "una duda general no lleva clase; si es de un conjuro, falta su nombre."));
                continue;
            }

            if (name.Length > 0)
            {
                if (classText.Length == 0)
                {
                    errors.Add(new ImportMessage(SpellSheetNames.Doubts, row.RowNumber, name, "falta la clase del conjuro (Mago o Clérigo)."));
                    continue;
                }

                if (SpellClasses.FromSlug(classText) is not { } spellClass)
                {
                    errors.Add(new ImportMessage(SpellSheetNames.Doubts, row.RowNumber, name, $"la clase «{classText}» no es Mago ni Clérigo."));
                    continue;
                }

                var slug = TextNormalizer.ToSlug(name);
                spell = spells.FirstOrDefault(candidate => candidate.Class == spellClass && candidate.Slug == slug);
                if (spell is null)
                {
                    errors.Add(new ImportMessage(SpellSheetNames.Doubts, row.RowNumber, name, $"no hay ningún conjuro «{name}» de {SpellClasses.Name(spellClass).ToLowerInvariant()}."));
                    continue;
                }
            }

            doubts.Add(new SpellDoubt
            {
                Id = $"conjuro-{row.RowNumber}",
                Subject = spell?.Name ?? "General",
                Class = spell?.Class,
                Slug = spell?.Slug,
                Source = row["C"].Length == 0 ? null : row["C"],
                Question = question,
                Provisional = row["E"].Length == 0 ? null : row["E"],
            });
        }

        return doubts;
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
        else if (name == PendingMark || TextNormalizer.ToSlug(name).Length == 0)
        {
            problems.Add($"el nombre «{name}» no vale: tiene que tener letras o números.");
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

        // Las escuelas se separan solo por comas: las compuestas del MJ («Encantamiento/Hechizo»,
        // «Ilusión/Fantasma»…, p. 124) son una sola escuela y van con barra.
        var schoolsText = Text("D", "schools");

        // Reversible: vacío = no; «Sí» = sí, sin nombre en el MJ; otro texto = nombre del inverso.
        var reverseText = Text("E", "reverse");
        var reversible = reverseText is not null || pending.Contains("reverse");
        var reverseName = reverseText is not null && TextNormalizer.Normalize(reverseText) == "si" ? null : reverseText;
        var spell = new Spell
        {
            Slug = TextNormalizer.ToSlug(name),
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Class = spellClass!.Value,
            Level = level,
            Schools = schoolsText is null ? [] : [.. schoolsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
            Reversible = reversible,
            Reverse = reverseName,
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

    /// <summary>
    /// Escribe <c>spells.json</c> en la carpeta de datos y el listado de dudas de conjuros
    /// (FR-009), ambos en UTF-8 sin BOM y con LF.
    /// </summary>
    public static void Write(SpellsFile file, string dataDirectory, string doubtsDocument)
    {
        // Los dos textos se preparan antes de escribir nada.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var json = JsonSerializer.Serialize(file, GameDataJson.Options) + "\n";
        var document = RenderDoubtsDocument(file.Doubts ?? []);
        Directory.CreateDirectory(dataDirectory);
        if (Path.GetDirectoryName(Path.GetFullPath(doubtsDocument)) is { } folder)
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(Path.Combine(dataDirectory, SpellsFileName), json, encoding);
        File.WriteAllText(doubtsDocument, document, encoding);
    }

    private static string RenderDoubtsDocument(IReadOnlyList<SpellDoubt> doubts)
    {
        var text = new StringBuilder();
        text.Append("# Dudas de los conjuros\n\n");
        text.Append("> Archivo generado por `tools/DataImport conjuros` a partir de la pestaña \"Dudas\" de la hoja de\n");
        text.Append("> conjuros. No lo edites a mano: cambia la hoja y vuelve a importar.\n");
        if (doubts.Count == 0)
        {
            text.Append("\nNo hay dudas de conjuros.\n");
            return text.ToString();
        }

        foreach (var doubt in doubts)
        {
            var title = doubt.Class is { } spellClass ? $"{doubt.Subject} ({SpellClasses.Name(spellClass).ToLowerInvariant()})" : doubt.Subject;
            text.Append($"\n### {title}\n\n");
            if (doubt.Source is not null)
            {
                text.Append($"- **Fuente**: {doubt.Source}\n");
            }

            text.Append($"- **Duda**: {doubt.Question}\n");
            if (doubt.Provisional is not null)
            {
                text.Append($"- **Decisión provisional**: {doubt.Provisional}\n");
            }
        }

        return text.ToString();
    }

    [GeneratedRegex(@"^(\S+)\s+(\S.*)$")]
    private static partial Regex BookPattern();
}
