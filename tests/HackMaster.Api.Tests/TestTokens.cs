using System.Security.Claims;
using System.Security.Cryptography;
using HackMaster.Api.Auth;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HackMaster.Api.Tests;

/// <summary>Emisor de tokens de prueba con una clave generada en cada ejecución (nunca una real).</summary>
public sealed class TestTokens
{
    public const string Issuer = "https://pruebas.example/tenant/v2.0";
    public const string Audience = "api://hackmaster-pruebas";

    private readonly RsaSecurityKey key = new(RSA.Create(2048)) { KeyId = "clave-de-prueba" };

    public OpenIdConnectConfiguration Configuration
    {
        get
        {
            var config = new OpenIdConnectConfiguration { Issuer = Issuer };
            config.SigningKeys.Add(key);
            return config;
        }
    }

    public TokenValidator Validator() => new(_ => Task.FromResult(Configuration), Audience);

    public string Create(
        string oid,
        string email,
        string name = "Jugador",
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expires = null,
        SecurityKey? signingKey = null)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            NotBefore = now.AddMinutes(-10),
            IssuedAt = now.AddMinutes(-10),
            Expires = expires ?? now.AddHours(1),
            Subject = new ClaimsIdentity([new Claim("oid", oid), new Claim("email", email), new Claim("name", name)]),
            SigningCredentials = new SigningCredentials(signingKey ?? key, SecurityAlgorithms.RsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public string Bearer(string oid, string email, string name = "Jugador") => "Bearer " + Create(oid, email, name);
}
