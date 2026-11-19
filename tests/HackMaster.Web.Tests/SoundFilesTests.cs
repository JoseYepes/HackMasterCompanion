using System.Text.RegularExpressions;

namespace HackMaster.Web.Tests;

/// <summary>
/// Grabaciones de los dados (F008, research §3): cada archivo que usa el módulo 3D existe, está en el
/// README con su origen y su licencia CC0, ninguno sobra y, juntos, pesan poco.
/// </summary>
public sealed partial class SoundFilesTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "sounds");
    private static readonly string Script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "js", "dice3d.js"));
    private static readonly string Readme = File.ReadAllText(Path.Combine(Folder, "README.md"));

    private static IEnumerable<string> Used() => AudioName().Matches(Script).Select(match => match.Groups[1].Value).Distinct();

    private static IEnumerable<string> OnDisk() =>
        Directory.EnumerateFiles(Folder).Select(Path.GetFileName).OfType<string>().Where(name => AudioName().IsMatch("'" + name + "'"));

    [Fact]
    public void Every_Recording_The_Dice_Use_Exists_And_Is_In_The_Readme__Kenney_OpenGameArt()
    {
        Assert.NotEmpty(Used());
        Assert.All(Used(), name =>
        {
            Assert.True(File.Exists(Path.Combine(Folder, name)), $"Falta {name}.");
            Assert.Contains(name, Readme, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void The_Recordings_Are_Cc0__Kenney_OpenGameArt()
    {
        Assert.Contains("CC0", Readme, StringComparison.Ordinal);
        Assert.Contains("kenney.nl", Readme, StringComparison.Ordinal);
        Assert.Contains("opengameart.org", Readme, StringComparison.Ordinal);
    }

    [Fact]
    public void No_Recording_Is_Left_Unused__SC005()
    {
        Assert.Empty(OnDisk().Except(Used()));
    }

    [Fact]
    public void All_Recordings_Weigh_Less_Than_400_KB__SC005()
    {
        var bytes = OnDisk().Sum(name => new FileInfo(Path.Combine(Folder, name)).Length);
        Assert.InRange(bytes, 1, 400 * 1024);
    }

    [GeneratedRegex(@"'([\w-]+\.(?:ogg|mp3))'")]
    private static partial Regex AudioName();
}
