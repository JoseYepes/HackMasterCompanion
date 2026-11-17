using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>Tamaños de pantalla en los que se prueba la interfaz (FR-026).</summary>
public static class Viewports
{
    /// <summary>Teléfono: 390 × 844.</summary>
    public static readonly ViewportSize Phone = new() { Width = 390, Height = 844 };

    /// <summary>Tableta: 820 × 1180.</summary>
    public static readonly ViewportSize Tablet = new() { Width = 820, Height = 1180 };

    /// <summary>Los dos tamaños, en orden.</summary>
    public static IReadOnlyList<ViewportSize> All { get; } = [Phone, Tablet];
}

/// <summary>
/// Navegador Chromium compartido por las pruebas de interfaz de una clase. Se abre la primera vez
/// que una prueba pide una página, así que no hace nada si las pruebas se omiten.
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>Dirección base de la aplicación, terminada en "/".</summary>
    public static Uri BaseUri
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(E2EFactAttribute.UrlVariable)
                ?? throw new InvalidOperationException($"Falta la variable de entorno {E2EFactAttribute.UrlVariable}.");
            return new Uri(value.EndsWith('/') ? value : value + "/");
        }
    }

    /// <summary>Abre una página nueva del tamaño indicado.</summary>
    public async Task<IPage> NewPageAsync(ViewportSize viewport)
    {
        await _gate.WaitAsync();
        try
        {
            _playwright ??= await Playwright.CreateAsync();
            _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        finally
        {
            _gate.Release();
        }

        return await _browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = viewport, Locale = "es-ES" });
    }

    /// <summary>Abre una dirección relativa a la base y espera a que la aplicación pinte el título.</summary>
    public static async Task GoToAsync(IPage page, string relativeUri)
    {
        await page.GotoAsync(new Uri(BaseUri, relativeUri).ToString());
        await page.Locator("h1").First.WaitForAsync();
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
