using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Fichas de PJ en un navegador real: se guardan en IndexedDB, exportar e importar deja la ficha
/// idéntica, nada sale del sitio y nada se desborda (F004).
/// </summary>
[Trait("Category", "E2E")]
public sealed class CharacterTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    /// <summary>Crea una ficha con varios campos y una habilidad desde el editor y la guarda.</summary>
    private static async Task CreateAsync(IPage page, string name)
    {
        await BrowserFixture.GoToAsync(page, "fichas/nueva");
        await page.Locator("[data-testid=character-name]").FillAsync(name);
        await page.Locator("[data-testid=character-race]").FillAsync("Enano");
        await page.Locator("[data-testid=character-class]").FillAsync("Guerrero");
        await page.Locator("[data-testid=character-level]").FillAsync("3");
        await page.Locator("[data-testid=character-ability-FUE]").FillAsync("15");
        await page.Locator("[data-testid=character-fraction-FUE]").FillAsync("47");
        await page.Locator("[data-testid=character-hp]").FillAsync("31");
        await page.Locator("[data-testid=character-honor]").FillAsync("-2");
        await page.Locator("[data-testid=character-skill-search]").FillAsync("botanica");
        await page.Locator("[data-testid=character-skill-result] button").First.ClickAsync();
        await page.Locator("[data-testid=character-skill-mastery]").FillAsync("25");
        await page.Locator("[data-testid=character-notes]").FillAsync("Línea uno\nLínea dos");
        await page.Locator("[data-testid=character-save]").ClickAsync();
        await page.Locator("[data-testid=character-row]").First.WaitForAsync();
    }

    private static async Task<string> ExportAsync(IPage page)
    {
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-testid=character-export]").ClickAsync());
        var path = await download.PathAsync();
        Assert.Equal("grimbold.hackmaster.json", download.SuggestedFilename);
        return await File.ReadAllTextAsync(path);
    }

    [E2EFact]
    public async Task A_Saved_Character_Is_Still_There_After_Reloading__FR004()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await CreateAsync(page, "Grimbold");

        await page.ReloadAsync();
        await page.Locator("[data-testid=character-row]").First.WaitForAsync();
        await page.Locator("[data-testid=character-row] a").First.ClickAsync();
        await page.Locator("[data-testid=character-name]").WaitForAsync();

        Assert.Equal("Grimbold", await page.Locator("[data-testid=character-name]").InputValueAsync());
        Assert.Equal("47", await page.Locator("[data-testid=character-fraction-FUE]").InputValueAsync());
        Assert.Equal("-2", await page.Locator("[data-testid=character-honor]").InputValueAsync());
        Assert.Equal("25", await page.Locator("[data-testid=character-skill-mastery]").InputValueAsync());
        Assert.Equal("Línea uno\nLínea dos", await page.Locator("[data-testid=character-notes]").InputValueAsync());
    }

    [E2EFact]
    public async Task Export_Delete_And_Import_Gives_The_Same_Character_Without_Leaving_The_Site__SC001_SC003()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        var outside = new List<string>();
        page.Request += (_, request) =>
        {
            if (!request.Url.StartsWith(BrowserFixture.BaseUri.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) && !request.Url.StartsWith("blob:", StringComparison.Ordinal))
            {
                outside.Add(request.Url);
            }
        };
        await CreateAsync(page, "Grimbold");
        await page.Locator("[data-testid=character-row] a").First.ClickAsync();
        await page.Locator("[data-testid=character-export]").WaitForAsync();

        var exported = await ExportAsync(page);
        var file = Path.Combine(Path.GetTempPath(), $"grimbold-{Guid.NewGuid():N}.hackmaster.json");
        await File.WriteAllTextAsync(file, exported);
        await page.Locator("[data-testid=character-delete]").ClickAsync();
        await page.Locator("[data-testid=character-delete-confirm]").ClickAsync();
        await page.Locator("[data-testid=characters-empty]").WaitForAsync();

        await page.Locator("[data-testid=character-import]").SetInputFilesAsync(file);
        await page.Locator("[data-testid=character-import-done]").WaitForAsync();
        await page.Locator("[data-testid=character-row] a").First.ClickAsync();
        await page.Locator("[data-testid=character-export]").WaitForAsync();
        var reimported = await ExportAsync(page);
        File.Delete(file);

        Assert.Equal(exported, reimported);
        Assert.Empty(outside);
    }

    [E2EFact]
    public async Task Duplicating_Opens_An_Independent_Copy__FR005()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await CreateAsync(page, "Grimbold");
        await page.Locator("[data-testid=character-row] a").First.ClickAsync();
        await page.Locator("[data-testid=character-duplicate]").ClickAsync();

        await page.WaitForFunctionAsync("() => document.querySelector('[data-testid=character-name]')?.value === 'Grimbold (copia)'");
        await page.Locator("[data-testid=character-notes]").FillAsync("Solo en la copia");
        await page.Locator("[data-testid=character-save]").ClickAsync();
        await page.Locator("[data-testid=character-row]").Nth(1).WaitForAsync();

        Assert.Equal(["Grimbold", "Grimbold (copia)"], await page.Locator("[data-testid=character-row] .character-name").AllInnerTextsAsync());
        await page.Locator("[data-testid=character-row] a").First.ClickAsync();
        await page.Locator("[data-testid=character-notes]").WaitForAsync();
        Assert.Equal("Línea uno\nLínea dos", await page.Locator("[data-testid=character-notes]").InputValueAsync());
    }

    [E2EFact]
    public async Task The_Character_Pages_Do_Not_Overflow_At_390_Or_820_In_Either_Mode__SC005()
    {
        foreach (var (viewport, mode) in Viewports.All.SelectMany(viewport => new[] { (viewport, "light"), (viewport, "dark") }))
        {
            var page = await browser.NewPageAsync(viewport);
            await page.AddInitScriptAsync("localStorage.setItem('hackmaster.theme', '{\"schemaVersion\":1,\"mode\":\"" + mode + "\"}')");
            await CreateAsync(page, "Grimbold Barbaespesa de las Montañas Grises, Señor de los Túneles Olvidados");
            await page.Locator("[data-testid=character-row] a").First.ClickAsync();
            await page.Locator("[data-testid=character-skill-search]").FillAsync("resistencia mental");
            await page.Locator("[data-testid=character-skill-result] button").First.ClickAsync();

            foreach (var where in new[] { "editor", "lista" })
            {
                if (where == "lista")
                {
                    await BrowserFixture.GoToAsync(page, "fichas");
                    await page.Locator("[data-testid=character-row]").First.WaitForAsync();
                }

                var theme = await page.EvaluateAsync<string?>("() => document.documentElement.dataset.theme");
                var widths = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
                Assert.Equal(mode, theme);
                Assert.True(widths[0] <= widths[1], $"{where} a {viewport.Width} px en modo {mode}: el contenido mide {widths[0]} px y la pantalla {widths[1]} px.");
            }
        }
    }
}
