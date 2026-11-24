using HackMaster.Api.Auth;

namespace HackMaster.Api.Tests;

public class CorsTests
{
    private readonly Cors cors = new(["https://joseyepes.github.io"]);

    [Theory]
    [InlineData("https://joseyepes.github.io", true)]
    [InlineData("https://JoseYepes.github.io/", true)]
    [InlineData("http://localhost:5238", true)]
    [InlineData("https://localhost:5238", false)]
    [InlineData("https://evil.example", false)]
    [InlineData("https://joseyepes.github.io.evil.example", false)]
    [InlineData(null, false)]
    [InlineData("no-es-una-direccion", false)]
    public void Only_Pages_And_Localhost_Are_Allowed__F006_ResearchR2(string? origin, bool allowed)
    {
        Assert.Equal(allowed, cors.IsAllowed(origin));
    }

    [Fact]
    public void Allowed_Origin_Gets_Headers_Others_Do_Not__F006_ResearchR2()
    {
        using var allowed = new HttpResponseMessage();
        using var denied = new HttpResponseMessage();

        cors.Apply(allowed.Headers, "http://localhost:5238");
        cors.Apply(denied.Headers, "https://evil.example");

        Assert.Equal("http://localhost:5238", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("If-Match", allowed.Headers.GetValues("Access-Control-Allow-Headers").Single());
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
