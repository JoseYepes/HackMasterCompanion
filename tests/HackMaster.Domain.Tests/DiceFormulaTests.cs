using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Analizador de fórmulas de dados (FR-001 a FR-004, SC-002).</summary>
public class DiceFormulaTests
{
    /// <summary>Fórmula de 101 caracteres, válida salvo por la longitud: "1d6" y 49 veces "+1".</summary>
    private static readonly string TooLong = "1d6" + string.Concat(Enumerable.Repeat("+1", 49));

    public static TheoryData<string> Valid => new()
    {
        "2d6+3", "1d20+1d6-2", "d8", "2D6 + 3", "-1+1d4", "1d100", "100d6", "1d1000", "d20", "3d6",
        "1d4+1", "2d8-1", "1d6+1d6", "4d6-1d4+2", "1d12", "1d10+10000", "  1d6  ", "1d2", "50d6+50d6", "+1d6",
    };

    public static TheoryData<string> Invalid => new()
    {
        "", "  ", "2d", "d", "3x6", "2d6+", "0d6", "2d0", "2d1", "+3", "5", "101d6", "1d1001", "1d6+10001",
        TooLong, "2d6++3", "2d6 3", "2d6d6", "--3+1d4", "1d6-", "51d6+50d6", "1,5d6", "1d6+1.5", "1dd6", "1d6*2",
    };

    private static DiceFormula Valid_(string text)
    {
        var result = DiceFormula.Parse(text);
        Assert.Null(result.Error);
        return result.Formula!;
    }

    private static DiceFormulaError Invalid_(string text)
    {
        var result = DiceFormula.Parse(text);
        Assert.Null(result.Formula);
        return result.Error!;
    }

    [Fact]
    public void There_Are_At_Least_20_Valid_And_20_Invalid_Examples__SC002()
    {
        Assert.True(Valid.Count >= 20);
        Assert.True(Invalid.Count >= 20);
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void Valid_Formula_Is_Accepted__FR001(string text) => Valid_(text);

    [Theory]
    [MemberData(nameof(Invalid))]
    public void Invalid_Formula_Is_Rejected_With_Position_Fragment_Message_And_Example__FR003(string text)
    {
        var error = Invalid_(text);

        Assert.InRange(error.Position, 0, Math.Max(0, text.Length));
        Assert.NotNull(error.Fragment);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.Equal("2d6+3", DiceFormulaError.Example);
    }

    [Fact]
    public void Formula_Is_Split_Into_Signed_Dice_Groups_And_Numbers__FR001()
    {
        var formula = Valid_("1d20+1d6-2");

        Assert.Equal(
            [DiceTerm.Dice(1, 1, 20), DiceTerm.Dice(1, 1, 6), DiceTerm.Number(-1, 2)],
            formula.Terms);
    }

    [Fact]
    public void Count_Can_Be_Omitted_And_Means_One__FR001()
    {
        Assert.Equal([DiceTerm.Dice(1, 1, 8)], Valid_("d8").Terms);
    }

    [Fact]
    public void Spaces_And_Capitals_Are_Ignored__FR001()
    {
        var formula = Valid_("2D6 + 3");

        Assert.Equal("2d6+3", formula.Text);
        Assert.Equal(Valid_("2d6+3").Terms, formula.Terms);
    }

    [Theory]
    [InlineData("- 1d6", "-1d6")]
    [InlineData("+ 2d6", "2d6")]
    [InlineData("2 d6", "2d6")]
    [InlineData("2d 6", "2d6")]
    [InlineData(" 1d20 +  3 ", "1d20+3")]
    public void Spaces_Anywhere_Inside_A_Term_Are_Ignored__FR001(string text, string normalized)
    {
        Assert.Equal(normalized, Valid_(text).Text);
    }

    [Fact]
    public void Two_Signs_In_A_Row_Say_That_One_Is_Left_Over__FR003()
    {
        var error = Invalid_("2d6+-3");

        Assert.Equal(4, error.Position);
        Assert.Contains("Sobra «-»", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Leading_Zeros_Do_Not_Make_A_Number_Too_Big__FR001()
    {
        Assert.Equal([DiceTerm.Dice(1, 1, 6)], Valid_("0000000001d6").Terms);
    }

    [Fact]
    public void Length_Error_Points_Into_The_Written_Text_After_Leading_Spaces__FR004()
    {
        var text = "   " + TooLong;

        var error = Invalid_(text);

        Assert.Equal(3 + DiceFormula.MaxLength, error.Position);
        Assert.Equal(text[(3 + DiceFormula.MaxLength)..], error.Fragment);
    }

    [Fact]
    public void A_Leading_Sign_Applies_To_The_First_Term__FR001()
    {
        Assert.Equal([DiceTerm.Number(-1, 1), DiceTerm.Dice(1, 1, 4)], Valid_("-1+1d4").Terms);
    }

    [Theory]
    [InlineData("+3")]
    [InlineData("5")]
    public void Formula_Without_Dice_Is_Rejected__FR002(string text)
    {
        Assert.Contains("al menos un dado", Invalid_(text).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Formula_Asks_For_One__FR003(string text)
    {
        Assert.Contains("Escribe una fórmula", Invalid_(text).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_Sides_Points_At_The_D__FR003()
    {
        var error = Invalid_("2d");

        Assert.Equal(1, error.Position);
        Assert.Equal("d", error.Fragment);
        Assert.Contains("caras", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_Character_Is_Pointed_Out__FR003()
    {
        var error = Invalid_("3x6");

        Assert.Equal(1, error.Position);
        Assert.Equal("x", error.Fragment);
        Assert.Contains("«x» no vale", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Trailing_Operator_Asks_For_Another_Term__FR003()
    {
        var error = Invalid_("2d6+");

        Assert.Equal(3, error.Position);
        Assert.Equal("+", error.Fragment);
    }

    [Fact]
    public void Two_Terms_Without_An_Operator_Are_Rejected__FR003()
    {
        var error = Invalid_("2d6 3");

        Assert.Equal(4, error.Position);
        Assert.Contains("«+» o «-»", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0d6", "al menos 1")]
    [InlineData("2d0", "al menos 2 caras")]
    [InlineData("2d1", "al menos 2 caras")]
    [InlineData("101d6", "100 dados")]
    [InlineData("51d6+50d6", "100 dados")]
    [InlineData("1d1001", "1000 caras")]
    [InlineData("1d6+10001", "10000")]
    public void Values_Outside_The_Limits_Are_Rejected_Naming_The_Limit__FR004(string text, string limit)
    {
        Assert.Contains(limit, Invalid_(text).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Formula_Longer_Than_100_Characters_Is_Rejected__FR004()
    {
        Assert.Equal(101, TooLong.Length);
        Assert.Contains("100 caracteres", Invalid_(TooLong).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Exactly_100_Dice_Are_Allowed__FR004()
    {
        Assert.Equal(100, Valid_("50d6+50d6").Terms.Sum(term => term.Count));
    }
}
