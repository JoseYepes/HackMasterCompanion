using System.Net;
using Microsoft.Azure.Cosmos;

namespace HackMaster.Api.Storage;

/// <summary>Documento de la cuenta tal como se guarda en el contenedor <c>userdata</c> (data-model.md).</summary>
public sealed class CloudDoc
{
    public string Id { get; set; } = "";

    public string UserId { get; set; } = "";

    public string Kind { get; set; } = "";

    public int SchemaVersion { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public string? Payload { get; set; }

    /// <summary>Hora de la nube del último cambio (segundos Unix), la pone Cosmos.</summary>
    [Newtonsoft.Json.JsonProperty("_ts")]
    public long? Timestamp { get; set; }

    [Newtonsoft.Json.JsonProperty("_etag")]
    public string? ETag { get; set; }
}

/// <summary>Una invitación del contenedor <c>invitations</c>.</summary>
public sealed class Invitation
{
    public string Id { get; set; } = "";

    public string Email { get; set; } = "";

    public DateTimeOffset InvitedAt { get; set; }

    public string InvitedBy { get; set; } = "";
}

/// <summary>Resultado de guardar un documento.</summary>
public enum SaveOutcome
{
    Saved,
    Conflict,
}

/// <summary>Acceso a Cosmos DB (research.md R3). Toda consulta de datos va por la partición de la cuenta.</summary>
public sealed class CosmosStore(Container userData, Container invitations)
{
    public const string UserDataContainer = "userdata";
    public const string InvitationsContainer = "invitations";

    public static CosmosClient CreateClient(string endpoint, string key, bool emulator = false) =>
        new(endpoint, key, new CosmosClientOptions
        {
            ApplicationName = "hackmaster-api",
            SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase, IgnoreNullValues = true },
            ConnectionMode = emulator ? ConnectionMode.Gateway : ConnectionMode.Direct,
            LimitToEndpoint = emulator,
        });

    public static CosmosStore Create(CosmosClient client, string database) =>
        new(client.GetContainer(database, UserDataContainer), client.GetContainer(database, InvitationsContainer));

    /// <summary>Solo para los tests: en Azure la base de datos y los contenedores los crea Bicep.</summary>
    public static async Task<CosmosStore> CreateForTestsAsync(CosmosClient client, string database)
    {
        var db = (await client.CreateDatabaseIfNotExistsAsync(database)).Database;
        var data = (await db.CreateContainerIfNotExistsAsync(UserDataContainer, "/userId")).Container;
        var invites = (await db.CreateContainerIfNotExistsAsync(InvitationsContainer, "/email")).Container;
        return new(data, invites);
    }

    public async Task<IReadOnlyList<CloudDoc>> ListAsync(string userId, long? since, CancellationToken ct = default)
    {
        var query = since is null
            ? new QueryDefinition("SELECT * FROM c")
            : new QueryDefinition("SELECT * FROM c WHERE c._ts >= @since").WithParameter("@since", since.Value);
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey(userId) };
        var items = new List<CloudDoc>();
        using var iterator = userData.GetItemQueryIterator<CloudDoc>(query, requestOptions: options);
        while (iterator.HasMoreResults)
        {
            items.AddRange(await iterator.ReadNextAsync(ct));
        }

        return items;
    }

    public async Task<CloudDoc?> GetAsync(string userId, string id, CancellationToken ct = default)
    {
        try
        {
            return (await userData.ReadItemAsync<CloudDoc>(id, new PartitionKey(userId), cancellationToken: ct)).Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Crea (sin <paramref name="ifMatch"/>) o sustituye (con la versión esperada). Si la nube tiene
    /// otra versión, devuelve <see cref="SaveOutcome.Conflict"/> con la de la nube.
    /// </summary>
    public async Task<(SaveOutcome Outcome, CloudDoc? Doc)> SaveAsync(CloudDoc doc, string? ifMatch, CancellationToken ct = default)
    {
        try
        {
            var response = ifMatch is null
                ? await userData.CreateItemAsync(doc, new PartitionKey(doc.UserId), cancellationToken: ct)
                : await userData.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.UserId), new ItemRequestOptions { IfMatchEtag = ifMatch }, ct);
            return (SaveOutcome.Saved, response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed or HttpStatusCode.NotFound)
        {
            return (SaveOutcome.Conflict, await GetAsync(doc.UserId, doc.Id, ct));
        }
    }

    public async Task DeleteAllAsync(string userId, CancellationToken ct = default)
    {
        foreach (var doc in await ListAsync(userId, null, ct))
        {
            try
            {
                await userData.DeleteItemAsync<CloudDoc>(doc.Id, new PartitionKey(userId), cancellationToken: ct);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Ya no estaba.
            }
        }
    }

    public async Task<bool> IsInvitedAsync(string email, CancellationToken ct = default)
    {
        try
        {
            await invitations.ReadItemAsync<Invitation>(email, new PartitionKey(email), cancellationToken: ct);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task InviteAsync(string email, string invitedBy, CancellationToken ct = default)
    {
        email = Auth.TokenValidator.NormalizeEmail(email);
        var invitation = new Invitation { Id = email, Email = email, InvitedAt = DateTimeOffset.UtcNow, InvitedBy = invitedBy };
        await invitations.UpsertItemAsync(invitation, new PartitionKey(email), cancellationToken: ct);
    }

    public async Task UninviteAsync(string email, CancellationToken ct = default)
    {
        email = Auth.TokenValidator.NormalizeEmail(email);
        try
        {
            await invitations.DeleteItemAsync<Invitation>(email, new PartitionKey(email), cancellationToken: ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Ya no estaba.
        }
    }

    public async Task<IReadOnlyList<Invitation>> ListInvitationsAsync(CancellationToken ct = default)
    {
        var items = new List<Invitation>();
        using var iterator = invitations.GetItemQueryIterator<Invitation>("SELECT * FROM c");
        while (iterator.HasMoreResults)
        {
            items.AddRange(await iterator.ReadNextAsync(ct));
        }

        return [.. items.OrderBy(i => i.Email, StringComparer.Ordinal)];
    }
}
