using HackMaster.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Los datos de juego se cargan una sola vez al arrancar.
builder.Services.AddSingleton(GameData.LoadEmbedded());

// Azar para el tirador y guardado en el navegador (conjuntos del jugador e historial).
builder.Services.AddSingleton<HackMaster.Domain.IRandom, SystemRandom>();
builder.Services.AddSingleton<BrowserStore>();

await builder.Build().RunAsync();
