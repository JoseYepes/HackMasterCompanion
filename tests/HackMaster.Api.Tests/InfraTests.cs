using System.Text.RegularExpressions;

namespace HackMaster.Api.Tests;

/// <summary>
/// Comprobaciones de la infraestructura (F006 FR-013, constitución VII): etiqueta en todo recurso,
/// solo niveles gratuitos y ningún secreto en los archivos de <c>infra/</c>.
/// </summary>
public partial class InfraTests
{
    private static readonly string Infra = Path.Combine(RepoRoot(), "infra");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HackMaster.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("No se encuentra la raíz del repositorio.");
    }

    private static string Main => File.ReadAllText(Path.Combine(Infra, "main.bicep"));

    /// <summary>Cada declaración <c>resource</c> con su cuerpo, hasta la siguiente.</summary>
    private static IEnumerable<(string Name, string Type, string Body)> Resources(string bicep)
    {
        var matches = ResourceRegex().Matches(bicep);
        for (var i = 0; i < matches.Count; i++)
        {
            var end = i + 1 < matches.Count ? matches[i + 1].Index : bicep.Length;
            yield return (matches[i].Groups[1].Value, matches[i].Groups[2].Value, bicep[matches[i].Index..end]);
        }
    }

    [Fact]
    public void Every_Taggable_Resource_Has_The_Project_Tag__F006_FR013()
    {
        Assert.Contains("proyecto: 'hackmaster'", Main);
        var resources = Resources(Main).ToList();
        Assert.NotEmpty(resources);
        foreach (var (name, type, body) in resources.Where(r => !r.Type.EndsWith("/config", StringComparison.Ordinal)))
        {
            Assert.True(body.Contains("tags: tags", StringComparison.Ordinal), $"{name} ({type}) no lleva la etiqueta proyecto=hackmaster.");
        }
    }

    [Fact]
    public void Only_Free_Tiers_Are_Used__F006_FR013()
    {
        var main = Main;
        Assert.Contains("enableFreeTier: true", main);
        Assert.Contains("name: 'Free'", main);
        Assert.DoesNotContain("autoscale", main, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableServerless", main, StringComparison.Ordinal);
        foreach (Match throughput in ThroughputRegex().Matches(main))
        {
            Assert.True(int.Parse(throughput.Groups[1].Value) <= 1000, "Más de 1000 RU/s sale del nivel gratuito de Cosmos DB.");
        }
    }

    [Fact]
    public void Budget_Exists_With_Alerts__F006_FR013()
    {
        var budget = File.ReadAllText(Path.Combine(Infra, "budget.bicep"));

        Assert.Contains("Microsoft.Consumption/budgets", budget);
        Assert.Contains("thresholdType: 'Actual'", budget);
        Assert.Contains("thresholdType: 'Forecasted'", budget);
    }

    [Fact]
    public void Infra_Files_Contain_No_Secrets_Or_Emails__F006_FR014()
    {
        foreach (var file in Directory.EnumerateFiles(Infra, "*", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.False(EmailRegex().IsMatch(text), $"{Path.GetFileName(file)} contiene un correo.");
            Assert.DoesNotContain("AccountKey=", text, StringComparison.Ordinal);
            Assert.False(KeyRegex().IsMatch(text), $"{Path.GetFileName(file)} parece contener una clave.");
        }
    }

    [GeneratedRegex(@"^resource\s+(\w+)\s+'([^@']+)@", RegexOptions.Multiline)]
    private static partial Regex ResourceRegex();

    [GeneratedRegex(@"throughput:\s*(\d+)")]
    private static partial Regex ThroughputRegex();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[A-Za-z0-9+/]{60,}={0,2}")]
    private static partial Regex KeyRegex();
}
