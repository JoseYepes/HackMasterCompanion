using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Dados de Honor de la Tabla 5C (MJ p. 85).</summary>
public class HonorDieTests
{
    [Fact]
    public void The_Honor_Dice_Are_Those_Of_Table_5C__MJ85()
    {
        Assert.Equal(
            ["1", "d3", "d4", "d4+1", "d6", "d6+1", "d8", "d8+1", "d10", "d10+1", "d12", "d12+1", "d12+2", "d20"],
            HonorDie.All.Select(die => die.ToString()));
    }

    [Fact]
    public void The_Cells_With_A_1_In_Table_5C_Are_A_Fixed_1__MJ85_PHB85()
    {
        // Interpretación del agente (2026-11-20): el MJ y el PHB en inglés ponen «1» donde no hay dado.
        var random = new SequenceRandom();

        Assert.True(HonorDie.TryParse("1", out var die));
        Assert.Equal(1, die.Roll(random));
        Assert.Empty(random.Requests);
    }

    [Theory]
    [InlineData("d12+1", 12, 1)]
    [InlineData("d4", 4, 0)]
    [InlineData(" D20 ", 20, 0)]
    public void An_Honor_Die_Of_The_Table_Is_Read_From_Its_Text__MJ85(string text, int sides, int bonus)
    {
        Assert.True(HonorDie.TryParse(text, out var die));
        Assert.Equal(new HonorDie(sides, bonus), die);
    }

    [Theory]
    [InlineData("d5")]
    [InlineData("d20+1")]
    [InlineData("d3+1")]
    [InlineData("2d6")]
    [InlineData("")]
    [InlineData(null)]
    public void A_Die_That_Is_Not_In_Table_5C_Is_Not_An_Honor_Die__MJ85(string? text)
    {
        Assert.False(HonorDie.TryParse(text, out _));
    }

    [Fact]
    public void Rolling_An_Honor_Die_Adds_Its_Bonus__MJ85()
    {
        var random = new SequenceRandom(7);

        var rolled = new HonorDie(12, 1).Roll(random);

        Assert.Equal(8, rolled);
        Assert.Equal((1, 12), random.Requests.Single());
    }
}
