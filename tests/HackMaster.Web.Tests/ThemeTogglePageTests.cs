using Bunit;
using HackMaster.Web.Components;
using HackMaster.Web.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Botón de modo normal y noche (F007 US2: FR-003; specs/005-estilo-fantasia/contracts/theme.md).</summary>
public class ThemeTogglePageTests : BunitContext
{
    public ThemeTogglePageTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Theory]
    [InlineData("light", "Cambiar a modo noche")]
    [InlineData("dark", "Cambiar a modo normal")]
    public void The_Button_Says_Which_Mode_It_Switches_To__FR003(string current, string label)
    {
        JSInterop.Setup<string>("hackmasterTheme.current").SetResult(current);

        var cut = Render<ThemeToggle>();

        cut.WaitForAssertion(() => Assert.Equal(label, cut.Find("[data-testid=theme-toggle]").GetAttribute("aria-label")));
    }

    [Fact]
    public void Pressing_It_Asks_For_The_Other_Mode_And_Updates_Its_Name__FR003()
    {
        JSInterop.Setup<string>("hackmasterTheme.current").SetResult("light");
        var set = JSInterop.SetupVoid("hackmasterTheme.set", "dark");
        set.SetVoidResult();
        var cut = Render<ThemeToggle>();
        cut.WaitForAssertion(() => Assert.Equal("Cambiar a modo noche", cut.Find("[data-testid=theme-toggle]").GetAttribute("aria-label")));

        cut.Find("[data-testid=theme-toggle]").Click();

        set.VerifyInvoke("hackmasterTheme.set");
        Assert.Equal("Cambiar a modo normal", cut.Find("[data-testid=theme-toggle]").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_Button_Is_In_The_Navigation_Bar_Of_Every_Screen__FR003()
    {
        Services.AddSingleton(TestData.Create());
        Services.AddSingleton<BrowserStore>();
        JSInterop.Setup<string>("hackmasterTheme.current").SetResult("light");

        var cut = Render<MainLayout>();

        Assert.NotNull(cut.Find(".site-header [data-testid=theme-toggle]"));
    }
}
