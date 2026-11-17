namespace HackMaster.Domain;

/// <summary>Campo por el que se ordena la lista de habilidades.</summary>
public enum SkillSortField
{
    /// <summary>Nombre (orden por defecto).</summary>
    Name,

    /// <summary>Coste en PC.</summary>
    Cost,

    /// <summary>Precio en piezas de oro.</summary>
    Price,

    /// <summary>Dificultad.</summary>
    Difficulty,
}

/// <summary>Orden de la lista: campo y sentido. Los valores que faltan van siempre al final.</summary>
public sealed record SkillSort(SkillSortField Field, bool Descending = false)
{
    /// <summary>Por nombre, ascendente.</summary>
    public static SkillSort Default { get; } = new(SkillSortField.Name);
}

/// <summary>Por dónde coincide una habilidad con el texto buscado.</summary>
public enum SkillMatchKind
{
    /// <summary>Por el nombre (o sin texto de búsqueda).</summary>
    Name,

    /// <summary>Solo por el resumen.</summary>
    Summary,
}

/// <summary>Búsqueda, filtros y orden de la lista de habilidades.</summary>
public sealed record SkillCriteria
{
    private static readonly IReadOnlySet<string> None = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Sin búsqueda ni filtros, ordenado por nombre.</summary>
    public static SkillCriteria Empty { get; } = new();

    /// <summary>Texto buscado en el nombre y el resumen.</summary>
    public string? Text { get; init; }

    /// <summary>Tipos marcados; basta con uno.</summary>
    public IReadOnlySet<string> Types { get; init; } = None;

    /// <summary>Acrónimos de libro marcados; basta con uno.</summary>
    public IReadOnlySet<string> Books { get; init; } = None;

    /// <summary>Clases o grupos marcados; basta con uno. Siempre incluye las habilidades de "Todas".</summary>
    public IReadOnlySet<string> Classes { get; init; } = None;

    /// <summary>Precio mínimo en po.</summary>
    public int? PriceMin { get; init; }

    /// <summary>Precio máximo en po.</summary>
    public int? PriceMax { get; init; }

    /// <summary>Dificultad mínima en %.</summary>
    public int? DifficultyMin { get; init; }

    /// <summary>Dificultad máxima en %.</summary>
    public int? DifficultyMax { get; init; }

    /// <summary>Orden de los resultados.</summary>
    public SkillSort Sort { get; init; } = SkillSort.Default;

    /// <summary>Verdadero si hay texto de búsqueda o algún filtro; el orden no cuenta.</summary>
    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Text)
        || Types.Count > 0
        || Books.Count > 0
        || Classes.Count > 0
        || PriceMin is not null
        || PriceMax is not null
        || DifficultyMin is not null
        || DifficultyMax is not null;
}

/// <summary>Habilidad que cumple la consulta.</summary>
/// <param name="Skill">Habilidad.</param>
/// <param name="MatchedBy">Por dónde coincide con el texto buscado.</param>
/// <param name="Snippet">Fragmento del resumen con la coincidencia, solo si coincide por resumen.</param>
public sealed record SkillMatch(Skill Skill, SkillMatchKind MatchedBy, string? Snippet = null);

/// <summary>Aplica la búsqueda, los filtros y el orden a la lista de habilidades.</summary>
public static class SkillQuery
{
    /// <summary>Caracteres de contexto a cada lado de la coincidencia en el fragmento.</summary>
    private const int SnippetContext = 40;

    /// <summary>
    /// Devuelve las habilidades que cumplen el criterio. Con texto de búsqueda, las que coinciden
    /// por nombre van antes que las que solo coinciden por resumen; dentro de cada grupo se aplica
    /// el orden pedido.
    /// </summary>
    public static IReadOnlyList<SkillMatch> Apply(IEnumerable<Skill> skills, SkillCriteria criteria)
    {
        var text = TextNormalizer.Normalize(criteria.Text);
        var matches = new List<SkillMatch>();

        foreach (var skill in skills)
        {
            if (!PassesFilters(skill, criteria))
            {
                continue;
            }

            if (text.Length == 0 || skill.SearchName.Contains(text, StringComparison.Ordinal))
            {
                matches.Add(new SkillMatch(skill, SkillMatchKind.Name));
            }
            else if (skill.SearchSummary?.Contains(text, StringComparison.Ordinal) == true)
            {
                matches.Add(new SkillMatch(skill, SkillMatchKind.Summary, Snippet(skill.Summary!, text)));
            }
        }

        return matches
            .OrderBy(match => match.MatchedBy)
            .ThenBy(match => match.Skill, new SkillComparer(criteria.Sort))
            .ToList();
    }

