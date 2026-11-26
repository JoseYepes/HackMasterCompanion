using System.Text.Json;
using HackMaster.Web.Tests.E2E;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.Published;

/// <summary>Chromium compartido por las pruebas del sitio publicado; se abre al pedirlo.</summary>
public sealed class PublishedBrowserFixture : IAsyncLifetime
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>El navegador, abierto la primera vez.</summary>
    public async Task<IBrowser> BrowserAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _playwright ??= await Playwright.CreateAsync();
            // WebGL por software para los dados 3D, como en BrowserFixture.
            return _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, Args = ["--enable-unsafe-swiftshader"] });
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _gate.Dispose();
    }
}

/// <summary>
/// La aplicación publicada con su ruta base, instalable y sin conexión (F013,
/// specs/011-instalable-sin-conexion): pantallas sin conexión (SC-001), qué se guarda (SC-004),
/// instalabilidad (SC-003) y versiones nuevas (SC-005), a 390 y 820 px.
/// </summary>
[Trait("Category", "Published")]
public sealed class OfflineTests(PublishedBrowserFixture fixture) : IClassFixture<PublishedBrowserFixture>
{
    private const string IphoneUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1";

    /// <summary>Primera visita con conexión: espera a que la copia esté guardada y la página quede bajo el trabajador.</summary>
    private static async Task FirstVisitAsync(IPage page, PublishedSite site)
    {
        await page.GotoAsync(site.AppUri.ToString());
        await page.Locator("h1").First.WaitForAsync();
        // Con tope: si no hay trabajador de servicio, la prueba falla en lugar de esperar para siempre.
        // (WaitForFunction no sirve: una promesa cuenta como «cierto» al instante.)
        var ready = await page.EvaluateAsync<bool>(
            "() => Promise.race([navigator.serviceWorker.ready.then(() => true), new Promise(resolve => setTimeout(() => resolve(false), 60000))])");
        Assert.True(ready, "El trabajador de servicio no llegó a activarse en 60 s.");
        await page.ReloadAsync();
        await page.Locator("h1").First.WaitForAsync();
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null", null, new PageWaitForFunctionOptions { Timeout = 30_000 });
    }

