namespace HackMaster.Domain;

/// <summary>Clase que lanza el conjuro (F009). Los de druida quedan fuera hasta que se pidan.</summary>
public enum SpellClass
{
    Mage,
    Cleric,
}

/// <summary>Nombres y direcciones de las clases de conjuro, en castellano.</summary>
public static class SpellClasses
{
    public static string Name(SpellClass spellClass) => spellClass == SpellClass.Mage ? "Mago" : "Clérigo";

    public static string Slug(SpellClass spellClass) => spellClass == SpellClass.Mage ? "mago" : "clerigo";

    /// <summary>Clase de una dirección, sin importar mayúsculas ni acentos («Mago», «CLÉRIGO»).</summary>
    public static SpellClass? FromSlug(string? slug) => TextNormalizer.Normalize(slug) switch
    {
        "mago" => SpellClass.Mage,
        "clerigo" => SpellClass.Cleric,
        _ => null,
    };
}

/// <summary>
/// Conjuro de mago o de clérigo (F009), con los datos de su cabecera en el manual y un resumen
/// propio. El texto literal del manual no se guarda nunca (constitución I).
/// </summary>
public sealed record Spell
{
    /// <summary>Identificador en la dirección, único dentro de su clase.</summary>
    public required string Slug { get; init; }

    public required string Name { get; init; }

    /// <summary>Nombre normalizado (sin acentos ni mayúsculas) para buscar.</summary>
    public required string SearchName { get; init; }

    public required SpellClass Class { get; init; }

    /// <summary>Nivel del conjuro; 0 para los trucos.</summary>
    public required int Level { get; init; }

    /// <summary>Escuelas de magia, como en el manual (en los de clérigo, «a efectos de referencia», MJ p. 253).</summary>
    public IReadOnlyList<string> Schools { get; init; } = [];

    /// <summary>Verdadero si el conjuro tiene versión inversa (el MJ a veces no le da nombre).</summary>
    public bool Reversible { get; init; }

    /// <summary>Nombre de la versión inversa, si el MJ se lo da.</summary>
    public string? Reverse { get; init; }

    public string? Range { get; init; }

    public string? Components { get; init; }

    public string? Duration { get; init; }

    public string? CastingTime { get; init; }

    public string? AreaOfEffect { get; init; }

    public string? SavingThrow { get; init; }

    public IReadOnlyList<BookRef> Books { get; init; } = [];

    /// <summary>Resumen propio, nunca el texto del manual.</summary>
    public string? Summary { get; init; }

    /// <summary>Campos pendientes de verificar (por su nombre en inglés, como en las habilidades).</summary>
    public IReadOnlyList<string> Pending { get; init; } = [];
}

/// <summary>Regla de los libros de conjuros o de la transcripción, resumida con su fuente.</summary>
public sealed record GrimoireRule(string Title, string Summary, IReadOnlyList<BookRef> Books);

/// <summary>Contenido de <c>data/spells.json</c>.</summary>
public sealed record SpellsFile(int SchemaVersion, IReadOnlyList<Spell> Spells, IReadOnlyList<GrimoireRule> Rules, IReadOnlyList<SpellDoubt>? Doubts = null);

/// <summary>
/// Duda sobre un conjuro, o general del Grimorio si no nombra ninguno (F009, FR-009): erratas
/// aplicadas, datos en blanco o contradictorios en el MJ, nombres cambiados.
/// </summary>
public sealed record SpellDoubt
{
    /// <summary>Identificador estable dentro del archivo de conjuros.</summary>
    public required string Id { get; init; }

    /// <summary>Nombre del conjuro, o tema de la duda general.</summary>
    public required string Subject { get; init; }

    /// <summary>Clase del conjuro; nula en una duda general.</summary>
    public SpellClass? Class { get; init; }

    /// <summary>Dirección del conjuro dentro de su clase; nula en una duda general.</summary>
    public string? Slug { get; init; }

    /// <summary>Libro y página.</summary>
    public string? Source { get; init; }

    /// <summary>Qué no es firme en las fuentes.</summary>
    public required string Question { get; init; }

    /// <summary>Qué se ha cargado mientras tanto.</summary>
    public string? Provisional { get; init; }
}

/// <summary>Criterios de búsqueda del Grimorio; los nulos no filtran.</summary>
public sealed record SpellCriteria(string? Text = null, SpellClass? Class = null, int? Level = null, string? School = null);

/// <summary>Búsqueda del Grimorio: nombre sin acentos ni mayúsculas y filtros combinables (FR-002, FR-003).</summary>
public static class SpellQuery
{
    public static IReadOnlyList<Spell> Filter(IEnumerable<Spell> spells, SpellCriteria criteria)
    {
        var text = TextNormalizer.Normalize(criteria.Text);
        var school = TextNormalizer.Normalize(criteria.School);
        return [.. spells
            .Where(spell => text.Length == 0 || spell.SearchName.Contains(text, StringComparison.Ordinal))
            .Where(spell => criteria.Class is null || spell.Class == criteria.Class)
            .Where(spell => criteria.Level is null || spell.Level == criteria.Level)
            .Where(spell => school.Length == 0 || spell.Schools.Any(candidate => TextNormalizer.Normalize(candidate) == school))
            .OrderBy(spell => spell.SearchName, StringComparer.Ordinal)
            .ThenBy(spell => spell.Class)];
    }
}

/// <summary>Opciones de los filtros del Grimorio, sacadas de los conjuros cargados.</summary>
public sealed record SpellFacets(IReadOnlyList<int> Levels, IReadOnlyList<string> Schools)
{
    public static SpellFacets From(IEnumerable<Spell> spells)
    {
        var list = spells.ToList();
        return new SpellFacets(
            [.. list.Select(spell => spell.Level).Distinct().Order()],
            [.. list.SelectMany(spell => spell.Schools).Distinct(StringComparer.Ordinal).OrderBy(TextNormalizer.Normalize, StringComparer.Ordinal)]);
    }
}
