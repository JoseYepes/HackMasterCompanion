using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>Interpreta y valida las filas de la hoja. No accede a disco ni a la librería de Excel.</summary>
public static class Importer
{
    public static ImportResult Import(RawWorkbook workbook) => new ImportRun(workbook).Execute();
}

/// <summary>Una ejecución de la importación: acumula todos los errores y avisos antes de decidir.</summary>
internal sealed class ImportRun(RawWorkbook workbook)
{
    private const string SuiteRequirementPrefix = "las de las habilidades";

    /// <summary>Valor de la columna A de "Dudas" que marca una duda sin elemento concreto.</summary>
    private const string GeneralSubject = "general";

    private readonly List<ImportMessage> _errors = [];
    private readonly List<ImportMessage> _warnings = [];
    private readonly List<Doubt> _importDoubts = [];
    private readonly NameIndex _skills = new();
    private readonly NameIndex _customSuites = new();
    private readonly NameIndex _officialSuites = new();
    private readonly NameIndex _styles = new();

    public ImportResult Execute()
    {
        var skillRows = workbook.Skills.Where(row => row["A"].Length > 0).ToList();
        var suiteSheet = SuiteSheetParser.Parse(workbook.Suites, _errors);

        // Primero todos los nombres, para poder resolver referencias en cualquier dirección.
        Register(_skills, skillRows.Select(row => (row, row["A"])));
        Register(_customSuites, suiteSheet.Custom.Select(block => (block.Title, block.Title["B"])));
        Register(_officialSuites, suiteSheet.Official.Select(row => (row, row["B"])));
        Register(_styles, suiteSheet.Styles.Select(row => (row, row["B"])));

        var summaries = ReadSummaries();
        var skills = skillRows.Select(row => BuildSkill(row, summaries)).ToList();
        var suites = suiteSheet.Custom.Select(BuildCustomSuite).Concat(suiteSheet.Official.Select(BuildOfficialSuite)).ToList();
        var styles = suiteSheet.Styles.Select(BuildStyle).ToList();
        var sheetDoubts = BuildSheetDoubts();

        if (_errors.Count > 0)
        {
            return new ImportResult(null, _errors, _warnings);
        }

        var model = new GameModel(
            [.. skills.OrderBy(skill => skill.SearchName, StringComparer.Ordinal).ThenBy(skill => skill.Slug, StringComparer.Ordinal)],
            [.. suites.OrderBy(suite => suite.SearchName, StringComparer.Ordinal).ThenBy(suite => suite.Kind)],
            [.. styles.OrderBy(style => style.SearchName, StringComparer.Ordinal)],
            [.. Ordered(sheetDoubts), .. Ordered(_importDoubts)]);

        return new ImportResult(model, _errors, _warnings);
    }

    private static IEnumerable<Doubt> Ordered(IEnumerable<Doubt> doubts) =>
        doubts.OrderBy(doubt => TextNormalizer.Normalize(doubt.Subject), StringComparer.Ordinal).ThenBy(doubt => doubt.Id, StringComparer.Ordinal);

    private void Register(NameIndex index, IEnumerable<(RawRow Row, string Name)> entries)
    {
        foreach (var (row, name) in entries)
        {
            var conflict = index.Add(name, row.RowNumber);
            if (conflict is { } found)
            {
                var text = found.SameName
                    ? $"nombre duplicado: «{found.Name}» y «{name}»."
                    : $"«{found.Name}» y «{name}» darían la misma dirección («{TextNormalizer.ToSlug(name)}»).";
                _errors.Add(new ImportMessage(row.Sheet, [found.Row, row.RowNumber], null, text));
            }
        }
    }

