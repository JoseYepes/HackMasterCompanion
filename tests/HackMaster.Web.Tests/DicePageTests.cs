using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Azar de prueba para la interfaz: repite los valores indicados en bucle.</summary>
public sealed class LoopRandom(params int[] values) : IRandom
{
    private int _next;

    public int Next(int minInclusive, int maxInclusive) => Math.Clamp(values[_next++ % values.Length], minInclusive, maxInclusive);
}

/// <summary>Página del tirador (US1: FR-001 a FR-007).</summary>
public class DicePageTests : BunitContext
{
    private readonly LoopRandom _random = new(3, 4);

    public DicePageTests()
    {
        Services.AddSingleton<IRandom>(_random);
        Services.AddSingleton<BrowserStore>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Dice> RollFormula(string formula)
    {
        var cut = Render<Dice>();
        cut.Find("[data-testid=dice-formula]").Input(formula);
        cut.Find("form").Submit();
        return cut;
    }

    [Fact]
    public void Rolling_2d6_Plus_3_Shows_The_Total_And_Each_Die__FR005()
    {
        var cut = RollFormula("2d6+3");

        Assert.Equal("10", cut.Find("[data-testid=dice-total]").TextContent.Trim());
        var detail = cut.Find("[data-testid=dice-detail]").TextContent;
        Assert.Contains("2d6", detail, StringComparison.Ordinal);
        Assert.Contains("3", detail, StringComparison.Ordinal);
        Assert.Contains("4", detail, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=dice-error]"));
    }

    [Fact]
    public void Invalid_Formula_Shows_The_Error_And_Does_Not_Roll__FR003()
    {
        var cut = RollFormula("2d");

        var error = cut.Find("[data-testid=dice-error]").TextContent;
        Assert.Contains("caras", error, StringComparison.Ordinal);
        Assert.Contains("2d6+3", error, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=dice-total]"));
    }

    [Fact]
    public void Error_Text_Separates_Its_Sentences__FR003()
    {
        var cut = RollFormula("2d");

        Assert.Contains(". Está en «d», posición 2. Ejemplo: 2d6+3", cut.Find("[data-testid=dice-error]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_Formula_Hides_The_Previous_Roll__FR003()
    {
        var cut = RollFormula("1d6");
        cut.Find("[data-testid=dice-formula]").Input("2d");

        cut.Find("form").Submit();

        Assert.Empty(cut.FindAll("[data-testid=dice-total]"));
    }

    [Fact]
    public void Repeat_Rolls_The_Same_Formula_Again__FR007()
    {
        var cut = RollFormula("1d6");
        Assert.Equal("3", cut.Find("[data-testid=dice-total]").TextContent.Trim());
        cut.Find("[data-testid=dice-formula]").Input("1d20+50");

        cut.Find("[data-testid=dice-repeat]").Click();

        Assert.Equal("4", cut.Find("[data-testid=dice-total]").TextContent.Trim());
    }

    [Fact]
    public void Repeat_Is_Not_Offered_Before_The_First_Roll__FR007()
    {
        var cut = Render<Dice>();

        Assert.Empty(cut.FindAll("[data-testid=dice-repeat]"));
    }

    [Fact]
    public void Penetrating_Option_Adds_A_Marked_Extra_Die__FR006()
    {
        Services.AddSingleton<IRandom>(new LoopRandom(4, 1));
        var cut = Render<Dice>();
        cut.Find("[data-testid=dice-penetrating]").Change(true);
        cut.Find("[data-testid=dice-formula]").Input("1d4");

        cut.Find("form").Submit();

        Assert.Equal("4", cut.Find("[data-testid=dice-total]").TextContent.Trim());
        Assert.Single(cut.FindAll("[data-testid=dice-detail] .penetration"));
    }

    [Fact]
    public void Formula_In_The_Address_Fills_The_Box_Without_Rolling__FR001()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("dados?f=2d6%2B3");

        var cut = Render<Dice>();

        Assert.Equal("2d6+3", cut.Find("[data-testid=dice-formula]").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-testid=dice-total]"));
    }
}
