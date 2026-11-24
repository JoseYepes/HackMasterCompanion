using System.Net.Mail;
using System.Text.Json;
using HackMaster.Api.Auth;
using HackMaster.Api.Storage;
using HackMaster.Domain.Sync;
using Microsoft.Azure.Cosmos;

namespace HackMaster.Api;

/// <summary>Respuesta de la API, independiente de Azure Functions para poder probarla.</summary>
public sealed record ApiResult(int Status, string? Json = null, string? FileName = null);

/// <summary>Documento tal como viaja entre la aplicación y la API (contracts/api.md).</summary>
public sealed record DocDto(
    string Id,
    string Kind,
    int SchemaVersion,
    DateTimeOffset UpdatedAt,
    bool Deleted,
    string? Payload,
    [property: System.Text.Json.Serialization.JsonPropertyName("etag")] string? ETag = null);

/// <summary>
/// Lo que hace cada ruta de la API (contracts/api.md). Comprueba el token y la invitación en cada
/// petición y solo toca la partición de la cuenta del token (FR-003, FR-008).
/// </summary>
public sealed class DataApi(TokenValidator tokens, CosmosStore store, ApiSettings settings)
{
    /// <summary>Tamaño máximo del documento: Cosmos admite 2 MB y deja margen para los demás campos.</summary>
    public const int MaxPayloadChars = 2 * 1024 * 1024 - 64 * 1024;

    private const int MaxItemIdLength = 200;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiResult> MeAsync(string? authorization, CancellationToken ct = default) =>
        WithAccountAsync(authorization, ct, (account, isAdmin) =>
            Task.FromResult(Ok(new { name = account.Name, email = account.Email, isAdmin })));

    public Task<ApiResult> GetDataAsync(string? authorization, long? since, CancellationToken ct = default) =>
        WithAccountAsync(authorization, ct, async (account, _) =>
        {
            var serverTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var items = await store.ListAsync(account.AccountId, since, ct);
            var latest = items.Select(i => i.Timestamp ?? 0).DefaultIfEmpty(0).Max();
            return Ok(new { items = items.Select(ToDto), serverTime = Math.Max(serverTime, latest) });
        });

    public Task<ApiResult> PutAsync(string? authorization, string id, string? ifMatch, string? ifNoneMatch, string? body, CancellationToken ct = default) =>
        WithAccountAsync(authorization, ct, async (account, _) =>
        {
            DocDto? doc;
            try
            {
                doc = JsonSerializer.Deserialize<DocDto>(body ?? "", JsonOptions);
            }
            catch (JsonException)
            {
                return Error(400, "invalid-json");
            }

            if (doc is null || doc.Id != id || !IsValidId(doc.Id, doc.Kind) || doc.SchemaVersion < 1)
            {
                return Error(400, "invalid-document");
            }

            if (doc.Payload is { Length: > MaxPayloadChars })
            {
                return Error(413, "too-large");
            }

            var create = ifNoneMatch == "*";
            if (create == (ifMatch is not null))
            {
                return Error(400, "precondition-required");
            }

            var cloud = new CloudDoc
            {
                Id = doc.Id,
                UserId = account.AccountId,
                Kind = doc.Kind,
                SchemaVersion = doc.SchemaVersion,
                UpdatedAt = doc.UpdatedAt,
                Deleted = doc.Deleted,
                Payload = doc.Deleted ? null : doc.Payload,
            };
            var (outcome, saved) = await store.SaveAsync(cloud, create ? null : ifMatch, ct);
            return outcome == SaveOutcome.Saved
                ? Ok(ToDto(saved!))
                : saved is null ? Error(409, "conflict") : new ApiResult(409, Serialize(ToDto(saved)));
        });

    public Task<ApiResult> ExportAsync(string? authorization, CancellationToken ct = default) =>
        WithAccountAsync(authorization, ct, async (account, _) =>
        {
            var items = await store.ListAsync(account.AccountId, null, ct);
            var export = new { schemaVersion = 1, exportedAt = DateTimeOffset.UtcNow, items = items.Where(i => !i.Deleted).Select(ToDto) };
            return new ApiResult(200, Serialize(export), "hackmaster-cuenta.json");
        });

    public Task<ApiResult> DeleteAccountAsync(string? authorization, CancellationToken ct = default) =>
        WithAccountAsync(authorization, ct, async (account, _) =>
        {
            await store.DeleteAllAsync(account.AccountId, ct);
            return new ApiResult(204);
        });

    public Task<ApiResult> ListInvitationsAsync(string? authorization, CancellationToken ct = default) =>
        WithAdminAsync(authorization, ct, async _ =>
            Ok((await store.ListInvitationsAsync(ct)).Select(i => new { email = i.Email, invitedAt = i.InvitedAt })));

    public Task<ApiResult> InviteAsync(string? authorization, string email, CancellationToken ct = default) =>
        WithAdminAsync(authorization, ct, async admin =>
        {
            if (!IsEmail(email))
            {
                return Error(400, "invalid-email");
            }

            await store.InviteAsync(email, admin.Email, ct);
            return new ApiResult(204);
        });

    public Task<ApiResult> UninviteAsync(string? authorization, string email, CancellationToken ct = default) =>
        WithAdminAsync(authorization, ct, async _ =>
        {
            await store.UninviteAsync(email, ct);
            return new ApiResult(204);
        });

    private bool IsAdmin(AccountIdentity account) =>
        settings.AdminEmails.Any(a => TokenValidator.NormalizeEmail(a) == account.Email);

    private async Task<ApiResult> WithAccountAsync(string? authorization, CancellationToken ct, Func<AccountIdentity, bool, Task<ApiResult>> action)
    {
        try
        {
            var account = await tokens.ValidateAsync(authorization, ct);
            if (account is null)
            {
                return Error(401, "unauthorized");
            }

            var isAdmin = IsAdmin(account);
            if (!isAdmin && !await store.IsInvitedAsync(account.Email, ct))
            {
                return Error(403, "not-invited");
            }

            return await action(account, isAdmin);
        }
        catch (Exception ex) when (ex is CosmosException or HttpRequestException or TaskCanceledException)
        {
            return Error(503, "unavailable");
        }
    }

    private Task<ApiResult> WithAdminAsync(string? authorization, CancellationToken ct, Func<AccountIdentity, Task<ApiResult>> action) =>
        WithAccountAsync(authorization, ct, (account, isAdmin) => isAdmin ? action(account) : Task.FromResult(Error(403, "not-admin")));

    private static bool IsValidId(string id, string kind)
    {
        if (SyncKinds.CurrentVersion(kind) is null || !id.StartsWith(kind + ":", StringComparison.Ordinal))
        {
            return false;
        }

        var itemId = id[(kind.Length + 1)..];
        return itemId.Length is > 0 and <= MaxItemIdLength && !itemId.Any(c => c is '/' or '\\' or '?' or '#' || char.IsControl(c));
    }

    private static bool IsEmail(string email) =>
        MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim() && email.Contains('@');

    private static DocDto ToDto(CloudDoc doc) =>
        new(doc.Id, doc.Kind, doc.SchemaVersion, doc.UpdatedAt, doc.Deleted, doc.Payload, doc.ETag);

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static ApiResult Ok(object value) => new(200, Serialize(value));

    private static ApiResult Error(int status, string error) => new(status, Serialize(new { error }));
}