    /// <summary>Busca un nombre en este orden: habilidad, conjunto de habilidades, conjunto oficial.</summary>
    private (ElementKind Kind, string Slug)? Resolve(string name)
    {
        if (_skills.Find(name) is { } skill)
        {
            return (ElementKind.Skill, skill);
        }

        if (_customSuites.Find(name) is { } suite)
        {
            return (ElementKind.Suite, suite);
        }

        return _officialSuites.Find(name) is { } official ? (ElementKind.OfficialSuite, official) : null;
    }

    private FieldReader ReaderFor(RawRow row, string name, ElementKind kind, string slug) =>
        new((field, message) =>
        {
            _warnings.Add(new ImportMessage(row.Sheet, row.RowNumber, name, message));
            _importDoubts.Add(new Doubt
            {
                Id = $"import-{KindId(kind)}-{slug}-{field}",
                Origin = DoubtOrigin.Import,
                Subject = name,
                Target = new DoubtTarget(kind, slug),
                Question = char.ToUpperInvariant(message[0]) + message[1..],
            });
        });

    private Dictionary<string, string> ReadSummaries()
    {
        var summaries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in workbook.Descriptions.Where(row => row["A"].Length > 0))
        {
            var name = row["A"];
            if (_skills.Find(name) is null)
            {
                _errors.Add(new ImportMessage(row.Sheet, row.RowNumber, name, $"no existe en la pestaña \"{SheetNames.Skills}\"."));
                continue;
            }

            if (!summaries.TryAdd(TextNormalizer.Normalize(name), row["C"]))
            {
                _errors.Add(new ImportMessage(row.Sheet, row.RowNumber, name, "tiene más de una fila de descripción."));
            }
        }

