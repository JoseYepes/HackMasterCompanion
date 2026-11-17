namespace HackMaster.Domain;

/// <summary>Opciones de los filtros de habilidades, sacadas de los datos.</summary>
/// <param name="Types">Tipos de habilidad.</param>
/// <param name="Books">Acrónimos de libro.</param>
/// <param name="Classes">Clases y grupos, sin "Todas".</param>
public sealed record SkillFacets(IReadOnlyList<string> Types, IReadOnlyList<string> Books, IReadOnlyList<string> Classes)
{
    /// <summary>Valor de clase que significa "sin restricción".</summary>
    public const string AllClasses = "Todas";

    /// <summary>
    /// Calcula las opciones de cada filtro, sin duplicados y en orden alfabético sin distinguir
    /// acentos. "Todas" no es una opción: esas habilidades se incluyen siempre en el filtro de clase.
    /// </summary>
    public static SkillFacets From(IEnumerable<Skill> skills)
    {
        var list = skills.ToList();
        return new SkillFacets(
            Sorted(list.Select(skill => skill.Type)),
            Sorted(list.SelectMany(skill => skill.Books).Select(book => book.Acronym)),
            Sorted(list.SelectMany(skill => skill.Classes).Where(@class => @class != AllClasses)));
    }

    private static IReadOnlyList<string> Sorted(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(TextNormalizer.Normalize, StringComparer.Ordinal)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToList();
}
