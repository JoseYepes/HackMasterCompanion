using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Bandeja de dados: componer la fórmula a golpe de clic (US4: FR-017 a FR-021, SC-007).</summary>
public class DiceTrayTests
{
    private static DiceTray Tray(string text) => DiceTray.FromText(text) ?? throw new InvalidOperationException($"No entiende «{text}».");

    private static DiceTray Ok(TrayChange change)
    {
        Assert.Null(change.Problem);
        return change.Tray;
    }

    [Fact]
    public void An_Empty_Formula_Is_An_Empty_Tray__FR020()
    {
        Assert.Equal(string.Empty, Tray(string.Empty).ToText());
        Assert.Equal(string.Empty, Tray("   ").ToText());
    }

    [Fact]
    public void Clicking_The_Same_Die_Increases_Its_Count__FR018()
    {
        var tray = Ok(Ok(Ok(DiceTray.Empty.Add(6)).Add(6)).Add(6));

        Assert.Equal("3d6", tray.ToText());
        Assert.Equal(3, tray.CountOf(6));
    }

    [Fact]
    public void Dice_Are_Written_From_Fewer_To_More_Sides_With_The_Modifier_Last__FR018()
    {
        var tray = Ok(Ok(Ok(Ok(DiceTray.Empty.Add(20)).Add(6)).AddModifier(1)).Add(100));

        Assert.Equal("1d6+1d20+1d100+1", tray.ToText());
    }

    [Fact]
    public void Three_D6_One_D20_And_Plus_Two_Take_Six_Clicks__SC007()
    {
        var tray = DiceTray.Empty;
        foreach (var click in new Func<DiceTray, TrayChange>[] { t => t.Add(6), t => t.Add(6), t => t.Add(6), t => t.Add(20), t => t.AddModifier(1), t => t.AddModifier(1) })
        {
            tray = Ok(click(tray));
        }

        Assert.Equal("3d6+1d20+2", tray.ToText());
    }

    [Theory]
    [InlineData(-1, "1d8-1")]
    [InlineData(0, "1d8")]
    public void The_Modifier_Can_Go_Down_To_Zero_Or_Below__FR019(int delta, string expected)
    {
        Assert.Equal(expected, Ok(Tray("1d8").AddModifier(delta)).ToText());
    }

    [Fact]
    public void A_Written_Formula_It_Understands_Is_The_Starting_Point__FR020()
    {
        var tray = Ok(Tray("2d6 + 3").Add(6));

        Assert.Equal("3d6+3", tray.ToText());
    }

    [Fact]
    public void Repeated_Groups_And_Numbers_Are_Added_Together__FR020()
    {
        Assert.Equal("3d6+1", Tray("1d6+2d6+3-2").ToText());
    }

    [Theory]
    [InlineData("+1", "+1")]
    [InlineData("-2", "-2")]
    [InlineData("5", "+5")]
    public void A_Lone_Number_Is_Only_A_Modifier__FR020(string text, string expected)
    {
        Assert.Equal(expected, Tray(text).ToText());
    }

    [Theory]
    [InlineData("1d3+1d6")]
    [InlineData("1d20-1d4")]
    [InlineData("2d")]
    [InlineData("hola")]
    public void Formulas_Outside_The_Tray_Are_Not_Understood__FR020(string text)
    {
        Assert.Null(DiceTray.FromText(text));
    }

    [Fact]
    public void Adding_A_Die_Beyond_100_Dice_Changes_Nothing_And_Says_Why__FR021()
    {
        var tray = Tray("100d6");

        var change = tray.Add(6);

        Assert.Equal("100d6", change.Tray.ToText());
        Assert.Contains("100 dados", change.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Modifier_Cannot_Go_Beyond_10000__FR021()
    {
        var change = Tray("1d6+10000").AddModifier(1);

        Assert.Equal("1d6+10000", change.Tray.ToText());
        Assert.Contains("10000", change.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Tray_Offers_The_Physical_Dice__FR017()
    {
        Assert.Equal([4, 6, 8, 10, 12, 20, 100], DiceTray.Sides);
    }

    [Fact]
    public void Only_Dice_Of_The_Tray_Can_Be_Added__FR017()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DiceTray.Empty.Add(7));
    }
}
