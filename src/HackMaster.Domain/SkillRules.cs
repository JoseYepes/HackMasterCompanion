using System.Globalization;

namespace HackMaster.Domain;

/// <summary>Dado de Honor de la Tabla 5C (MJ p. 85): un dado y, a veces, un bono de +1 o +2.</summary>
public sealed record HonorDie(int Sides, int Bonus = 0)
{
    /// <summary>
    /// Los dados de Honor que aparecen en la Tabla 5C, de menor a mayor. Las casillas «1» de la
    /// tabla (Honor muy bajo) quedan pendientes de verificar y no se incluyen.
    /// </summary>
    public static IReadOnlyList<HonorDie> All { get; } =
    [
        new(3), new(4), new(4, 1), new(6), new(6, 1), new(8), new(8, 1),
        new(10), new(10, 1), new(12), new(12, 1), new(12, 2), new(20),
    ];

    /// <summary>Lee un dado de la tabla escrito como «d12+1»; falso si no es uno de ellos.</summary>
    public static bool TryParse(string? text, out HonorDie die)
    {
        var normalized = text?.Trim().ToLowerInvariant();
        die = All.FirstOrDefault(candidate => candidate.ToString() == normalized)!;
        return die is not null;
    }

    /// <summary>Tira el dado y suma su bono.</summary>
    public int Roll(IRandom random) => random.Next(1, Sides) + Bonus;

    public override string ToString() =>
        Bonus == 0 ? $"d{Sides.ToString(CultureInfo.InvariantCulture)}" : $"d{Sides.ToString(CultureInfo.InvariantCulture)}+{Bonus.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>
/// Una mejora de una habilidad: una tirada del dado de maestría y una del Dado de Honor, que se
/// suman a la maestría (MJ p. 100–101).
/// </summary>
public sealed record SkillImprovement(int Mastery, int Honor)
{
    public int Total => Mastery + Honor;
}

/// <summary>Niveles de maestría de la tabla del MJ p. 100.</summary>
public enum MasteryLevel
{
    NoIdea,
    Novice,
    Apprentice,
    Advanced,
    Expert,
    Master,
}

/// <summary>Resultado de una prueba de habilidad.</summary>
public enum SkillCheckOutcome
{
    Success,
    Failure,

    /// <summary>Un 01–05 siempre tiene éxito (MJ p. 356).</summary>
    AutomaticSuccess,

    /// <summary>Un 98–00 siempre falla (MJ p. 356).</summary>
    AutomaticFailure,
}

/// <summary>Una prueba de habilidad: el d100, el modificador, el total y el resultado.</summary>
public sealed record SkillCheck(int Roll, int Modifier, int Total, int Mastery, SkillCheckOutcome Outcome)
{
    public bool Succeeded => Outcome is SkillCheckOutcome.Success or SkillCheckOutcome.AutomaticSuccess;
}

/// <summary>Reglas de maestría y de prueba de habilidad del Manual del Jugador (F005).</summary>
public static class SkillRules
{
    /// <summary>La maestría no puede pasar de 125 % (MJ p. 101).</summary>
    public const int MaxMastery = 125;

    /// <summary>
    /// Límite del modificador de una prueba, a cada lado. No es una regla del manual: evita fórmulas
    /// que el tirador no acepta (revisión de F005); con d100 y maestría hasta 125, cubre de sobra.
    /// </summary>
    public const int MaxModifier = 1000;

    /// <summary>Verdadero si el modificador se puede usar en una prueba.</summary>
    public static bool IsValidModifier(int modifier) => Math.Abs(modifier) <= MaxModifier;

    /// <summary>
    /// Maestría base: la característica relevante o, si hay varias, su media redondeada a la baja
    /// (MJ p. 307). Cuenta la puntuación sin la parte fraccionaria. Nulo si falta alguna.
    /// </summary>
    public static int? BaseMastery(IReadOnlyList<Ability> relevant, CharacterAbilities abilities)
    {
        if (relevant.Count == 0)
        {
            return null;
        }

        var scores = relevant.Select(ability => abilities[ability].Score).ToList();
        return scores.Any(score => score is null) ? null : scores.Sum(score => score!.Value) / scores.Count;
    }

