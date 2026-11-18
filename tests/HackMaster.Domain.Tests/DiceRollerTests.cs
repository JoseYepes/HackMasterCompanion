using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Tirada de una fórmula, con y sin dados que penetran (FR-005, FR-006, SC-003).</summary>
public class DiceRollerTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 18, 12, 0, 0, TimeSpan.FromHours(1));

    private static DiceFormula Formula(string text) => DiceFormula.Parse(text).Formula!;

    private static RollResult Roll(string text, bool penetrating, params int[] values) =>
        DiceRoller.Roll(Formula(text), penetrating, new SequenceRandom(values), At);

    [Fact]
    public void Total_Is_The_Sum_Of_The_Dice_Plus_The_Numbers__FR005()
    {
        var result = Roll("2d6+3", false, 3, 4);

        Assert.Equal(10, result.Total);
        Assert.Equal(3, result.Constant);
        var group = Assert.Single(result.Groups);
        Assert.Equal([3, 4], group.Dice.Select(die => die.Value));
        Assert.Equal(7, group.Subtotal);
        Assert.Equal(At, result.RolledAt);
    }

    [Fact]
    public void Each_Group_Is_Rolled_Separately_And_Combined_With_Its_Sign__FR005()
    {
        var result = Roll("1d20+1d6-2", false, 15, 2);

        Assert.Equal([15, 2], result.Groups.Select(group => group.Subtotal));
        Assert.Equal(15, result.Total);
    }

    [Fact]
    public void A_Subtracted_Group_Has_A_Negative_Subtotal__FR005()
    {
        var result = Roll("1d20-1d4", false, 10, 3);

        Assert.Equal(-3, result.Groups[1].Subtotal);
        Assert.Equal(7, result.Total);
    }

    [Fact]
    public void Each_Die_Is_Asked_Between_1_And_Its_Sides__FR005()
    {
        var random = new SequenceRandom(1, 8);

        DiceRoller.Roll(Formula("1d4+1d8"), false, random, At);

        Assert.Equal([(1, 4), (1, 8)], random.Requests);
    }

    [Fact]
    public void Without_Penetration_A_Maximum_Adds_No_Die__FR006()
    {
        var result = Roll("2d4+1", false, 4, 4);

        Assert.Equal(2, result.Groups[0].Dice.Count);
        Assert.Equal(9, result.Total);
    }

    [Fact]
    public void Penetrating_Maximum_Adds_A_Die_Worth_Its_Value_Minus_One__MJ()
    {
        // 2d4+1: sale 4 (penetra) y 2; el dado extra saca 3, que vale 3 - 1 = 2.
        var result = Roll("2d4+1", true, 4, 3, 2);

        var dice = result.Groups[0].Dice;
        Assert.Equal([new DieRoll(4, 4, false), new DieRoll(4, 3, true), new DieRoll(4, 2, false)], dice);
        Assert.Equal(4 + 2 + 2, result.Groups[0].Subtotal);
        Assert.Equal(9, result.Total);
    }

    [Fact]
    public void Penetration_Repeats_While_The_Maximum_Keeps_Coming__MJ()
    {
        // 1d6: 6, extra 6 (vale 5, sigue), extra 1 (vale 0, para).
        var result = Roll("1d6", true, 6, 6, 1);

        Assert.Equal([false, true, true], result.Groups[0].Dice.Select(die => die.IsPenetration));
        Assert.Equal(6 + 5 + 0, result.Total);
        Assert.False(result.Groups[0].PenetrationCapped);
    }

    [Fact]
    public void Numbers_Do_Not_Penetrate__MJ()
    {
        var result = Roll("1d4+4", true, 1);

        Assert.Single(result.Groups[0].Dice);
        Assert.Equal(5, result.Total);
    }

    [Fact]
    public void Penetration_Stops_After_100_Extra_Dice_And_Says_So__FR006()
    {
        var values = Enumerable.Repeat(2, 101).ToArray();

        var result = Roll("1d2", true, values);

        Assert.Equal(1 + 100, result.Groups[0].Dice.Count);
        Assert.True(result.Groups[0].PenetrationCapped);
        Assert.Equal(2 + (100 * 1), result.Total);
    }

    [Fact]
    public void Result_Keeps_The_Formula_And_The_Option__FR005()
    {
        var result = Roll("1d6", true, 3);

        Assert.Equal("1d6", result.Formula.Text);
        Assert.True(result.Penetrating);
    }

    [Fact]
    public void Each_Face_Of_1d6_Comes_Up_Between_15_And_18_4_Percent_In_10000_Rolls__SC003()
    {
        var random = new SeededRandom(20261118);
        var formula = Formula("1d6");
        var counts = new int[7];

        for (var i = 0; i < 10_000; i++)
        {
            var value = DiceRoller.Roll(formula, false, random, At).Total;
            Assert.InRange(value, 1, 6);
            counts[value]++;
        }

        for (var face = 1; face <= 6; face++)
        {
            Assert.InRange(counts[face], 1500, 1840);
        }
    }
}
