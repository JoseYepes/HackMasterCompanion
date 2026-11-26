using System.Buffers.Binary;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HackMaster.Web.Tests;

/// <summary>
/// Archivos que hacen instalable la aplicación (F013, specs/011-instalable-sin-conexion): la
/// descripción de la aplicación, sus iconos, los enlaces de <c>index.html</c> y el trabajador de
/// servicio de desarrollo. Se leen los archivos de <c>wwwroot</c> copiados junto a las pruebas.
/// </summary>
public sealed partial class PwaFilesTests
{
    private static string WebFile(string name) => Path.Combine(AppContext.BaseDirectory, "pwa", name);

    private static JsonElement Manifest()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(WebFile("manifest.webmanifest")));
        return document.RootElement.Clone();
    }

    private static string IndexHtml => File.ReadAllText(WebFile("index.html"));

    [Fact]
    public void Manifest_Names_The_App_In_Spanish__FR001()
    {
        var manifest = Manifest();
        Assert.Equal("HackMaster Companion", manifest.GetProperty("name").GetString());
        Assert.Equal("HackMaster", manifest.GetProperty("short_name").GetString());
        Assert.Equal("es", manifest.GetProperty("lang").GetString());
        Assert.False(string.IsNullOrWhiteSpace(manifest.GetProperty("description").GetString()));
    }

    [Fact]
    public void Manifest_Opens_In_Its_Own_Window__FR001()
    {
        Assert.Equal("standalone", Manifest().GetProperty("display").GetString());
    }

    [Fact]
    public void Manifest_Start_Scope_And_Id_Are_Relative_To_The_Base_Path__FR002()
    {
        var manifest = Manifest();
        Assert.Equal("./", manifest.GetProperty("start_url").GetString());
        Assert.Equal("./", manifest.GetProperty("scope").GetString());
        Assert.Equal("./", manifest.GetProperty("id").GetString());
    }

    [Fact]
    public void Manifest_Colors_Are_The_Normal_Mode_Colors__FR001_F007()
    {
        var manifest = Manifest();
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "css", "app.css"));
        Assert.Equal(CssToken(css, "--nav-bg"), manifest.GetProperty("theme_color").GetString());
        Assert.Equal(CssToken(css, "--paper"), manifest.GetProperty("background_color").GetString());
        Assert.Contains($"<meta name=\"theme-color\" content=\"{CssToken(css, "--nav-bg")}\" />", IndexHtml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(192)]
    [InlineData(512)]
    public void Manifest_Has_A_Png_Icon_Of_Each_Required_Size__SC003(int size)
    {
        var icon = Manifest().GetProperty("icons").EnumerateArray()
            .Single(i => i.GetProperty("sizes").GetString() == $"{size}x{size}" && Purpose(i) == "any");
        Assert.Equal("image/png", icon.GetProperty("type").GetString());
        Assert.Equal((size, size), PngSize(WebFile(icon.GetProperty("src").GetString()!)));
    }

    [Fact]
    public void Manifest_Has_A_Maskable_Icon_For_Android__SC003()
    {
        var icon = Manifest().GetProperty("icons").EnumerateArray().Single(i => Purpose(i) == "maskable");
        Assert.Equal("512x512", icon.GetProperty("sizes").GetString());
        Assert.Equal((512, 512), PngSize(WebFile(icon.GetProperty("src").GetString()!)));
    }

    [Fact]
    public void Index_Links_The_Manifest_And_The_Apple_Icon__FR001()
    {
        Assert.Contains("<link rel=\"manifest\" href=\"manifest.webmanifest\" />", IndexHtml, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"apple-touch-icon\" href=\"icon-192.png\" />", IndexHtml, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"apple-mobile-web-app-title\" content=\"HackMaster\" />", IndexHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Index_Loads_The_Pwa_Script_Before_Blazor__ContractsPwa()
    {
        var pwa = IndexHtml.IndexOf("<script src=\"js/pwa.js\"></script>", StringComparison.Ordinal);
        var blazor = IndexHtml.IndexOf("_framework/blazor.webassembly", StringComparison.Ordinal);
        Assert.True(pwa >= 0, "index.html no carga js/pwa.js.");
        Assert.True(pwa < blazor, "js/pwa.js debe cargarse antes que Blazor para no perder el aviso de instalar.");
    }

    [Fact]
    public void Project_Publishes_The_Offline_Service_Worker__ResearchR1()
    {
        var project = File.ReadAllText(WebFile("HackMaster.Web.csproj"));
        Assert.Contains("<ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>", project, StringComparison.Ordinal);
        Assert.Contains("<ServiceWorker Include=\"wwwroot\\service-worker.js\" PublishedContent=\"wwwroot\\service-worker.published.js\" />", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_Service_Worker_Keeps_Nothing__FR011()
    {
        var worker = File.ReadAllText(WebFile("service-worker.js"));
        Assert.DoesNotContain("caches", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("respondWith", worker, StringComparison.Ordinal);
    }

    private static string Purpose(JsonElement icon) =>
        icon.TryGetProperty("purpose", out var purpose) ? purpose.GetString()! : "any";

    private static (int Width, int Height) PngSize(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G', $"{path} no es un PNG.");
        return (BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static string CssToken(string css, string name)
    {
        var root = RootBlock().Match(css).Groups[1].Value;
        return Regex.Match(root, Regex.Escape(name) + @":\s*(#[0-9a-fA-F]{6});").Groups[1].Value;
    }

    [GeneratedRegex(@":root\s*\{([^}]*)\}")]
    private static partial Regex RootBlock();
}