    private static async Task GoAsync(IPage page, PublishedSite site, string relative)
    {
        var response = await page.GotoAsync(new Uri(site.AppUri, relative).ToString());
        Assert.NotNull(response);
        await page.Locator("h1").First.WaitForAsync();
        var title = await page.Locator("h1").First.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(title), $"/{relative} se abrió sin título.");
    }

    private static async Task AssertNoOverflowAsync(IPage page, string where)
    {
        var widths = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        Assert.True(widths[0] <= widths[1], $"{where}: el contenido mide {widths[0]} px y la pantalla {widths[1]} px.");
    }

    private static Task<string[]> CachedUrlsAsync(IPage page) => page.EvaluateAsync<string[]>(
        "async () => { const urls = []; for (const key of await caches.keys()) { const cache = await caches.open(key); for (const request of await cache.keys()) urls.push(request.url); } return urls; }");

    [PublishedFact]
    public async Task Every_Screen_Opens_Offline_After_The_First_Visit__SC001_FR004()
    {
        var browser = await fixture.BrowserAsync();
        foreach (var viewport in Viewports.All)
        {
            using var site = new PublishedSite();
            var page = await PublishedSite.NewPageAsync(browser, viewport);
            await FirstVisitAsync(page, site);
            await page.Context.SetOfflineAsync(true);

            await GoAsync(page, site, "habilidades");
            var skill = await page.Locator("[data-testid=skill-row] a").First.GetAttributeAsync("href");
            await GoAsync(page, site, "grimorio");
            var spell = await page.Locator("[data-testid=spell-row] a").First.GetAttributeAsync("href");
            Assert.False(string.IsNullOrEmpty(skill));
            Assert.False(string.IsNullOrEmpty(spell));

            // Cada dirección se abre escribiéndola (recarga completa), no navegando dentro de la aplicación.
            foreach (var uri in new[] { string.Empty, "habilidades", "habilidades?q=" + Uri.EscapeDataString("espada"), skill!, "conjuntos", "conjuntos/nuevo", "estilos", "grimorio", spell!, "grimorio/reglas", "dudas", "dados", "fichas", "fichas/nueva" })
            {
                await GoAsync(page, site, uri);
                await AssertNoOverflowAsync(page, $"/{uri} sin conexión a {viewport.Width} px");
            }

            // El tirador funciona sin conexión.
            await GoAsync(page, site, "dados");
            await page.Locator("[data-testid=dice-formula]").FillAsync("2d6+3");
            await page.Locator("[data-testid=dice-formula]").PressAsync("Enter");
            await page.Locator("[data-testid=dice-total]").WaitForAsync();

            // Los dados 3D cargan sin conexión (aclaración: se guarda todo).
            var supported = await page.EvaluateAsync<JsonElement>("async () => (await import('./js/dice3d.js')).isSupported()");
            Assert.NotEqual(JsonValueKind.Undefined, supported.ValueKind);

            await page.Context.CloseAsync();
        }
    }

    [PublishedFact]
    public async Task Copy_Keeps_Every_Own_File_And_Nothing_Else__FR003_FR005_SC004()
    {
        var browser = await fixture.BrowserAsync();
        using var site = new PublishedSite();
        var page = await PublishedSite.NewPageAsync(browser, Viewports.Phone);
        await FirstVisitAsync(page, site);

        var urls = await CachedUrlsAsync(page);
        var app = site.AppUri.ToString();
        Assert.All(urls, url => Assert.StartsWith(app, url, StringComparison.Ordinal));
        Assert.DoesNotContain(urls, url => url.Contains("/authentication/", StringComparison.Ordinal));
        Assert.DoesNotContain(urls, url => url.EndsWith(".br", StringComparison.Ordinal) || url.EndsWith(".gz", StringComparison.Ordinal));
        foreach (var file in new[] { "index.html", "manifest.webmanifest", "icon-512.png", "js/dice3d.js", "lib/three.module.js", "lib/cannon-es.js", "fonts/cinzel-latin-600-normal.woff2" })
        {
            Assert.Contains(app + file, urls);
        }

        Assert.Contains(urls, url => url.StartsWith(app + "sounds/", StringComparison.Ordinal) && url.EndsWith(".ogg", StringComparison.Ordinal));
        Assert.Contains(urls, url => url.StartsWith(app + "_framework/", StringComparison.Ordinal) && url.EndsWith(".wasm", StringComparison.Ordinal));

        // Una dirección interna cualquiera no llega a la red (sale de la copia)…
        while (site.Requests.TryDequeue(out _))
        {
        }

        await GoAsync(page, site, "estilos");
        Assert.DoesNotContain(PublishedSite.BasePath + "estilos", site.Requests);

        // La vuelta del inicio de sesión carga la aplicación guardada, de la misma versión que la página,
        // en lugar de traer de la red un index.html quizá más nuevo (FR-005, revisión independiente).
        await GoAsync(page, site, "authentication/login-callback");
        Assert.DoesNotContain(site.Requests, path => path.Contains("/authentication/", StringComparison.Ordinal));
        Assert.DoesNotContain(await CachedUrlsAsync(page), url => url.Contains("/authentication/", StringComparison.Ordinal));
        await page.Context.CloseAsync();
    }

    [PublishedFact]
    public async Task Browser_Finds_No_Installability_Errors__SC003_FR001()
    {
        var browser = await fixture.BrowserAsync();
        using var site = new PublishedSite();
        var page = await PublishedSite.NewPageAsync(browser, Viewports.Tablet);
        await FirstVisitAsync(page, site);

        var session = await page.Context.NewCDPSessionAsync(page);
        var result = await session.SendAsync("Page.getInstallabilityErrors");
        Assert.NotNull(result);
        var errors = result.Value.GetProperty("installabilityErrors").EnumerateArray().Select(e => e.GetProperty("errorId").GetString()).ToList();
        Assert.True(errors.Count == 0, "Errores de instalabilidad: " + string.Join(", ", errors));

        var manifest = await session.SendAsync("Page.getAppManifest");
        Assert.Equal(site.AppUri + "manifest.webmanifest", manifest!.Value.GetProperty("url").GetString());
        await page.Context.CloseAsync();
    }

    [PublishedFact]
    public async Task Iphone_Note_Fits_The_Screen__FR010_FR012()
    {
        var browser = await fixture.BrowserAsync();
        foreach (var viewport in Viewports.All)
        {
            using var site = new PublishedSite();
            var page = await PublishedSite.NewPageAsync(browser, viewport, IphoneUserAgent);
            await GoAsync(page, site, string.Empty);

            await page.Locator("[data-testid=install-ios]").WaitForAsync();
            await AssertNoOverflowAsync(page, $"nota de iPhone a {viewport.Width} px");
            await page.Context.CloseAsync();
        }
    }

    [PublishedFact]
    public async Task New_Version_Shows_The_Notice_And_Keeps_Saved_Data__SC005_FR007_FR008_FR009()
    {
        var browser = await fixture.BrowserAsync();
        using var site = new PublishedSite();
        var page = await PublishedSite.NewPageAsync(browser, Viewports.Phone);
        await FirstVisitAsync(page, site);
        var oldVersion = site.AssetsVersion;

        // Datos del jugador: modo noche elegido con el botón y un conjunto guardado.
        await page.Locator("[data-testid=theme-toggle]").ClickAsync();
        await page.EvaluateAsync("() => localStorage.setItem('hackmaster.f013-prueba', 'sigue aquí')");
        var before = await page.EvaluateAsync<string>("() => JSON.stringify(Object.entries(localStorage).sort())");

        var newVersion = site.PublishNewVersion();
        await page.ReloadAsync();
        await page.Locator("[data-testid=update-ready]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
        await AssertNoOverflowAsync(page, "aviso de versión nueva a 390 px");

        // «Actualizar» recarga la página con la versión nueva.
        var reloaded = new TaskCompletionSource();
        page.Load += (_, _) => reloaded.TrySetResult();
        await page.Locator("[data-testid=update-apply]").ClickAsync();
        await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await page.Locator("h1").First.WaitForAsync();
        // Queda solo la copia nueva: la vieja se borra al activarse (FR-007).
        string[] keys = [];
        for (var i = 0; i < 120; i++)
        {
            keys = await page.EvaluateAsync<string[]>("() => caches.keys()");
            if (keys.SequenceEqual(["hackmaster-offline-" + newVersion]))
            {
                break;
            }

            await Task.Delay(250);
        }

        Assert.Equal(["hackmaster-offline-" + newVersion], keys);

        Assert.NotEqual(oldVersion, newVersion);
        Assert.Empty(await page.Locator("[data-testid=update-ready]").AllAsync());
        Assert.Equal(before, await page.EvaluateAsync<string>("() => JSON.stringify(Object.entries(localStorage).sort())"));
        Assert.Equal("dark", await page.EvaluateAsync<string>("() => document.documentElement.dataset.theme"));
        await page.Context.CloseAsync();
    }
}
