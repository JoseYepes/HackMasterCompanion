using Microsoft.Extensions.Hosting;

// Punto de entrada de las funciones (modelo aislado). Las dependencias se añaden en cada tarea.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

await host.RunAsync();
