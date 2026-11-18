using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Qué dados se dibujan en 3D para una tirada (FR-003, FR-006 a FR-008; data-model de F003).</summary>
public class DiceBoardTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 18, 12, 0, 0, TimeSpan.FromHours(1));

    private static BoardPlan Plan(string formula, bool penetrating, params int[] values) =>
        DiceBoard.Plan(DiceRoller.Roll(DiceFormula.Parse(formula).Formula!, penetrating, new SequenceRandom(values), At));

    private static IEnumerable<(DieShape Shape, string Face)> Faces(BoardPlan plan) => plan.Dice.Select(die => (die.Shape, die.Face));

    [Fact]
    public void Each_Physical_Die_Is_Drawn_With_Its_Shape_And_Face__FR003()
    {
        var plan = Plan("1d4+1d6+1d8+1d10+1d12+1d20", false, 3, 5, 7, 10, 11, 19);

        Assert.Equal(
            [(DieShape.D4, "3"), (DieShape.D6, "5"), (DieShape.D8, "7"), (DieShape.D10, "10"), (DieShape.D12, "11"), (DieShape.D20, "19")],
            Faces(plan));
        Assert.All(plan.Dice, die => Assert.Equal(0, die.Wave));
        Assert.False(plan.TooMany);
        Assert.False(plan.Truncated);
        Assert.Empty(plan.Unshaped);
    }

    [Theory]
    [InlineData(100, "00", "0")]
    [InlineData(7, "00", "7")]
    [InlineData(70, "70", "0")]
    [InlineData(10, "10", "0")]
    [InlineData(55, "50", "5")]
    public void D100_Is_Drawn_As_Tens_And_Units__FR003(int value, string tens, string units)
    {
        var plan = Plan("1d100", false, value);

        Assert.Equal([(DieShape.D10Tens, tens), (DieShape.D10, units)], Faces(plan));
    }

    [Fact]
    public void Dice_Without_A_Physical_Shape_Are_Not_Drawn__FR008()
    {
        var plan = Plan("1d2+1d3+1d1000+1d6", false, 2, 1, 500, 4);

        Assert.Equal([(DieShape.D6, "4")], Faces(plan));
        Assert.Equal(["1d2", "1d3", "1d1000"], plan.Unshaped.Select(term => term.ToString()));
    }

    [Fact]
    public void Numbers_Are_Not_Drawn__FR003()
    {
        Assert.Single(Plan("1d6+5", false, 2).Dice);
    }

    [Fact]
    public void Subtracted_Dice_Are_Drawn_With_Their_Face__FR003()
    {
        Assert.Equal([(DieShape.D20, "15"), (DieShape.D4, "3")], Faces(Plan("1d20-1d4", false, 15, 3)));
    }

    [Fact]
    public void Penetration_Extras_Roll_In_Later_Waves_After_Their_Die__FR006()
    {
        // 2d4: primer dado 4 → extra 4 → extra 2; segundo dado 1.
        var plan = Plan("2d4", true, 4, 4, 2, 1);

        Assert.Equal(
            [(DieShape.D4, "4", 0, false), (DieShape.D4, "1", 0, false), (DieShape.D4, "4", 1, true), (DieShape.D4, "2", 2, true)],
            plan.Dice.Select(die => (die.Shape, die.Face, die.Wave, die.IsPenetration)));
    }

    [Fact]
    public void A_Penetrating_D100_Adds_Two_D10_In_The_Next_Wave__FR006()
    {
        var plan = Plan("1d100", true, 100, 37);

        Assert.Equal(
            [(DieShape.D10Tens, "00", 0), (DieShape.D10, "0", 0), (DieShape.D10Tens, "30", 1), (DieShape.D10, "7", 1)],
            plan.Dice.Select(die => (die.Shape, die.Face, die.Wave)));
    }

    [Theory]
    [InlineData("20d6", 20)]
    [InlineData("10d100", 10)]
    public void Exactly_20_Dice_Are_Drawn__FR007(string formula, int rolls)
    {
        var plan = Plan(formula, false, [.. Enumerable.Repeat(3, rolls)]);

        Assert.Equal(DiceBoard.MaxDice, plan.Dice.Count);
        Assert.False(plan.TooMany);
    }

    [Theory]
    [InlineData("21d6", 21)]
    [InlineData("11d100", 11)]
    [InlineData("19d6+1d100", 20)]
    public void More_Than_20_Dice_At_The_Start_Draws_Nothing__FR007(string formula, int rolls)
    {
        var plan = Plan(formula, false, [.. Enumerable.Repeat(1, rolls)]);

        Assert.True(plan.TooMany);
        Assert.Empty(plan.Dice);
    }

    [Fact]
    public void Penetration_Extras_Beyond_20_Are_Cut__FR007()
    {
        // 19d4: el primero penetra tres veces (4, 4, 4, 1); los demás sacan 1. Serían 22 dados.
        var values = new[] { 4, 4, 4, 1 }.Concat(Enumerable.Repeat(1, 18)).ToArray();

        var plan = Plan("19d4", true, values);

        Assert.Equal(DiceBoard.MaxDice, plan.Dice.Count);
        Assert.True(plan.Truncated);
        Assert.Equal(1, plan.Dice.Count(die => die.IsPenetration));
    }

    [Fact]
    public void A_Cut_Never_Splits_The_Two_D10_Of_A_D100__FR007()
    {
        // 18d6 + 1d100 que penetra: 18 + 2 = 20 al empezar; el extra del d100 (2 dados) no cabe entero.
        var values = Enumerable.Repeat(2, 18).Concat([100, 50]).ToArray();

        var plan = Plan("18d6+1d100", true, values);

        Assert.Equal(20, plan.Dice.Count);
        Assert.True(plan.Truncated);
        Assert.DoesNotContain(plan.Dice, die => die.Wave > 0);
    }
}
