using HackMaster.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Los datos de juego se cargan una sola vez al arrancar.
builder.Services.AddSingleton(GameData.LoadEmbedded());

await builder.Build().RunAsync();
