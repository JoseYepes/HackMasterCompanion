using System.Text.Json;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Estilo de fantasía heroica (F007): letras propias, modo normal y noche, botón de modo y mesa de
/// dados en un navegador real (specs/005-estilo-fantasia/contracts/theme.md).
/// </summary>
[Trait("Category", "E2E")]
public sealed class ThemeTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    private const string Dark = """{"schemaVersion":1,"mode":"dark"}""";

    /// <summary>Guarda el modo antes de que cargue cada página, como si el jugador ya lo hubiera elegido.</summary>
    private static Task SaveBeforeLoadAsync(IPage page, string value) =>
        page.AddInitScriptAsync($"localStorage.setItem('hackmaster.theme', {JsonSerializer.Serialize(value)})");

    private static Task<string?> ThemeAsync(IPage page) =>
        page.EvaluateAsync<string?>("() => document.documentElement.dataset.theme");

    /// <summary>Las 12 pantallas de SC-001, con fichas tomadas de los datos reales.</summary>
    private static async Task<List<string>> AllScreensAsync(IPage page)
    {
        async Task<string> FirstAsync(string list, string selector)
        {
            await BrowserFixture.GoToAsync(page, list);
            var href = await page.Locator(selector).First.GetAttributeAsync("href");
            Assert.False(string.IsNullOrEmpty(href), $"/{list} no tiene ningún enlace {selector}.");
            return href!;
        }

        return
        [
            string.Empty,
            "habilidades",
            await FirstAsync("habilidades", "[data-testid=skill-row] a"),
            "conjuntos",
            await FirstAsync("conjuntos", "[data-testid=suite-row] a"),
            await FirstAsync("conjuntos", "a[href*='conjuntos/oficiales/']"),
            "conjuntos/nuevo",
            "estilos",
            await FirstAsync("estilos", "[data-testid=style-row] a"),
            "dudas",
            "dados",
            "esta-pagina-no-existe",
        ];
    }

    [E2EFact]
    public async Task The_12_Screens_Load_Nothing_From_Other_Sites__FR008_SC004()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        var origin = BrowserFixture.BaseUri.GetLeftPart(UriPartial.Authority);
        var outside = new List<string>();
        page.Request += (_, request) =>
        {
            if (!request.Url.StartsWith(origin, StringComparison.Ordinal) && !request.Url.StartsWith("data:", StringComparison.Ordinal))
            {
                outside.Add(request.Url);
            }
        };

        foreach (var uri in await AllScreensAsync(page))
        {
            await BrowserFixture.GoToAsync(page, uri);
        }

        Assert.Empty(outside);
    }

    [E2EFact]
    public async Task Titles_Use_Cinzel_And_Text_Uses_EB_Garamond__FR001_FR007()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades");
        await page.Locator("[data-testid=skill-row]").First.WaitForAsync();

        var fonts = await page.EvaluateAsync<string[]>("""
            async () => {
              await document.fonts.ready;
              const family = selector => getComputedStyle(document.querySelector(selector)).fontFamily;
              return [
                family('h1'),
                family('[data-testid=skill-row]'),
                family('[data-testid=search-input]'),
                String(document.fonts.check('600 1em Cinzel')),
                String(document.fonts.check('400 1em "EB Garamond"')),
              ];
            }
            """);

        Assert.StartsWith("Cinzel", fonts[0], StringComparison.Ordinal);
        Assert.StartsWith("\"EB Garamond\"", fonts[1], StringComparison.Ordinal);
        Assert.StartsWith("\"EB Garamond\"", fonts[2], StringComparison.Ordinal);
        Assert.Equal(["true", "true"], fonts[3..]);
    }

    [E2EFact]
    public async Task Buttons_Measure_At_Least_44_Pixels_At_390__FR010()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        foreach (var uri in new[] { "habilidades", "conjuntos/nuevo", "dados" })
        {
            await BrowserFixture.GoToAsync(page, uri);
            await page.Locator("[data-testid=theme-toggle]").WaitForAsync();

            var small = await page.EvaluateAsync<string[]>("""
                () => [...document.querySelectorAll('button, .button-link')]
                  .filter(button => button.offsetParent !== null)
                  .map(button => [button, button.getBoundingClientRect()])
                  .filter(([, box]) => box.width < 44 || box.height < 44)
                  .map(([button, box]) => `${button.className || button.textContent.trim()} (${Math.round(box.width)}×${Math.round(box.height)})`)
                """);

            Assert.True(small.Length == 0, $"/{uri}: botones de menos de 44 px: {string.Join(", ", small)}");
        }
    }

    [E2EFact]
    public async Task A_Saved_Dark_Mode_Is_Set_Before_The_Page_Paints__FR005()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await SaveBeforeLoadAsync(page, Dark);
        await page.AddInitScriptAsync("""
            document.addEventListener('DOMContentLoaded', () => { window.themeAtLoad = document.documentElement.dataset.theme; });
            """);

        await BrowserFixture.GoToAsync(page, "habilidades");

        Assert.Equal("dark", await page.EvaluateAsync<string?>("() => window.themeAtLoad"));
    }

    [E2EFact]
    public async Task The_Chosen_Mode_Is_Remembered_After_Reloading__FR004()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades");
        Assert.Equal("light", await ThemeAsync(page));

        await page.Locator("[data-testid=theme-toggle]").ClickAsync();
        await page.ReloadAsync();
        await page.Locator("h1").First.WaitForAsync();

        Assert.Equal("dark", await ThemeAsync(page));
        Assert.Equal("Cambiar a modo normal", await page.Locator("[data-testid=theme-toggle]").GetAttributeAsync("aria-label"));
    }

    [E2EFact]
    public async Task Without_A_Choice_The_Device_Mode_Is_Followed__FR004()
    {
        foreach (var (scheme, expected) in new[] { (ColorScheme.Dark, "dark"), (ColorScheme.Light, "light") })
        {
            var page = await browser.NewPageAsync(Viewports.Phone);
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = scheme });

            await BrowserFixture.GoToAsync(page, "habilidades");

            Assert.Equal(expected, await ThemeAsync(page));
            await page.CloseAsync();
        }
    }

    [E2EFact]
    public async Task Broken_Or_Unknown_Saved_Values_Count_As_No_Choice__Contract()
    {
        foreach (var saved in new[] { "{roto", """{"schemaVersion":2,"mode":"dark"}""", """{"schemaVersion":1,"mode":"sepia"}""" })
        {
            var page = await browser.NewPageAsync(Viewports.Phone);
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
            await SaveBeforeLoadAsync(page, saved);

            await BrowserFixture.GoToAsync(page, "habilidades");

            Assert.Equal("light", await ThemeAsync(page));
            await page.CloseAsync();
        }
    }

    [E2EFact]
    public async Task The_Button_Changes_The_Mode_Even_If_Storage_Fails__Contract()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await page.AddInitScriptAsync("""
            Storage.prototype.setItem = () => { throw new Error('QuotaExceededError'); };
            """);
        await BrowserFixture.GoToAsync(page, "habilidades");

        await page.Locator("[data-testid=theme-toggle]").ClickAsync();

        Assert.Equal("dark", await ThemeAsync(page));
    }

    [E2EFact]
    public async Task Switching_Takes_Under_Half_A_Second_And_Keeps_What_Was_Typed__SC003_FR005()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades");
        await page.Locator("[data-testid=search-input]").FillAsync("golpe");

        var ms = await page.EvaluateAsync<double>("""
            async () => {
              const before = getComputedStyle(document.documentElement).backgroundColor;
              const started = performance.now();
              document.querySelector('[data-testid=theme-toggle]').click();
              while (getComputedStyle(document.documentElement).backgroundColor === before && performance.now() - started < 2000) {
                await new Promise(resolve => requestAnimationFrame(resolve));
              }
              return performance.now() - started;
            }
            """);

        Assert.InRange(ms, 0, 500);
        Assert.Equal("golpe", await page.Locator("[data-testid=search-input]").InputValueAsync());
        Assert.Equal("dark", await ThemeAsync(page));
    }

    [E2EFact]
    public async Task The_Dice_Table_Changes_With_The_Mode_Without_Changing_The_Roll__FR012_US3()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "dados");
        await page.EvaluateAsync("""() => localStorage.setItem('hackmaster.rolls', '{"schemaVersion":2,"penetrating":false,"dice3d":true,"rolls":[]}')""");
        await BrowserFixture.GoToAsync(page, "dados?f=" + Uri.EscapeDataString("2d6+1d20"));
        await page.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();

        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const board = document.querySelector('[data-testid=dice-board]');
              const table = () => getComputedStyle(board).backgroundImage;
              const light = table();
              document.querySelector('[data-testid=dice-roll]').click();
              await new Promise(resolve => setTimeout(resolve, 300));
              document.querySelector('[data-testid=theme-toggle]').click();
              const dark = table();
              while (!document.querySelector('[data-testid=dice-total]')) {
                await new Promise(resolve => setTimeout(resolve, 20));
              }
              const dice3d = await import('./js/dice3d.js');
              return {
                changed: light !== dark,
                shown: dice3d.shownFaces(board),
                detail: [...document.querySelectorAll('[data-testid=dice-detail] .die')].map(die => die.textContent.trim()),
              };
            }
            """);

        Assert.True(result.GetProperty("changed").GetBoolean(), "La mesa de dados no cambia con el modo.");
        Assert.Equal(result.GetProperty("detail").GetRawText(), result.GetProperty("shown").GetRawText());
    }
}
