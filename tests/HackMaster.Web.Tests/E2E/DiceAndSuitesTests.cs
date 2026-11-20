using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>Flujos críticos de F002 con los datos reales: tirar, recargar y crear un conjunto (SC-001, SC-005).</summary>
[Trait("Category", "E2E")]
public sealed partial class DiceAndSuitesTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex Number();

    private static async Task AddComponentAsync(IPage page, string search, string name)
    {
        await page.Locator("[data-testid=suite-search]").FillAsync(search);
        var result = page.Locator("[data-testid=suite-result]").Filter(new LocatorFilterOptions { HasTextRegex = new Regex("^" + Regex.Escape(name) + @" \(") });
        await result.Locator("button").ClickAsync();
    }

    [E2EFact]
    public async Task A_Roll_Shows_Its_Total_And_Stays_In_The_History_After_Reloading__SC005()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "dados");

        await page.Locator("[data-testid=dice-formula]").FillAsync("2d6+3");
        await page.Locator("[data-testid=dice-formula]").PressAsync("Enter");
        await Assertions.Expect(page.Locator("[data-testid=dice-total]")).ToHaveTextAsync(Number());
        var total = int.Parse(await page.Locator("[data-testid=dice-total]").InnerTextAsync(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(total, 5, 15);

        await page.ReloadAsync();

        await Assertions.Expect(page.Locator("[data-testid=history-row]").First).ToContainTextAsync("2d6+3");
    }

    [E2EFact]
    public async Task Creating_Alchemist_Again_Gives_The_Sheet_Values_And_Is_Kept__SC001()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "conjuntos/nuevo");

        await AddComponentAsync(page, "botanica", "Botánica");
        await AddComponentAsync(page, "herbalismo", "Herbalismo");
        await AddComponentAsync(page, "holistica", "Identificación vegetal holística");
        await AddComponentAsync(page, "alquimia", "Alquimia");
        await AddComponentAsync(page, "destilar", "Destilar");

        var summary = page.Locator("[data-testid=suite-summary]");
        await Assertions.Expect(summary).ToContainTextAsync("16 PC");
        await Assertions.Expect(summary).ToContainTextAsync("1403 po");
        await Assertions.Expect(summary).ToContainTextAsync("15 %");
        await Assertions.Expect(summary).ToContainTextAsync("d8");

        await page.Locator("[data-testid=suite-name]").FillAsync("Prueba E2E");
        await page.Locator("[data-testid=suite-save]").ClickAsync();
        await page.WaitForURLAsync(new Regex("/conjuntos/prueba-e2e$"));
        await page.ReloadAsync();
        // Tras recargar, la aplicación vuelve a arrancar; en la CI puede tardar más de los 5 s de
        // Expect (fallo de la publicación del 2026-11-19), así que se espera a que pinte el título.
        await page.Locator("h1").WaitForAsync();

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Prueba E2E");
        await BrowserFixture.GoToAsync(page, "conjuntos");
        await Assertions.Expect(page.Locator("[data-testid=custom-suites] .suite-name", new PageLocatorOptions { HasText = "Prueba E2E" })).ToHaveCountAsync(1);
    }
}
