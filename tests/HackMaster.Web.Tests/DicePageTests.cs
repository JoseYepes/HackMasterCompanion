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

    private const string StoredHistory = """
        { "schemaVersion": 1, "penetrating": true, "rolls": [
          { "formula": "1d20+2", "penetrating": false, "total": 17, "rolledAt": "2026-11-18T10:05:00+01:00", "constant": 2,
            "groups": [ { "count": 1, "sides": 20, "sign": 1, "subtotal": 15, "capped": false, "dice": [ { "value": 15, "penetration": false } ] } ] },
          { "formula": "2d6", "penetrating": false, "total": 7, "rolledAt": "2026-11-18T10:00:00+01:00", "constant": 0,
            "groups": [ { "count": 2, "sides": 6, "sign": 1, "subtotal": 7, "capped": false, "dice": [ { "value": 3, "penetration": false }, { "value": 4, "penetration": false } ] } ] } ] }
        """;

    private IRenderedComponent<Dice> RenderWithHistory()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.rolls").SetResult(StoredHistory);
        var cut = Render<Dice>();
        cut.WaitForState(() => cut.FindAll("[data-testid=history-row]").Count == 2);
        return cut;
    }

    [Fact]
    public void History_Lists_Stored_Rolls_Newest_First_With_Formula_Detail_Total_And_Date__FR016()
    {
        var cut = RenderWithHistory();

        var rows = cut.FindAll("[data-testid=history-row]");
        Assert.Contains("1d20+2", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("17", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("15", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("18/11/2026 10:05", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("2d6", rows[1].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Penetrating_Option_Is_Restored_From_The_Device__FR017()
    {
        var cut = RenderWithHistory();

        Assert.True(cut.Find("[data-testid=dice-penetrating]").HasAttribute("checked"));
    }

    [Fact]
    public void A_New_Roll_Is_Saved_First_In_The_History__FR017()
    {
        var cut = RenderWithHistory();
        cut.Find("[data-testid=dice-formula]").Input("1d6");

        cut.Find("form").Submit();

        Assert.Equal(3, cut.FindAll("[data-testid=history-row]").Count);
        Assert.Contains("1d6", cut.FindAll("[data-testid=history-row]")[0].TextContent, StringComparison.Ordinal);
        var saved = JSInterop.Invocations["localStorage.setItem"].Last().Arguments;
        Assert.Equal("hackmaster.rolls", saved[0]);
        Assert.Contains("\"formula\": \"1d6\"", (string)saved[1]!, StringComparison.Ordinal);
    }

    [Fact]
    public void Choosing_A_History_Row_Copies_Its_Formula_Without_Rolling__FR016()
    {
        var cut = RenderWithHistory();

        cut.FindAll("[data-testid=history-row] button")[1].Click();

        Assert.Equal("2d6", cut.Find("[data-testid=dice-formula]").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-testid=dice-total]"));
    }

    [Fact]
    public void Clearing_The_History_Asks_First_And_Then_Empties_It__FR016()
    {
        var cut = RenderWithHistory();

        cut.Find("[data-testid=history-clear]").Click();
        Assert.Equal(2, cut.FindAll("[data-testid=history-row]").Count);
        cut.Find("[data-testid=history-clear-confirm]").Click();

        Assert.Empty(cut.FindAll("[data-testid=history-row]"));
        Assert.Contains("\"rolls\": []", (string)JSInterop.Invocations["localStorage.setItem"].Last().Arguments[1]!, StringComparison.Ordinal);
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
