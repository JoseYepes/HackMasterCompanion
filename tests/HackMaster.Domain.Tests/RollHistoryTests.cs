using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Historial de tiradas: orden, máximo de 100 y borrado (FR-016).</summary>
public class RollHistoryTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 18, 12, 0, 0, TimeSpan.FromHours(1));

    private static RollResult Roll(string formula, bool penetrating, params int[] values) =>
        DiceRoller.Roll(DiceFormula.Parse(formula).Formula!, penetrating, new SequenceRandom(values), At);

    [Fact]
    public void New_Roll_Goes_First__FR016()
    {
        var history = RollHistory.Add(RollHistoryFile.Empty, Roll("1d6", false, 2));

        history = RollHistory.Add(history, Roll("1d20", false, 15));

        Assert.Equal(["1d20", "1d6"], history.Rolls.Select(roll => roll.Formula));
    }

    [Fact]
    public void The_101st_Roll_Drops_The_Oldest__FR016()
    {
        var history = RollHistoryFile.Empty;
        for (var i = 1; i <= 100; i++)
        {
            history = RollHistory.Add(history, Roll($"1d6+{i}", false, 1));
        }

        history = RollHistory.Add(history, Roll("1d4", false, 1));

        Assert.Equal(RollHistory.MaxRolls, history.Rolls.Count);
        Assert.Equal("1d4", history.Rolls[0].Formula);
        Assert.Equal("1d6+2", history.Rolls[^1].Formula);
    }

    [Fact]
    public void Clear_Empties_The_History_And_Keeps_The_Option__FR016()
    {
        var history = RollHistory.Add(RollHistoryFile.Empty with { Penetrating = true }, Roll("1d6", false, 2));

        history = RollHistory.Clear(history);

        Assert.Empty(history.Rolls);
        Assert.True(history.Penetrating);
    }

    [Fact]
    public void Stored_Roll_Keeps_Formula_Total_Date_And_Every_Die__FR016()
    {
        var stored = RollHistory.ToStored(Roll("2d4+1", true, 4, 3, 2));

        Assert.Equal("2d4+1", stored.Formula);
        Assert.True(stored.Penetrating);
        Assert.Equal(9, stored.Total);
        Assert.Equal(1, stored.Constant);
        Assert.Equal(At, stored.RolledAt);
        var group = Assert.Single(stored.Groups);
        Assert.Equal((2, 4, 1, 8), (group.Count, group.Sides, group.Sign, group.Subtotal));
        Assert.Equal([new StoredDie(4, false), new StoredDie(3, true), new StoredDie(2, false)], group.Dice);
    }
}
