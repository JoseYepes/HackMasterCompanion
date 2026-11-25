using HackMaster.Web;
using HackMaster.Web.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Los datos de juego se cargan una sola vez al arrancar.
builder.Services.AddSingleton(GameData.LoadEmbedded());
builder.Services.AddSingleton(GrimoireData.LoadEmbedded());

// Cuenta con la sesión iniciada: los almacenes guardan cada cuenta en sus propias claves (F006).
builder.Services.AddSingleton<AccountSession>();

// Azar para el tirador y guardado en el navegador (conjuntos del jugador e historial).
builder.Services.AddSingleton<HackMaster.Domain.IRandom, SystemRandom>();
builder.Services.AddSingleton<BrowserStore>();
builder.Services.AddSingleton<PlayerSuites>();

// Fichas de PJ, guardadas en IndexedDB (F004).
builder.Services.AddSingleton<IndexedStore>();
builder.Services.AddSingleton<PlayerCharacters>();

// Cuentas y sincronización (F006). Los valores de wwwroot/appsettings.json son públicos; sin
// ellos la aplicación funciona como antes, solo con los datos del dispositivo (FR-012).
var authority = builder.Configuration["Auth:Authority"];
var clientId = builder.Configuration["Auth:ClientId"];
var scope = builder.Configuration["Auth:Scope"];
var apiUrl = builder.Configuration["Api:BaseUrl"];
var configured = !string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(clientId)
    && !string.IsNullOrWhiteSpace(scope) && Uri.TryCreate(apiUrl, UriKind.Absolute, out _);
if (configured)
{
    builder.Services.AddMsalAuthentication(options =>
    {
        options.ProviderOptions.Authentication.Authority = authority;
        options.ProviderOptions.Authentication.ClientId = clientId;
        options.ProviderOptions.Authentication.ValidateAuthority = false;
        options.ProviderOptions.DefaultAccessTokenScopes.Add(scope!);
        options.ProviderOptions.LoginMode = "redirect";
    });
}

builder.Services.AddScoped<IAccountSync>(services => new AccountSync(
    configured ? MsalAuth(services) : AccountAuth.NotConfigured,
    new HttpClient { BaseAddress = new Uri(configured ? apiUrl! : builder.HostEnvironment.BaseAddress) },
    services.GetRequiredService<AccountSession>(),
    services.GetRequiredService<BrowserStore>(),
    services.GetRequiredService<IndexedStore>(),
    services.GetRequiredService<PlayerSuites>(),
    services.GetRequiredService<IJSRuntime>()));

await builder.Build().RunAsync();

// Inicio de sesión con MSAL (Entra External ID, research.md R1).
static AccountAuth MsalAuth(IServiceProvider services)
{
    var state = services.GetRequiredService<AuthenticationStateProvider>();
    var tokens = services.GetRequiredService<IAccessTokenProvider>();
    var navigation = services.GetRequiredService<NavigationManager>();
    return new AccountAuth(
        true,
        async () =>
        {
            var user = (await state.GetAuthenticationStateAsync()).User;
            var oid = user.FindFirst("oid")?.Value;
            return user.Identity?.IsAuthenticated == true && oid is not null
                ? new AccountUser(oid, user.FindFirst("name")?.Value ?? user.Identity.Name ?? "")
                : null;
        },
        async () =>
        {
            var result = await tokens.RequestAccessToken();
            return result.TryGetToken(out var token) ? token.Value : null;
        },
        () => navigation.NavigateToLogin("authentication/login"),
        () => navigation.NavigateToLogout("authentication/logout"),
        changed => state.AuthenticationStateChanged += _ => changed());
}
