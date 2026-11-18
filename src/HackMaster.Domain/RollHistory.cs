namespace HackMaster.Domain;

/// <summary>Historial de tiradas: la más reciente primero y como mucho <see cref="MaxRolls"/> (FR-016).</summary>
public static class RollHistory
{
    public const int MaxRolls = 100;

    /// <summary>Añade la tirada al principio y descarta las que pasen del máximo.</summary>
    public static RollHistoryFile Add(RollHistoryFile history, RollResult roll) =>
        history with { Rolls = [ToStored(roll), .. history.Rolls.Take(MaxRolls - 1)] };

    /// <summary>Vacía el historial; la opción de dados que penetran se conserva.</summary>
    public static RollHistoryFile Clear(RollHistoryFile history) => history with { Rolls = [] };

    /// <summary>Forma guardada de una tirada.</summary>
    public static StoredRoll ToStored(RollResult roll) => new(
        roll.Formula.Text,
        roll.Penetrating,
        roll.Total,
        roll.RolledAt,
        roll.Constant,
        [.. roll.Groups.Select(group => new StoredGroup(
            group.Term.Count,
            group.Term.Sides,
            group.Term.Sign,
            group.Subtotal,
            group.PenetrationCapped,
            [.. group.Dice.Select(die => new StoredDie(die.Value, die.IsPenetration))]))]);
}
