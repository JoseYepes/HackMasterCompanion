namespace HackMaster.Domain;

/// <summary>
/// Un dado tirado. Un dado extra por penetración (<see cref="IsPenetration"/>) aporta
/// <see cref="Value"/> menos 1 (MJ, «Daño de penetración»).
/// </summary>
public sealed record DieRoll(int Sides, int Value, bool IsPenetration)
{
    /// <summary>Lo que el dado suma al grupo.</summary>
    public int Contribution => IsPenetration ? Value - 1 : Value;
}

/// <summary>Un grupo de dados tirado: sus dados (con los extra en su sitio) y su subtotal con signo.</summary>
public sealed record GroupRoll(DiceTerm Term, IReadOnlyList<DieRoll> Dice, int Subtotal, bool PenetrationCapped);

/// <summary>Resultado de tirar una fórmula.</summary>
public sealed record RollResult(
    DiceFormula Formula,
    bool Penetrating,
    IReadOnlyList<GroupRoll> Groups,
    int Constant,
    int Total,
    DateTimeOffset RolledAt);

/// <summary>Tira una fórmula de dados con el azar que se le pasa (FR-005, FR-006).</summary>
public static class DiceRoller
{
    /// <summary>Dados extra por penetración que puede añadir cada dado como mucho (FR-006).</summary>
    public const int MaxPenetrationsPerDie = 100;

    public static RollResult Roll(DiceFormula formula, bool penetrating, IRandom random, DateTimeOffset rolledAt)
    {
        var groups = new List<GroupRoll>();
        var constant = 0;

        foreach (var term in formula.Terms)
        {
            if (!term.IsDice)
            {
                constant += term.Sign * term.Constant;
                continue;
            }

            var dice = new List<DieRoll>();
            var capped = false;
            for (var i = 0; i < term.Count; i++)
            {
                capped |= RollDie(term.Sides, penetrating, random, dice);
            }

            groups.Add(new GroupRoll(term, dice, term.Sign * dice.Sum(die => die.Contribution), capped));
        }

        return new RollResult(formula, penetrating, groups, constant, groups.Sum(group => group.Subtotal) + constant, rolledAt);
    }

    /// <summary>Tira un dado y, si penetra, sus dados extra. Devuelve si se alcanzó el límite.</summary>
    private static bool RollDie(int sides, bool penetrating, IRandom random, List<DieRoll> dice)
    {
        var value = random.Next(1, sides);
        dice.Add(new DieRoll(sides, value, false));
        if (!penetrating)
        {
            return false;
        }

        for (var extra = 0; value == sides; extra++)
        {
            if (extra == MaxPenetrationsPerDie)
            {
                return true;
            }

            value = random.Next(1, sides);
            dice.Add(new DieRoll(sides, value, true));
        }

        return false;
    }
}
