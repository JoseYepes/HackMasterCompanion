using System.Text.Json;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Dados personalizables en un navegador real (F008): aspecto, efectos al penetrar, sonido y calidad
/// (specs/006-dados-personalizables/contracts/dice3d-look.md).
/// </summary>
[Trait("Category", "E2E")]
public sealed class DiceLookTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    /// <summary>Historial versión 3 con los dados 3D activados y el aspecto indicado.</summary>
    private static string History(string material, string effect = "Explosion", bool sound = true, string quality = "High", bool penetrating = false) =>
        JsonSerializer.Serialize(new
        {
            schemaVersion = 3,
            penetrating,
            dice3d = true,
            look = new { material, color = "#2f5a46", ink = "#f1e5c4", effect, sound, quality },
            rolls = Array.Empty<object>(),
        });

    private static async Task<IPage> OpenAsync(BrowserFixture browser, ViewportSize viewport, string history, string formula = "2d6+1d20")
    {
        var page = await browser.NewPageAsync(viewport);
        await BrowserFixture.GoToAsync(page, "dados");
        await page.EvaluateAsync("value => localStorage.setItem('hackmaster.rolls', value)", history);
        await BrowserFixture.GoToAsync(page, "dados?f=" + Uri.EscapeDataString(formula));
        await page.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();
        return page;
    }

    /// <summary>Tira con el botón y espera al total; devuelve los dados en pantalla.</summary>
    private static async Task<JsonElement> RollAsync(IPage page) =>
        await page.EvaluateAsync<JsonElement>("""
            async () => {
              document.querySelector('[data-testid=dice-roll]').click();
              const started = performance.now();
              while (!document.querySelector('[data-testid=dice-total]') && performance.now() - started < 10000) {
                await new Promise(resolve => setTimeout(resolve, 20));
              }
              const dice3d = await import('./js/dice3d.js');
              const board = document.querySelector('[data-testid=dice-board]');
              return JSON.parse(JSON.stringify(dice3d.shownDice(board)));
            }
            """);

    [E2EFact]
    public async Task The_Dice_Are_Drawn_With_The_Saved_Material_Also_After_Reloading__FR001_FR012()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Marble"));

        var dice = await RollAsync(page);
        Assert.Equal(3, dice.GetArrayLength());
        Assert.All(dice.EnumerateArray(), die => Assert.Equal("Marble", die.GetProperty("material").GetString()));

        await page.ReloadAsync();
        await page.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();
        var again = await RollAsync(page);
        Assert.All(again.EnumerateArray(), die => Assert.Equal("Marble", die.GetProperty("material").GetString()));
    }

    [E2EFact]
    public async Task Choosing_A_Material_In_The_Panel_Changes_The_Next_Roll__FR001()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        await page.Locator("[data-testid=look-open]").ClickAsync();
        await page.Locator("[data-testid=look-material-Crystal]").ClickAsync();
        var dice = await RollAsync(page);

        Assert.All(dice.EnumerateArray(), die => Assert.Equal("Crystal", die.GetProperty("material").GetString()));
    }

    [E2EFact]
    public async Task The_Open_Panel_Does_Not_Overflow_And_Its_Buttons_Measure_44_Pixels__FR016()
    {
        foreach (var viewport in Viewports.All)
        {
            var page = await OpenAsync(browser, viewport, History("Ivory"));
            await page.Locator("[data-testid=look-open]").ClickAsync();
            await page.Locator("[data-testid=look-preview] canvas").WaitForAsync();

            var result = await page.EvaluateAsync<JsonElement>("""
                () => ({
                  overflow: document.documentElement.scrollWidth > document.documentElement.clientWidth,
                  small: [...document.querySelectorAll('.dice-look button')]
                    .filter(button => button.offsetParent !== null)
                    .map(button => button.getBoundingClientRect())
                    .filter(box => box.width < 44 || box.height < 44).length,
                })
                """);

            Assert.False(result.GetProperty("overflow").GetBoolean(), $"Desborda a {viewport.Width} px con el panel abierto.");
            Assert.Equal(0, result.GetProperty("small").GetInt32());
            await page.CloseAsync();
        }
    }
}
