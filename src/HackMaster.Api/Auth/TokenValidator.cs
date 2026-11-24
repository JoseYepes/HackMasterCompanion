using HackMaster.Domain.Sync;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HackMaster.Api.Auth;

/// <summary>La cuenta que hace la petición, sacada de un token válido.</summary>
public sealed record AccountIdentity(string AccountId, string Email, string Name);

/// <summary>
/// Valida el token de acceso de Entra External ID (research.md R1): firma, emisor, audiencia y
/// caducidad. La configuración (emisor y claves) se obtiene de los metadatos OIDC de la autoridad.
/// </summary>
public sealed class TokenValidator(Func<CancellationToken, Task<OpenIdConnectConfiguration>> configuration, string audience)
{
    private readonly JsonWebTokenHandler handler = new() { MapInboundClaims = false };

    public async Task<AccountIdentity?> ValidateAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        const string scheme = "Bearer ";
        if (authorizationHeader is null || !authorizationHeader.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorizationHeader[scheme.Length..].Trim();
        if (token.Length == 0)
        {
            return null;
        }

        var config = await configuration(cancellationToken);
        var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = config.Issuer,
            ValidAudience = audience,
            IssuerSigningKeys = config.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        });
        if (!result.IsValid)
        {
            return null;
        }

        var oid = Claim(result, "oid");
        var email = Claim(result, "email") ?? Claim(result, "emails") ?? Claim(result, "preferred_username");
        if (!AccountKeys.IsValidAccountId(oid) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return new(oid!, NormalizeEmail(email), Claim(result, "name") ?? email.Trim());
    }

    /// <summary>Forma única de un correo para compararlo: sin espacios y en minúsculas.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? Claim(TokenValidationResult result, string type) =>
        result.Claims.TryGetValue(type, out var value) ? value switch
        {
            string s => s,
            IEnumerable<object> list => list.FirstOrDefault()?.ToString(),
            _ => value?.ToString(),
        } : null;
}
