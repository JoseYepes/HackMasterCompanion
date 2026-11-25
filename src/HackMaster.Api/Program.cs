using HackMaster.Api;
using HackMaster.Api.Auth;
using HackMaster.Api.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

// Punto de entrada de las funciones (modelo aislado). Los ajustes vienen de Azure (ApiSettings).
static string Setting(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Falta el ajuste {name}.");

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var settings = new ApiSettings
        {
            AdminAccountIds = ApiSettings.SplitList(Environment.GetEnvironmentVariable("ADMIN_ACCOUNT_IDS")),
            AllowedOrigins = ApiSettings.SplitList(Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")),
        };
        services.AddSingleton(settings);
        services.AddSingleton(new Cors(settings.AllowedOrigins));

        services.AddSingleton(_ =>
        {
            var authority = Setting("AUTH_AUTHORITY").TrimEnd('/');
            var metadata = new ConfigurationManager<OpenIdConnectConfiguration>(
                authority + "/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true });
            return new TokenValidator(metadata.GetConfigurationAsync, Setting("AUTH_AUDIENCE"), metadata.RequestRefresh);
        });

        services.AddSingleton(_ => CosmosStore.CreateClient(Setting("COSMOS_ENDPOINT"), Setting("COSMOS_KEY")));
        services.AddSingleton(sp => CosmosStore.Create(
            sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>(),
            Environment.GetEnvironmentVariable("COSMOS_DATABASE") is { Length: > 0 } db ? db : "hackmaster"));
        services.AddSingleton<DataApi>();
    })
    .Build();

await host.RunAsync();