    private static bool PassesFilters(Skill skill, SkillCriteria criteria) =>
        (criteria.Types.Count == 0 || criteria.Types.Contains(skill.Type))
        && (criteria.Books.Count == 0 || skill.Books.Any(book => criteria.Books.Contains(book.Acronym)))
        && (criteria.Classes.Count == 0 || skill.Classes.Any(@class => @class == SkillFacets.AllClasses || criteria.Classes.Contains(@class)))
        && InRange(skill.Price, criteria.PriceMin, criteria.PriceMax)
        && InRange(skill.Difficulty, criteria.DifficultyMin, criteria.DifficultyMax);

    private static bool InRange(int? value, int? min, int? max)
    {
        if (min is null && max is null)
        {
            return true;
        }

        return value is not null && (min is null || value >= min) && (max is null || value <= max);
    }

    /// <summary>
    /// Fragmento del resumen original alrededor de la coincidencia. Se busca sobre el texto
    /// normalizado y se traduce la posición al texto original.
    /// </summary>
    private static string Snippet(string summary, string normalizedText)
    {
        var (normalized, map) = NormalizeWithMap(summary);
        var index = normalized.IndexOf(normalizedText, StringComparison.Ordinal);
        if (index < 0)
        {
            return summary;
        }

        var start = map[index];
        var end = map[index + normalizedText.Length - 1] + 1;
        var from = WordStart(summary, Math.Max(0, start - SnippetContext));
        var to = WordEnd(summary, Math.Min(summary.Length, end + SnippetContext));

        var snippet = summary[from..to].Trim();
        return (from > 0 ? "…" : string.Empty) + snippet + (to < summary.Length ? "…" : string.Empty);
    }

    /// <summary>Normaliza carácter a carácter y guarda, para cada carácter normalizado, su posición original.</summary>
    private static (string Normalized, List<int> Map) NormalizeWithMap(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        var map = new List<int>(text.Length);
        var pendingSpace = false;

        for (var position = 0; position < text.Length; position++)
        {
            var character = text[position];
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                map.Add(position - 1);
                pendingSpace = false;
            }

            foreach (var normalized in TextNormalizer.Normalize(character.ToString()))
            {
                builder.Append(normalized);
                map.Add(position);
            }
        }

        return (builder.ToString(), map);
    }

    private static int WordStart(string text, int position)
    {
        while (position > 0 && !char.IsWhiteSpace(text[position - 1]))
        {
            position--;
        }

        return position;
    }

    private static int WordEnd(string text, int position)
    {
        while (position < text.Length && !char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    /// <summary>Compara según el orden pedido; los valores que faltan van al final y el nombre desempata.</summary>
    private sealed class SkillComparer(SkillSort sort) : IComparer<Skill>
    {
        public int Compare(Skill? x, Skill? y)
        {
            if (x is null || y is null)
            {
                return Comparer<Skill?>.Default.Compare(x, y);
            }

            var byField = sort.Field switch
            {
                SkillSortField.Cost => CompareValues(x.Cost, y.Cost),
                SkillSortField.Price => CompareValues(x.Price, y.Price),
                SkillSortField.Difficulty => CompareValues(x.Difficulty, y.Difficulty),
                _ => Directed(string.CompareOrdinal(x.SearchName, y.SearchName)),
            };

            return byField != 0 ? byField : string.CompareOrdinal(x.SearchName, y.SearchName);
        }

        private int CompareValues(int? x, int? y) => (x, y) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            _ => Directed(x.Value.CompareTo(y.Value)),
        };

        private int Directed(int comparison) => sort.Descending ? -comparison : comparison;
    }
}
