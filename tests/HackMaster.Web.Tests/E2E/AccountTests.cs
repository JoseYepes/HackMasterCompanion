namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Pantallas de la cuenta en el navegador sin inicio de sesión configurado (F006 FR-012, FR-016):
/// la cabecera sigue como antes y las páginas nuevas no se desbordan a 390 ni a 820 px.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AccountTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    [E2EFact]
    public async Task Without_Configuration_The_Header_Has_No_Session__F006_FR012()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);

        await BrowserFixture.GoToAsync(page, string.Empty);

        Assert.Equal(0, await page.Locator("[data-testid=account-menu]").CountAsync());
    }

    [E2EFact]
    public async Task Account_Pages_Do_Not_Overflow__F006_FR016()
    {
        foreach (var (viewport, mode) in Viewports.All.SelectMany(viewport => new[] { (viewport, "light"), (viewport, "dark") }))
        {
            var page = await browser.NewPageAsync(viewport);
            await page.AddInitScriptAsync("localStorage.setItem('hackmaster.theme', '{\"schemaVersion\":1,\"mode\":\"" + mode + "\"}')");

            foreach (var uri in new[] { "cuenta", "invitaciones", "authentication/login" })
            {
                await BrowserFixture.GoToAsync(page, uri);
                await page.Locator("h1").First.WaitForAsync();
                var widths = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
                Assert.True(widths[0] <= widths[1], $"/{uri} a {viewport.Width} px en modo {mode}: el contenido mide {widths[0]} px y la pantalla {widths[1]} px.");
            }
        }
    }
}
