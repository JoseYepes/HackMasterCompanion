using System.Globalization;

namespace HackMaster.Domain;

/// <summary>
/// Valores de un componente que intervienen en el cálculo de un conjunto. Un valor nulo es un dato
/// que falta o no es numérico ("NA" o pendiente de verificar); el dado es "d6", "NA", "*" o nulo.
/// </summary>
public sealed record SuiteComponentValues(
    string Name,
    int? Cost,
    int? Price,
    int? Difficulty,
    string? MasteryDie,
    IReadOnlyList<string> Classes,
    IReadOnlyList<Ability> Abilities)
{
    public static SuiteComponentValues From(Skill skill) => new(
        skill.Name,
        skill.Cost,
        skill.Price,
        skill.Difficulty,
        skill.Pending.Contains("masteryDie") ? null : skill.MasteryDie,
        skill.Classes,
        skill.Abilities);

    public static SuiteComponentValues From(SkillSuite suite) => new(
        suite.Name,
        suite.Cost,
        suite.Price,
        suite.Difficulty,
        suite.Pending.Contains("masteryDie") ? null : suite.MasteryDie,
        suite.Classes,
        suite.Abilities);
}

/// <summary>
/// Valores calculados de un conjunto. Cada valor nulo lleva su motivo en la propiedad
/// <c>…Pending</c>, que se muestra como "pendiente de verificar" (FR-013).
/// </summary>
public sealed record SuiteCalculation(
    int? Cost,
    string? CostPending,
    int? Price,
    string? PricePending,
    int? Difficulty,
    string? DifficultyPending,
    string? MasteryDie,
    string? MasteryDiePending,
    IReadOnlyList<string> Classes,
    string? ClassesPending,
    IReadOnlyList<Ability> AvailableAbilities);

/// <summary>
/// Reglas de los conjuntos de habilidades, tomadas de las fórmulas de la hoja del usuario, que las
/// recoge de HJ 4.05 (research.md §1 a §4).
/// </summary>
public static class SuiteCalculator
{
    public const int MinComponents = 2;
    public const int MaxComponents = 5;
    public const string AllClasses = "Todas";

    private static readonly int[] RealDice = [4, 6, 8, 10, 12, 20];

    public static SuiteCalculation Calculate(IReadOnlyList<SuiteComponentValues> components)
    {
        var (cost, costPending) = Sum(components, component => component.Cost, "el coste en PC");
        var (price, pricePending) = Sum(components, component => component.Price, "el precio");
        var (difficulty, difficultyPending) = Highest(components);
        var (die, diePending) = MasteryDie(components);
        var (classes, classesPending) = Classes(components);

        return new SuiteCalculation(
            cost - 1,
            costPending,
            price is null ? null : (int)Math.Round(price.Value * 1.1m, MidpointRounding.AwayFromZero),
            pricePending,
            difficulty + 5,
            difficultyPending,
            die,
            diePending,
            classes,
            classesPending,
            [.. components.SelectMany(component => component.Abilities).Distinct()]);
    }

    /// <summary>
    /// Motivo por el que los componentes no forman un conjunto que se pueda guardar, o nulo si sí:
    /// de 2 a 5 componentes, sin repetidos y sin que el conjunto se contenga a sí mismo (FR-010, FR-011).
    /// </summary>
    /// <param name="self">El conjunto que se edita, si ya existe.</param>
    /// <param name="childrenOf">Componentes de un conjunto del jugador; vacío para lo demás.</param>
    public static string? CheckComposition(
        IReadOnlyList<SuiteComponentRef> components,
        SuiteComponentRef? self,
        Func<SuiteComponentRef, IReadOnlyList<SuiteComponentRef>> childrenOf)
    {
        if (components.Count is < MinComponents or > MaxComponents)
        {
            return $"Un conjunto debe tener entre {MinComponents} y {MaxComponents} componentes; ahora tiene {components.Count}.";
        }

        if (components.Distinct().Count() != components.Count)
        {
            return "Hay un componente repetido.";
        }

        if (self is not null && Reaches(components, self, childrenOf, []))
        {
            return "Un conjunto no puede incluirse a sí mismo, ni a través de otro conjunto.";
        }

        return null;
    }

