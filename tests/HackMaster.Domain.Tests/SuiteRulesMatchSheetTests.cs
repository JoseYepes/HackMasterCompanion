using System.Text.Json;
using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>
/// Recalcula los 36 conjuntos de habilidades de <c>data/</c> a partir de sus componentes y los
/// compara con los valores de la hoja (SC-001, FR-015). Solo se admiten las excepciones de
/// <c>research.md</c> §1, cada una con su motivo; cualquier otra diferencia hace fallar la prueba.
/// </summary>
public sealed class SuiteRulesMatchSheetTests
{
    private const string Die = "dado";

    /// <summary>Excepciones conocidas: (slug, valor) → motivo.</summary>
    private static readonly Dictionary<(string Slug, string Value), string> Exceptions = new()
    {
        [("zoologo", Die)] = "la hoja suma 1 a la media del dado",
        [("escribano", Die)] = "la hoja suma 1 a la media del dado",
        [("hechicero", Die)] = "la hoja suma 1 a la media del dado",
        [("capitan-de-compania", Die)] = "Liderazgo básico y Liderazgo de pequeños grupos tienen el dado «5» pendiente",
        [("lider-de-escuadron-jefe-de-equipo", Die)] = "Liderazgo básico tiene el dado «5» pendiente",
        [("actor", "PC")] = "error de la hoja: la celda de PC de «Imitar voces» empieza por «C86=» y vale 0",
        [("mantenimiento", Die)] = "todas sus habilidades tienen dado «*»; la regla no da un dado y, según el usuario, es un error de la hoja",
    };

    private static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "data");

    private static T Read<T>(string fileName) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(DataDirectory, fileName)), GameDataJson.Options)!;

    private static readonly IReadOnlyList<Skill> Skills = Read<SkillsFile>("skills.json").Skills;
    private static readonly IReadOnlyList<SkillSuite> Suites = Read<SuitesFile>("suites.json").Suites;

    private static SuiteComponentValues ValuesOf(SuiteComponent component) => component.Kind switch
    {
        ElementKind.Skill => SuiteComponentValues.From(Skills.Single(skill => skill.Slug == component.Target)),
        ElementKind.Suite => SuiteComponentValues.From(Suites.Single(suite => suite.Kind == SuiteKind.Custom && suite.Slug == component.Target)),
        ElementKind.OfficialSuite => SuiteComponentValues.From(Suites.Single(suite => suite.Kind == SuiteKind.Official && suite.Slug == component.Target)),
        _ => throw new InvalidDataException($"Componente de tipo {component.Kind}."),
    };

    /// <summary>Diferencias entre la hoja y el cálculo: (slug, valor, hoja, cálculo).</summary>
    private static List<(string Slug, string Value, string Sheet, string Calculated)> Differences()
    {
        var differences = new List<(string, string, string, string)>();
        foreach (var suite in Suites.Where(suite => suite.Kind == SuiteKind.Custom))
        {
            var result = SuiteCalculator.Calculate([.. suite.Components.Select(ValuesOf)]);

            void Compare(string value, object? sheet, object? calculated)
            {
                if (!Equals(sheet, calculated))
                {
                    differences.Add((suite.Slug, value, sheet?.ToString() ?? "pendiente", calculated?.ToString() ?? "pendiente"));
                }
            }

            Compare("PC", suite.Cost, result.Cost);
            Compare("precio", suite.Price, result.Price);
            Compare("dificultad", suite.Difficulty, result.Difficulty);
            Compare(Die, suite.MasteryDie, result.MasteryDie);
        }

        return differences;
    }

    [Fact]
    public void There_Are_36_Suites_To_Compare__SC001()
    {
        Assert.Equal(36, Suites.Count(suite => suite.Kind == SuiteKind.Custom));
    }

    [Fact]
    public void Recalculated_Suites_Match_The_Sheet_Except_Named_Exceptions__SC001()
    {
        var unexpected = Differences()
            .Where(difference => !Exceptions.ContainsKey((difference.Slug, difference.Value)))
            .Select(difference => $"{difference.Slug}, {difference.Value}: hoja {difference.Sheet}, cálculo {difference.Calculated}");

        Assert.Empty(unexpected);
    }

    [Fact]
    public void Every_Named_Exception_Still_Differs__SC001()
    {
        var found = Differences().Select(difference => (difference.Slug, difference.Value)).ToHashSet();

        var stale = Exceptions.Keys.Where(key => !found.Contains(key)).Select(key => $"{key.Slug}, {key.Value}: {Exceptions[key]}");

        Assert.Empty(stale);
    }
}