    /// <summary>
    /// Maestría: la base (una sola vez) más todas las mejoras, con tope de 125 % (MJ p. 100–101).
    /// Nulo si no se puede calcular la base.
    /// </summary>
    public static int? Mastery(IReadOnlyList<Ability> relevant, CharacterAbilities abilities, IReadOnlyList<SkillImprovement> improvements) =>
        BaseMastery(relevant, abilities) is { } baseMastery
            ? Math.Min(MaxMastery, baseMastery + improvements.Sum(improvement => improvement.Total))
            : null;

    /// <summary>Nivel de maestría según la tabla del MJ p. 100.</summary>
    public static MasteryLevel Level(int mastery) => mastery switch
    {
        <= 0 => MasteryLevel.NoIdea,
        <= 25 => MasteryLevel.Novice,
        <= 50 => MasteryLevel.Apprentice,
        <= 75 => MasteryLevel.Advanced,
        <= 100 => MasteryLevel.Expert,
        _ => MasteryLevel.Master,
    };

    /// <summary>Nombre del nivel como en el MJ p. 100.</summary>
    public static string LevelName(MasteryLevel level) => level switch
    {
        MasteryLevel.NoIdea => "ni idea",
        MasteryLevel.Novice => "principiante",
        MasteryLevel.Apprentice => "aprendiz",
        MasteryLevel.Advanced => "avanzado",
        MasteryLevel.Expert => "experto",
        _ => "maestro",
    };

    /// <summary>Tira una mejora: el dado de maestría de la habilidad y el Dado de Honor (MJ p. 101).</summary>
    public static SkillImprovement RollImprovement(int masteryDieSides, HonorDie honorDie, IRandom random) =>
        new(random.Next(1, masteryDieSides), honorDie.Roll(random));

    /// <summary>
    /// Prueba de habilidad (MJ p. 100 y Glosario, p. 356): la tirada más los modificadores debe ser
    /// igual o menor que la maestría; un 01–05 siempre tiene éxito y un 98–00 (98–100) siempre falla.
    /// </summary>
    public static SkillCheck Check(int roll, int modifier, int mastery)
    {
        var total = roll + modifier;
        var outcome = roll switch
        {
            <= 5 => SkillCheckOutcome.AutomaticSuccess,
            >= 98 => SkillCheckOutcome.AutomaticFailure,
            _ => total <= mastery ? SkillCheckOutcome.Success : SkillCheckOutcome.Failure,
        };
        return new SkillCheck(roll, modifier, total, mastery, outcome);
    }

    /// <summary>
    /// Tira la prueba con el tirador de la aplicación: 1d100 sin penetración y el modificador en la
    /// fórmula, para guardarla en el historial (F002).
    /// </summary>
    public static (RollResult Roll, SkillCheck Check) RollCheck(int mastery, int modifier, IRandom random, DateTimeOffset at)
    {
        if (!IsValidModifier(modifier))
        {
            throw new ArgumentOutOfRangeException(nameof(modifier), modifier, $"El modificador va de −{MaxModifier} a {MaxModifier}.");
        }

        var text = modifier switch
        {
            0 => "1d100",
            > 0 => $"1d100+{modifier.ToString(CultureInfo.InvariantCulture)}",
            _ => $"1d100{modifier.ToString(CultureInfo.InvariantCulture)}",
        };
        var formula = DiceFormula.Parse(text).Formula
            ?? throw new InvalidOperationException($"La fórmula de la prueba no es válida: {text}");
        var roll = DiceRoller.Roll(formula, penetrating: false, random, at);
        return (roll, Check(roll.Groups[0].Dice[0].Value, modifier, mastery));
    }
}
