using HackMaster.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Los datos de juego se cargan una sola vez al arrancar.
builder.Services.AddSingleton(GameData.LoadEmbedded());
builder.Services.AddSingleton(GrimoireData.LoadEmbedded());

// Azar para el tirador y guardado en el navegador (conjuntos del jugador e historial).
builder.Services.AddSingleton<HackMaster.Domain.IRandom, SystemRandom>();
builder.Services.AddSingleton<BrowserStore>();
builder.Services.AddSingleton<PlayerSuites>();

// Fichas de PJ, guardadas en IndexedDB (F004).
builder.Services.AddSingleton<IndexedStore>();
builder.Services.AddSingleton<PlayerCharacters>();

await builder.Build().RunAsync();