    private static bool Reaches(
        IReadOnlyList<SuiteComponentRef> components,
        SuiteComponentRef target,
        Func<SuiteComponentRef, IReadOnlyList<SuiteComponentRef>> childrenOf,
        HashSet<SuiteComponentRef> visited)
    {
        foreach (var component in components)
        {
            if (component == target)
            {
                return true;
            }

            if (visited.Add(component) && Reaches(childrenOf(component), target, childrenOf, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static (int? Value, string? Pending) Sum(IReadOnlyList<SuiteComponentValues> components, Func<SuiteComponentValues, int?> value, string what)
    {
        if (components.Count == 0)
        {
            return (null, "Sin componentes.");
        }

        var missing = components.Where(component => value(component) is null).Select(component => component.Name).ToList();
        return missing.Count > 0
            ? (null, $"Falta {what} de {Join(missing)}.")
            : (components.Sum(component => value(component)!.Value), null);
    }

    private static (int? Value, string? Pending) Highest(IReadOnlyList<SuiteComponentValues> components)
    {
        if (components.Count == 0)
        {
            return (null, "Sin componentes.");
        }

        var missing = components.Where(component => component.Difficulty is null).Select(component => component.Name).ToList();
        return missing.Count > 0
            ? (null, $"Falta la dificultad de {Join(missing)}.")
            : (components.Max(component => component.Difficulty!.Value), null);
    }

    /// <summary>
    /// Media de las caras de los dados numéricos (como mínimo 4), redondeada a la baja al dado real
    /// más grande que no la supere. "NA" y "*" no cuentan; un dado pendiente deja pendiente el resultado.
    /// </summary>
    private static (string? Die, string? Pending) MasteryDie(IReadOnlyList<SuiteComponentValues> components)
    {
        var pending = components.Where(component => component.MasteryDie is null).Select(component => component.Name).ToList();
        if (pending.Count > 0)
        {
            return (null, $"El dado de maestría de {Join(pending)} está pendiente de verificar.");
        }

        var sides = components.Select(component => Sides(component.MasteryDie!)).OfType<int>().ToList();
        if (sides.Count == 0)
        {
            return (null, "Ningún componente tiene un dado de maestría numérico.");
        }

        var average = Math.Max(4m, (decimal)sides.Sum() / sides.Count);
        return ($"d{RealDice.Last(die => die <= average).ToString(CultureInfo.InvariantCulture)}", null);
    }

    private static int? Sides(string die) =>
        die.Length > 1 && die[0] == 'd' && int.TryParse(die.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var sides) ? sides : null;

    /// <summary>
    /// "Todas" no restringe. Las restricciones de los componentes se cruzan: quedan las clases que
    /// aparecen en todas. Si no queda ninguna, no se puede deducir con los datos (FR-012b).
    /// </summary>
    private static (IReadOnlyList<string> Classes, string? Pending) Classes(IReadOnlyList<SuiteComponentValues> components)
    {
        var restricted = components
            .Where(component => component.Classes.Count > 0 && !component.Classes.Contains(AllClasses))
            .ToList();
        if (restricted.Count == 0)
        {
            return ([AllClasses], null);
        }

        var shared = restricted
            .Select(component => (IEnumerable<string>)component.Classes)
            .Aggregate((left, right) => left.Intersect(right, StringComparer.Ordinal))
            .ToList();
        return shared.Count > 0
            ? (shared, null)
            : ([], "No se puede saber qué clases pueden comprarlo: " + string.Join("; ", restricted.Select(component => $"{component.Name}: {string.Join(", ", component.Classes)}")) + ".");
    }

    private static string Join(IReadOnlyList<string> names) => string.Join(", ", names.Select(name => $"«{name}»"));
}
