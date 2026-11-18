using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>La bandeja de dados en la página del tirador (US4: FR-017 a FR-021).</summary>
public class DiceTrayPageTests : BunitContext
{
    public DiceTrayPageTests()
    {
        Services.AddSingleton<IRandom>(new LoopRandom(3, 4));
        Services.AddSingleton<BrowserStore>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Dice> RenderPage()
    {
        var cut = Render<Dice>();
        cut.WaitForElement("[data-testid=tray-die-6]");
        return cut;
    }

    private static string Formula(IRenderedComponent<Dice> cut) => cut.Find("[data-testid=dice-formula]").GetAttribute("value") ?? string.Empty;

    [Fact]
    public void The_Tray_Has_A_Button_Per_Die_And_The_Modifier_And_Clear_Buttons__FR017()
    {
        var cut = RenderPage();

        foreach (var sides in DiceTray.Sides)
        {
            Assert.NotNull(cut.Find($"[data-testid=tray-die-{sides}]"));
        }

        Assert.NotNull(cut.Find("[data-testid=tray-plus]"));
        Assert.NotNull(cut.Find("[data-testid=tray-minus]"));
        Assert.NotNull(cut.Find("[data-testid=tray-clear]"));
    }

    [Fact]
    public void Clicks_Compose_The_Formula_And_Show_How_Many_Of_Each__SC007()
    {
        var cut = RenderPage();

        cut.Find("[data-testid=tray-die-6]").Click();
        cut.Find("[data-testid=tray-die-6]").Click();
        cut.Find("[data-testid=tray-die-6]").Click();
        cut.Find("[data-testid=tray-die-20]").Click();
        cut.Find("[data-testid=tray-plus]").Click();
        cut.Find("[data-testid=tray-plus]").Click();

        Assert.Equal("3d6+1d20+2", Formula(cut));
        Assert.Equal("3", cut.Find("[data-testid=tray-count-6]").TextContent.Trim());
        Assert.Equal("1", cut.Find("[data-testid=tray-count-20]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid=tray-count-8]"));
        Assert.Empty(cut.FindAll("[data-testid=dice-total]"));
    }

    [Fact]
    public void Clear_Empties_The_Formula__FR019()
    {
        var cut = RenderPage();
        cut.Find("[data-testid=tray-die-8]").Click();

        cut.Find("[data-testid=tray-clear]").Click();

        Assert.Equal(string.Empty, Formula(cut));
        Assert.Empty(cut.FindAll("[data-testid=tray-count-8]"));
    }

    [Fact]
    public void A_Written_Formula_It_Understands_Is_Continued__FR020()
    {
        var cut = RenderPage();
        cut.Find("[data-testid=dice-formula]").Input("2d6+3");

        cut.Find("[data-testid=tray-die-6]").Click();

        Assert.Equal("3d6+3", Formula(cut));
    }

    [Fact]
    public void A_Written_Formula_It_Does_Not_Understand_Is_Replaced_With_A_Notice__FR020()
    {
        var cut = RenderPage();
        cut.Find("[data-testid=dice-formula]").Input("1d3+1");

        cut.Find("[data-testid=tray-die-6]").Click();

        Assert.Equal("1d6", Formula(cut));
        Assert.Contains("fórmula nueva", cut.Find("[data-testid=tray-notice]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Dice_Limit_Is_Respected_With_A_Notice__FR021()
    {
        var cut = RenderPage();
        cut.Find("[data-testid=dice-formula]").Input("100d6");

        cut.Find("[data-testid=tray-die-6]").Click();

        Assert.Equal("100d6", Formula(cut));
        Assert.Contains("100 dados", cut.Find("[data-testid=tray-notice]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Rolling_Uses_The_Formula_Made_With_The_Tray__FR018()
    {
        var cut = RenderPage();
        cut.Find("[data-testid=tray-die-6]").Click();
        cut.Find("[data-testid=tray-die-6]").Click();
        cut.Find("[data-testid=tray-plus]").Click();

        cut.Find("form").Submit();

        Assert.Equal("8", cut.Find("[data-testid=dice-total]").TextContent.Trim());
    }
}
