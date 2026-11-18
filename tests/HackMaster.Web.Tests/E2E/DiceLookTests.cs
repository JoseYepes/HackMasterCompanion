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

    /// <summary>Tira en la mesa un d6 que saca 6 y penetra (extra 2) con el aspecto indicado; devuelve efectos y dados.</summary>
    private const string PenetratingRoll = """
        async look => {
          const dice3d = await import('./js/dice3d.js');
          const board = document.querySelector('[data-testid=dice-board]');
          const started = performance.now();
          await dice3d.roll(board, { dice: [
            { shape: 'D6', face: '6', wave: 0, penetration: false, parent: null },
            { shape: 'D6', face: '6', wave: 1, penetration: true, parent: 0 },
            { shape: 'D6', face: '2', wave: 2, penetration: true, parent: 1 },
          ] }, look);
          return JSON.parse(JSON.stringify({
            ms: performance.now() - started,
            effects: dice3d.shownEffects(board),
            dice: dice3d.shownDice(board),
          }));
        }
        """;

    private static object Look(string effect, string quality = "High") =>
        new { material = "Ivory", color = "#f4efe6", ink = "#1f2328", effect, sound = false, quality };

    [E2EFact]
    public async Task Each_Penetration_Shows_The_Chosen_Effect_On_Its_Die_And_Extra_Dice_Glow__FR007_FR017()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        var result = await page.EvaluateAsync<JsonElement>(PenetratingRoll, Look("Fire"));

        Assert.Equal(
            [("Fire", 0), ("Fire", 1)],
            result.GetProperty("effects").EnumerateArray().Select(effect => (effect.GetProperty("effect").GetString(), effect.GetProperty("parent").GetInt32())));
        Assert.Equal([false, true, true], result.GetProperty("dice").EnumerateArray().Select(die => die.GetProperty("aura").GetBoolean()));
        Assert.InRange(result.GetProperty("ms").GetDouble(), 0, 4000);
    }

    [E2EFact]
    public async Task With_No_Effect_The_Extra_Dice_Still_Glow__FR007_FR017()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        var result = await page.EvaluateAsync<JsonElement>(PenetratingRoll, Look("None"));

        Assert.Equal(0, result.GetProperty("effects").GetArrayLength());
        Assert.Equal([false, true, true], result.GetProperty("dice").EnumerateArray().Select(die => die.GetProperty("aura").GetBoolean()));
    }

    [E2EFact]
    public async Task With_Low_Quality_Or_Reduced_Motion_The_Effect_Is_A_Short_Flash__FR018()
    {
        var low = await OpenAsync(browser, Viewports.Phone, History("Ivory"));
        var lowResult = await low.EvaluateAsync<JsonElement>(PenetratingRoll, Look("Blood", "Low"));
        Assert.All(lowResult.GetProperty("effects").EnumerateArray(), effect => Assert.Equal("Flash", effect.GetProperty("effect").GetString()));

        var calm = await browser.NewPageAsync(Viewports.Phone);
        await calm.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await BrowserFixture.GoToAsync(calm, "dados");
        await calm.EvaluateAsync("value => localStorage.setItem('hackmaster.rolls', value)", History("Ivory"));
        await BrowserFixture.GoToAsync(calm, "dados");
        await calm.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();
        var calmResult = await calm.EvaluateAsync<JsonElement>(PenetratingRoll, Look("Explosion"));
        Assert.Equal(2, calmResult.GetProperty("effects").GetArrayLength());
        Assert.All(calmResult.GetProperty("effects").EnumerateArray(), effect => Assert.Equal("Flash", effect.GetProperty("effect").GetString()));
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