        return summaries;
    }

    private Skill BuildSkill(RawRow row, Dictionary<string, string> summaries)
    {
        var name = row["A"];
        var slug = TextNormalizer.ToSlug(name);
        var reader = ReaderFor(row, name, ElementKind.Skill, slug);

        var type = row["B"];
        if (type.Length == 0)
        {
            reader.MarkPending("type", string.Empty, "falta el tipo; queda pendiente de verificar.");
        }

        var cost = reader.Integer("cost", "el coste en PC", row["C"]);
        var abilities = reader.Abilities(row["D"]);
        var die = reader.MasteryDie(row["E"]);
        var prerequisite = ParsePrerequisite(reader, row, name, row["F"]);
        var price = reader.Integer("price", "el precio", row["G"]);
        var difficulty = reader.Percentage("difficulty", "la dificultad", row["H"]);
        var books = reader.Books(row["I"]);
        var classes = reader.Classes(row["J"]);

        summaries.TryGetValue(TextNormalizer.Normalize(name), out var summary);
        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = null;
            reader.MarkPending("summary", string.Empty, "no tiene resumen propio; queda pendiente de verificar.");
        }

        return new Skill
        {
            Slug = slug,
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Type = type,
            Cost = cost,
            Abilities = abilities,
            MasteryDie = die,
            Prerequisite = prerequisite,
            Price = price,
            Difficulty = difficulty,
            Books = books,
            Classes = classes,
            Summary = summary,
            SearchSummary = summary is null ? null : TextNormalizer.Normalize(summary),
            Pending = reader.Pending,
            Raw = reader.Raw,
        };
    }

    private SkillSuite BuildCustomSuite(CustomSuiteBlock block)
    {
        var row = block.Title;
        var name = row["B"];
        var slug = TextNormalizer.ToSlug(name);
        var reader = ReaderFor(row, name, ElementKind.Suite, slug);

        var cost = reader.Integer("cost", "el coste en PC", row["C"]);
        var abilities = reader.Abilities(row["D"]);

        // La hoja no da dado de maestría en estos conjuntos: se calcula con las reglas de HJ 4.05 (F002).
        var die = reader.MasteryDie(row["E"], missingIsPending: false);
        var prerequisite = ParsePrerequisite(reader, row, name, row["F"], suiteText: true);
        var price = reader.Integer("price", "el precio", row["G"]);
        var difficulty = reader.Percentage("difficulty", "la dificultad", row["H"]);
        var books = reader.Books(row["I"]);
        var classes = reader.Classes(row["J"]);

        var components = new List<SuiteComponent>();
        foreach (var component in block.Components)
        {
            var componentName = component["B"];
            if (Resolve(componentName) is { } found)
            {
                components.Add(new SuiteComponent(componentName, found.Kind, found.Slug));
            }
            else
            {
                _errors.Add(new ImportMessage(component.Sheet, component.RowNumber, name, $"el componente «{componentName}» no existe."));
            }
        }

        if (block.Components.Count == 0)
        {
            reader.MarkPending("components", string.Empty, "la fuente no detalla sus componentes; quedan pendientes de verificar.");
        }

        return new SkillSuite
        {
            Slug = slug,
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Kind = SuiteKind.Custom,
            Cost = cost,
            Abilities = abilities,
            MasteryDie = die,
            Price = price,
            Difficulty = difficulty,
            Books = books,
            Classes = classes,
            Components = components,
            Prerequisite = prerequisite,
            Pending = reader.Pending,
            Raw = reader.Raw,
        };
    }

    private SkillSuite BuildOfficialSuite(RawRow row)
    {
        var name = row["B"];
        var slug = TextNormalizer.ToSlug(name);
        var reader = ReaderFor(row, name, ElementKind.OfficialSuite, slug);

        var cost = reader.Integer("cost", "el coste en PC", row["D"]);
        var abilities = reader.Abilities(row["E"]);
        var die = reader.MasteryDie(row["F"]);
        var prerequisite = ParsePrerequisite(reader, row, name, row["G"], suiteText: true);
        var price = reader.Integer("price", "el precio", row["H"]);
        var difficulty = reader.Percentage("difficulty", "la dificultad", row["I"]);
        var books = reader.Books(row["J"]);
        reader.MarkPending("components", string.Empty, "la fuente no detalla sus componentes; quedan pendientes de verificar.");

        return new SkillSuite
        {
            Slug = slug,
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Kind = SuiteKind.Official,
            Cost = cost,
            Abilities = abilities,
            MasteryDie = die,
            Price = price,
            Difficulty = difficulty,
            Books = books,
            Prerequisite = prerequisite,
            Pending = reader.Pending,
            Raw = reader.Raw,
        };
    }

    private CombatStyle BuildStyle(RawRow row)
    {
        var name = row["B"];
        var slug = TextNormalizer.ToSlug(name);
        var reader = ReaderFor(row, name, ElementKind.Style, slug);

        var abilities = reader.Abilities(row["E"]);
        var die = reader.MasteryDie(row["F"]);
        var prerequisite = ParsePrerequisite(reader, row, name, row["G"]);
        var price = reader.Integer("price", "el precio", row["H"]);
        var difficulty = reader.Percentage("difficulty", "la dificultad", row["I"]);
        var books = reader.Books(row["J"]);

        return new CombatStyle
        {
            Slug = slug,
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Abilities = abilities,
            MasteryDie = die,
            Prerequisite = prerequisite,
            Price = price,
            Difficulty = difficulty,
            Books = books,
            Pending = reader.Pending,
            Raw = reader.Raw,
        };
    }

    private Prerequisite ParsePrerequisite(FieldReader reader, RawRow row, string name, string text, bool suiteText = false)
    {
        // En los conjuntos la celda suele decir "Las de las habilidades que lo componen": es texto, no un nombre.
        if (suiteText && TextNormalizer.Normalize(text).StartsWith(SuiteRequirementPrefix, StringComparison.Ordinal))
        {
            return new Prerequisite { Segments = [new PrerequisiteSegment(text, PrerequisiteSegmentKind.Text)] };
        }

        var parsed = PrerequisiteParser.Parse(text, Resolve);
        foreach (var unresolved in parsed.Unresolved)
        {
            _errors.Add(new ImportMessage(row.Sheet, row.RowNumber, name, $"el requisito «{unresolved}» no coincide con ninguna habilidad ni conjunto."));
        }

        if (parsed.HasUnverified)
        {
            reader.MarkPending("prerequisite", text, "el requisito está sin verificar en la fuente; queda pendiente de verificar.");
        }

        return parsed.Prerequisite;
    }

    private List<Doubt> BuildSheetDoubts()
    {
        var doubts = new List<Doubt>();
        var perTarget = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in workbook.Doubts.Where(row => row["A"].Length > 0))
        {
            var subject = row["A"];
            if (TextNormalizer.Normalize(subject) == GeneralSubject)
            {
                var generalOrdinal = perTarget[GeneralSubject] = perTarget.GetValueOrDefault(GeneralSubject) + 1;
                doubts.Add(new Doubt
                {
                    Id = $"sheet-general-{generalOrdinal}",
                    Origin = DoubtOrigin.Sheet,
                    Subject = "General",
                    Source = NullIfEmpty(row["B"]),
                    Question = row["C"],
                    Provisional = NullIfEmpty(row["D"]),
                });
                continue;
            }

            var target = ResolveAny(subject);
            if (target is null)
            {
                _errors.Add(new ImportMessage(
                    row.Sheet,
                    row.RowNumber,
                    subject,
                    "no coincide con ninguna habilidad, conjunto ni estilo; cada fila debe nombrar un único elemento con su nombre exacto, o decir «General»."));
                continue;
            }

            var key = $"{KindId(target.Kind)}-{target.Slug}";
            var ordinal = perTarget[key] = perTarget.GetValueOrDefault(key) + 1;
            doubts.Add(new Doubt
            {
                Id = $"sheet-{key}-{ordinal}",
                Origin = DoubtOrigin.Sheet,
                Subject = subject,
                Target = target,
                Source = NullIfEmpty(row["B"]),
                Question = row["C"],
                Provisional = NullIfEmpty(row["D"]),
            });
        }

        return doubts;
    }

    private DoubtTarget? ResolveAny(string name)
    {
        if (Resolve(name) is { } found)
        {
            return new DoubtTarget(found.Kind, found.Slug);
        }

        return _styles.Find(name) is { } style ? new DoubtTarget(ElementKind.Style, style) : null;
    }

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;

    private static string KindId(ElementKind kind) => kind switch
    {
        ElementKind.Skill => "skill",
        ElementKind.Suite => "suite",
        ElementKind.OfficialSuite => "official-suite",
        _ => "style",
    };
}

