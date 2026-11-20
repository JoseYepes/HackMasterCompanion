namespace HackMaster.Web.Tests.E2E;

/// <summary>Grimorio con los datos reales (F009): buscar, abrir un conjuro, reglas y sin desbordamiento.</summary>
[Trait("Category", "E2E")]
public sealed class GrimoireTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    [E2EFact]
    public async Task A_Spell_Is_Found_By_Name_And_Shows_Its_Data__FR002_FR004()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "grimorio");

        await page.Locator("[data-testid=spell-search]").FillAsync("PROYECTIL MAGICO");
        await page.Locator("[data-testid=spell-row] a", new() { HasText = "Proyectil mágico" }).ClickAsync();
        await page.Locator("[data-testid=spell-summary]").WaitForAsync();

        Assert.Equal("Proyectil mágico", (await page.Locator("h1").InnerTextAsync()).Trim());
        Assert.Contains("MJ 185", await page.Locator("[data-testid=spell-field-books]").InnerTextAsync(), StringComparison.Ordinal);
        Assert.Contains("Evocación", await page.Locator("[data-testid=spell-field-schools]").InnerTextAsync(), StringComparison.Ordinal);
    }

    [E2EFact]
    public async Task Filtering_By_Cleric_Level_1_Lists_Only_Those__FR003()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "grimorio?clase=clerigo&nivel=1");
        await page.Locator("[data-testid=spell-row]").First.WaitForAsync();

        var metas = await page.Locator("[data-testid=spell-row] .spell-meta").AllInnerTextsAsync();

        Assert.NotEmpty(metas);
        Assert.All(metas, meta => Assert.StartsWith("Clérigo 1", meta, StringComparison.Ordinal));
    }

    [E2EFact]
    public async Task The_Grimoire_Pages_Do_Not_Overflow_At_390_Or_820_In_Either_Mode__FR008()
    {
        foreach (var (viewport, mode) in Viewports.All.SelectMany(viewport => new[] { (viewport, "light"), (viewport, "dark") }))
        {
            var page = await browser.NewPageAsync(viewport);
            await page.AddInitScriptAsync("localStorage.setItem('hackmaster.theme', '{\"schemaVersion\":1,\"mode\":\"" + mode + "\"}')");
            foreach (var uri in new[] { "grimorio", "grimorio/mago/silbido-de-la-puerta-del-infierno-de-yudder", "grimorio/reglas" })
            {
                await BrowserFixture.GoToAsync(page, uri);
                var widths = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
                Assert.True(widths[0] <= widths[1], $"/{uri} a {viewport.Width} px en modo {mode}: el contenido mide {widths[0]} px y la pantalla {widths[1]} px.");
            }
        }
    }
}
