using HackMaster.Api;
using HackMaster.Api.Storage;
using Microsoft.Azure.Cosmos;

namespace HackMaster.Api.Tests;

/// <summary>
/// Prueba de integración contra el emulador de Cosmos DB (research.md R7). Se salta si no está la
/// variable <c>COSMOS_EMULATOR_ENDPOINT</c> (p. ej. <c>http://localhost:8081</c>); ver tests/README.md.
/// </summary>
public sealed class EmulatorFactAttribute : FactAttribute
{
    public EmulatorFactAttribute()
    {
        if (string.IsNullOrEmpty(EmulatorFixture.Endpoint))
        {
            Skip = "Sin emulador de Cosmos DB: define COSMOS_EMULATOR_ENDPOINT (tests/README.md).";
        }
    }
}

[CollectionDefinition(Name)]
public sealed class EmulatorCollection : ICollectionFixture<EmulatorFixture>
{
    public const string Name = "Emulador de Cosmos DB";
}

public sealed class EmulatorFixture : IAsyncLifetime
{
    /// <summary>Clave pública y conocida del emulador local de Cosmos DB (no es un secreto).</summary>
    private const string EmulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public const string AdminEmail = "admin@pruebas.example";

    public static string? Endpoint => Environment.GetEnvironmentVariable("COSMOS_EMULATOR_ENDPOINT");

    private CosmosClient? client;

    public CosmosStore Store { get; private set; } = null!;

    public TestTokens Tokens { get; } = new();

    public DataApi Api() => new(Tokens.Validator(), Store, new ApiSettings { AdminEmails = [AdminEmail] });

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Endpoint))
        {
            return;
        }

        client = CosmosStore.CreateClient(Endpoint, EmulatorKey, emulator: true);
        Store = await CosmosStore.CreateForTestsAsync(client, "hackmaster-pruebas");
    }

    public Task DisposeAsync()
    {
        client?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Una cuenta nueva, invitada, para que las pruebas no se pisen.</summary>
    public async Task<string> InvitedAccountAsync(string? email = null)
    {
        var oid = Guid.NewGuid().ToString();
        email ??= $"{oid}@pruebas.example";
        await Store.InviteAsync(email, AdminEmail);
        return Tokens.Bearer(oid, email);
    }
}
