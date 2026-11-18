using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>Ninguna pantalla de F001 se desborda en horizontal a 390 px ni a 820 px (FR-026, SC-006).</summary>
[Trait("Category", "E2E")]
public sealed class NoHorizontalOverflowTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    /// <summary>Conjunto del jugador guardado a mano, con el nombre más largo de los datos entre sus componentes.</summary>
    private const string PlayerSuiteJson =
        "{\"schemaVersion\":1,\"suites\":[{\"slug\":\"prueba-de-ancho\",\"name\":\"Prueba de ancho\"," +
        "\"components\":[{\"kind\":\"Skill\",\"slug\":\"resistencia-mental-entren-mental-y-fisico-3-mental-resist-m-p-train-3\"},{\"kind\":\"Skill\",\"slug\":\"botanica\"}]," +
        "\"abilities\":[],\"createdAt\":\"2026-11-18T10:00:00+01:00\",\"updatedAt\":\"2026-11-18T10:00:00+01:00\"}]}";

    private static async Task AssertNoOverflowAsync(IPage page, string where)
    {
        var widths = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        Assert.True(widths[0] <= widths[1], $"{where}: el contenido mide {widths[0]} px y la pantalla {widths[1]} px.");
    }

    /// <summary>Dirección de la primera ficha enlazada desde una lista.</summary>
    private static async Task<string> FirstLinkAsync(IPage page, string listUri, string rowSelector)
    {
        await BrowserFixture.GoToAsync(page, listUri);
        var href = await page.Locator($"{rowSelector} a").First.GetAttributeAsync("href");
        Assert.False(string.IsNullOrEmpty(href), $"{listUri} no tiene ninguna ficha: faltan los datos.");
        return href!;
    }

    [E2EFact]
    public async Task No_Screen_Overflows_At_390_Or_820__FR026()
    {
        foreach (var viewport in Viewports.All)
        {
            var page = await browser.NewPageAsync(viewport);
            var size = $"{viewport.Width} px";

            var skill = await FirstLinkAsync(page, "habilidades", "[data-testid=skill-row]");
            var suite = await FirstLinkAsync(page, "conjuntos", "[data-testid=suite-row]");

            foreach (var uri in new[] { string.Empty, "habilidades", skill, "conjuntos", suite, "estilos", "dudas" })
            {
                await BrowserFixture.GoToAsync(page, uri);
                await AssertNoOverflowAsync(page, $"/{uri} a {size}");
            }

            await BrowserFixture.GoToAsync(page, "habilidades");
            await page.Locator("details.filters summary").ClickAsync();
            await AssertNoOverflowAsync(page, $"/habilidades con los filtros abiertos a {size}");

            // F002: tirador con una tirada que penetra, creador con componentes y ficha de un conjunto del jugador (FR-020, SC-006).
            await page.EvaluateAsync($"() => localStorage.setItem('hackmaster.suites', '{PlayerSuiteJson}')");
            await BrowserFixture.GoToAsync(page, "dados");
            await page.Locator("[data-testid=dice-penetrating]").CheckAsync();
            await page.Locator("[data-testid=dice-formula]").FillAsync("20d4+1d20-1d6+10000");
            await page.Locator("[data-testid=dice-formula]").PressAsync("Enter");
            await page.Locator("[data-testid=dice-total]").WaitForAsync();
            await AssertNoOverflowAsync(page, $"/dados con una tirada a {size}");

            await BrowserFixture.GoToAsync(page, "conjuntos/prueba-de-ancho/editar");
            await page.Locator("[data-testid=suite-component]").First.WaitForAsync();
            await AssertNoOverflowAsync(page, $"/conjuntos/…/editar a {size}");

            await BrowserFixture.GoToAsync(page, "conjuntos/prueba-de-ancho");
            await AssertNoOverflowAsync(page, $"ficha de un conjunto del jugador a {size}");

            await page.CloseAsync();
        }
    }
}
