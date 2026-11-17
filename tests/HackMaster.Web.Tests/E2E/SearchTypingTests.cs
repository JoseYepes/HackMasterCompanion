using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Escribir en un buscador actualiza la dirección; la aplicación no debe devolver el cuadro a un
/// texto anterior aunque la búsqueda lleve tildes o espacios (US2, US3).
/// </summary>
[Trait("Category", "E2E")]
public sealed class SearchTypingTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    private const string Text = "cartógrafo de mazmorras";

    [E2EFact]
    public async Task Typing_Fast_In_The_Suites_Search_Keeps_Every_Letter__US3()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "conjuntos");
        var input = page.Locator("[data-testid=search-input]");

        await input.PressSequentiallyAsync(Text, new LocatorPressSequentiallyOptions { Delay = 30 });
        await page.WaitForTimeoutAsync(1000);

        await Assertions.Expect(input).ToHaveValueAsync(Text);
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"q=cart%C3%B3grafo(%20|\+)de(%20|\+)mazmorras$"));
    }

    [E2EFact]
    public async Task Typing_With_Pauses_In_The_Skills_Search_Keeps_Every_Letter__US2()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades");
        var input = page.Locator("[data-testid=search-input]");

        // Pausas cercanas a la espera del buscador (150 ms), para que las navegaciones se crucen.
        await input.PressSequentiallyAsync(Text, new LocatorPressSequentiallyOptions { Delay = 160 });
        await page.WaitForTimeoutAsync(1000);

        await Assertions.Expect(input).ToHaveValueAsync(Text);
    }
}
