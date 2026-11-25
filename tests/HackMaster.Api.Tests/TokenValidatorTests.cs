using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace HackMaster.Api.Tests;

public class TokenValidatorTests
{
    private readonly TestTokens tokens = new();

    [Fact]
    public async Task Valid_Token_Gives_Account_And_Email__F006_FR008()
    {
        var account = await tokens.Validator().ValidateAsync("Bearer " + tokens.Create("oid-1", " Ana@Example.COM ", "Ana"));

        Assert.NotNull(account);
        Assert.Equal("oid-1", account.AccountId);
        Assert.Equal("ana@example.com", account.Email);
        Assert.Equal("Ana", account.Name);
    }

    [Fact]
    public async Task Missing_Or_Malformed_Header_Is_Rejected__F006_FR008()
    {
        var validator = tokens.Validator();

        Assert.Null(await validator.ValidateAsync(null));
        Assert.Null(await validator.ValidateAsync(""));
        Assert.Null(await validator.ValidateAsync("Basic abc"));
        Assert.Null(await validator.ValidateAsync("Bearer no-es-un-token"));
    }

    [Fact]
    public async Task Wrong_Issuer_Is_Rejected__F006_FR008()
    {
        Assert.Null(await tokens.Validator().ValidateAsync("Bearer " + tokens.Create("oid-1", "a@b.c", issuer: "https://otro.example/v2.0")));
    }

    [Fact]
    public async Task Wrong_Audience_Is_Rejected__F006_FR008()
    {
        Assert.Null(await tokens.Validator().ValidateAsync("Bearer " + tokens.Create("oid-1", "a@b.c", audience: "api://otra")));
    }

    [Fact]
    public async Task Expired_Token_Is_Rejected__F006_Edge_SessionExpired()
    {
        Assert.Null(await tokens.Validator().ValidateAsync("Bearer " + tokens.Create("oid-1", "a@b.c", expires: DateTime.UtcNow.AddMinutes(-6))));
    }

    [Fact]
    public async Task Token_Signed_With_Another_Key_Is_Rejected__F006_FR008()
    {
        var other = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "clave-de-prueba" };

        Assert.Null(await tokens.Validator().ValidateAsync("Bearer " + tokens.Create("oid-1", "a@b.c", signingKey: other)));
    }

    [Fact]
    public async Task Signing_Key_Rollover_Refreshes_The_Configuration_Once__F006_FR008()
    {
        var old = new TestTokens();
        var current = new TestTokens("clave-nueva");
        var refreshed = 0;
        var useCurrent = false;
        var validator = new HackMaster.Api.Auth.TokenValidator(
            _ => Task.FromResult(useCurrent ? current.Configuration : old.Configuration),
            TestTokens.Audience,
            () =>
            {
                refreshed++;
                useCurrent = true;
            });

        var account = await validator.ValidateAsync("Bearer " + current.Create("oid-1", "a@b.c"));

        Assert.NotNull(account);
        Assert.Equal(1, refreshed);
    }

    [Fact]
    public async Task Unavailable_Metadata_Is_Reported_As_Unavailable__F006_Edge_Unavailable()
    {
        var validator = new HackMaster.Api.Auth.TokenValidator(_ => throw new InvalidOperationException("sin red"), TestTokens.Audience);

        await Assert.ThrowsAsync<HackMaster.Api.Auth.AuthUnavailableException>(() => validator.ValidateAsync("Bearer " + tokens.Create("oid-1", "a@b.c")));
    }

    [Fact]
    public async Task Preferred_Username_Is_Not_Taken_As_Email__F006_FR003()
    {
        var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Issuer = TestTokens.Issuer,
            Audience = TestTokens.Audience,
            Expires = DateTime.UtcNow.AddHours(1),
            Subject = new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("oid", "oid-1"), new System.Security.Claims.Claim("preferred_username", "admin@pruebas.example")]),
            SigningCredentials = tokens.SigningCredentials,
        });

        Assert.Null(await tokens.Validator().ValidateAsync("Bearer " + token));
    }

    [Fact]
    public async Task Token_Without_Valid_Account_Id_Is_Rejected__F006_FR009()
    {
        Assert.Null(await tokens.Validator().ValidateAsync("Bearer " + tokens.Create("../otra", "a@b.c")));
    }
}
