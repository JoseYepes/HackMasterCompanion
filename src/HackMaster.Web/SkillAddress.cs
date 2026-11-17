using System.Globalization;
using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>
/// Traduce la consulta de habilidades a la cadena de consulta de la dirección y al revés
/// (<c>contracts/rutas.md</c>). Los parámetros desconocidos o con valores no válidos se ignoran.
/// </summary>
public static class SkillAddress
{
    /// <summary>Ruta de la lista de habilidades, relativa a la base.</summary>
    public const string Path = "habilidades";

    private static readonly (string Value, SkillSortField Field)[] SortNames =
    [
        ("nombre", SkillSortField.Name),
        ("pc", SkillSortField.Cost),
        ("precio", SkillSortField.Price),
        ("dificultad", SkillSortField.Difficulty),
    ];

    /// <summary>Lee la consulta de una dirección; solo se aceptan opciones que existen en los datos.</summary>
    public static SkillCriteria Parse(string uri, SkillFacets facets)
    {
        var parameters = QueryParameters(uri);

        return new SkillCriteria
        {
            Text = parameters["q"].LastOrDefault(),
            Types = Known(parameters["tipo"], facets.Types),
            Books = Known(parameters["libro"], facets.Books),
            Classes = Known(parameters["clase"], facets.Classes),
            PriceMin = Integer(parameters["precioMin"]),
            PriceMax = Integer(parameters["precioMax"]),
            DifficultyMin = Integer(parameters["difMin"]),
            DifficultyMax = Integer(parameters["difMax"]),
            Sort = ParseSort(parameters["orden"].LastOrDefault()) ?? SkillSort.Default,
        };
    }

    /// <summary>Dirección relativa (<c>habilidades?…</c>) que representa la consulta.</summary>
    public static string ToRelativeUri(SkillCriteria criteria)
    {
        var parts = new List<string>();
        Add(parts, "q", string.IsNullOrWhiteSpace(criteria.Text) ? null : criteria.Text);
        foreach (var value in criteria.Types.Order(StringComparer.Ordinal))
        {
            Add(parts, "tipo", value);
        }

        foreach (var value in criteria.Books.Order(StringComparer.Ordinal))
        {
            Add(parts, "libro", value);
        }

        foreach (var value in criteria.Classes.Order(StringComparer.Ordinal))
        {
            Add(parts, "clase", value);
        }

        Add(parts, "precioMin", Invariant(criteria.PriceMin));
        Add(parts, "precioMax", Invariant(criteria.PriceMax));
        Add(parts, "difMin", Invariant(criteria.DifficultyMin));
        Add(parts, "difMax", Invariant(criteria.DifficultyMax));
        Add(parts, "orden", criteria.Sort == SkillSort.Default ? null : SortValue(criteria.Sort));

        return parts.Count == 0 ? Path : Path + "?" + string.Join('&', parts);
    }

    /// <summary>Valor de <c>orden</c>: "precio", "-precio"…</summary>
    public static string SortValue(SkillSort sort) =>
        (sort.Descending ? "-" : string.Empty) + SortNames.Single(name => name.Field == sort.Field).Value;

    /// <summary>Orden a partir del valor de <c>orden</c>; nulo si no es válido.</summary>
    public static SkillSort? ParseSort(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var descending = value.StartsWith('-');
        var name = descending ? value[1..] : value;
        var match = SortNames.Where(sort => sort.Value == name).Select(sort => (SkillSortField?)sort.Field).FirstOrDefault();
        return match is null ? null : new SkillSort(match.Value, descending);
    }

    private static ILookup<string, string> QueryParameters(string uri)
    {
        var questionMark = uri.IndexOf('?', StringComparison.Ordinal);
        if (questionMark < 0)
        {
            return Array.Empty<KeyValuePair<string, string>>().ToLookup(pair => pair.Key, pair => pair.Value);
        }

        var query = uri[(questionMark + 1)..];
        var hash = query.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0)
        {
            query = query[..hash];
        }

        return query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Select(pair => new KeyValuePair<string, string>(Decode(pair[0]), pair.Length > 1 ? Decode(pair[1]) : string.Empty))
            .ToLookup(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));

    private static HashSet<string> Known(IEnumerable<string> values, IReadOnlyList<string> options) =>
        new(values.Where(options.Contains), StringComparer.Ordinal);

    private static int? Integer(IEnumerable<string> values) =>
        int.TryParse(values.LastOrDefault(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) ? number : null;

    private static string? Invariant(int? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static void Add(List<string> parts, string name, string? value)
    {
        if (value is not null)
        {
            parts.Add(name + "=" + Uri.EscapeDataString(value));
        }
    }
}