/// <summary>Nombres de un tipo de elemento, únicos sin distinguir acentos ni mayúsculas.</summary>
internal sealed class NameIndex
{
    private readonly Dictionary<string, (int Row, string Name, string Slug)> _byName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (int Row, string Name)> _bySlug = new(StringComparer.Ordinal);

    /// <summary>Añade un nombre. Si choca con otro, devuelve con cuál y si es por nombre o por dirección.</summary>
    public (int Row, string Name, bool SameName)? Add(string name, int row)
    {
        var normalized = TextNormalizer.Normalize(name);
        if (_byName.TryGetValue(normalized, out var sameName))
        {
            return (sameName.Row, sameName.Name, true);
        }

        // El nombre se registra aunque su dirección choque, para que las referencias a él no
        // generen además errores en cascada.
        var slug = TextNormalizer.ToSlug(name);
        _byName[normalized] = (row, name, slug);
        if (_bySlug.TryGetValue(slug, out var sameSlug))
        {
            return (sameSlug.Row, sameSlug.Name, false);
        }

        _bySlug[slug] = (row, name);
        return null;
    }

    /// <summary>Devuelve el identificador del elemento con ese nombre, o nulo si no existe.</summary>
    public string? Find(string name) =>
        _byName.TryGetValue(TextNormalizer.Normalize(name), out var found) ? found.Slug : null;
}
