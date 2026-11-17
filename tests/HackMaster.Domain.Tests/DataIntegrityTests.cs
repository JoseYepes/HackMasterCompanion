using System.Text.Json;
using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>
/// Comprueba los datos generados que se versionan en <c>data/</c> y <c>docs/dudas.md</c>
/// (SC-001, SC-009, SC-010, FR-019, FR-022, FR-023). Si fallan tras una importaciÃ³n, la hoja ha cambiado.
/// </summary>
public sealed class DataIntegrityTests
{
    private static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "data");

    private static string ReadText(string fileName) => File.ReadAllText(Path.Combine(DataDirectory, fileName));

    private static T Read<T>(string fileName) =>
        JsonSerializer.Deserialize<T>(ReadText(fileName), GameDataJson.Options)
        ?? throw new InvalidDataException($"{fileName} estÃ¡ vacÃ­o.");

    private static readonly SkillsFile SkillsData = Read<SkillsFile>("skills.json");
    private static readonly SuitesFile SuitesData = Read<SuitesFile>("suites.json");
    private static readonly StylesFile StylesData = Read<StylesFile>("styles.json");
    private static readonly DoubtsFile DoubtsData = Read<DoubtsFile>("doubts.json");

    private static IEnumerable<SkillSuite> SuitesOf(SuiteKind kind) => SuitesData.Suites.Where(suite => suite.Kind == kind);

    /// <summary>Slugs que existen para cada tipo de elemento.</summary>
    private static readonly Dictionary<ElementKind, HashSet<string>> Slugs = new()
    {
        [ElementKind.Skill] = [.. SkillsData.Skills.Select(skill => skill.Slug)],
        [ElementKind.Suite] = [.. SuitesOf(SuiteKind.Custom).Select(suite => suite.Slug)],
        [ElementKind.OfficialSuite] = [.. SuitesOf(SuiteKind.Official).Select(suite => suite.Slug)],
        [ElementKind.Style] = [.. StylesData.Styles.Select(style => style.Slug)],
    };

    private static bool Exists(ElementKind kind, string slug) => Slugs[kind].Contains(slug);

    [Fact]
    public void All_Files_Have_The_Current_Schema_Version__FR011()
    {
        Assert.All(
            new[] { SkillsData.SchemaVersion, SuitesData.SchemaVersion, StylesData.SchemaVersion, DoubtsData.SchemaVersion },
            version => Assert.Equal(GameDataJson.CurrentSchemaVersion, version));
    }

    [Fact]
    public void Data_Has_343_Skills_36_Suites_23_Official_Suites_And_5_Styles__SC001()
    {
        Assert.Equal(343, SkillsData.Skills.Count);
        Assert.Equal(36, SuitesOf(SuiteKind.Custom).Count());
        Assert.Equal(23, SuitesOf(SuiteKind.Official).Count());
        Assert.Equal(5, StylesData.Styles.Count);
    }

    [Fact]
    public void Slugs_Are_Unique_Within_Each_Kind__FR019()
    {
        var lists = new Dictionary<ElementKind, IReadOnlyList<string>>
        {
            [ElementKind.Skill] = [.. SkillsData.Skills.Select(skill => skill.Slug)],
            [ElementKind.Suite] = [.. SuitesOf(SuiteKind.Custom).Select(suite => suite.Slug)],
            [ElementKind.OfficialSuite] = [.. SuitesOf(SuiteKind.Official).Select(suite => suite.Slug)],
            [ElementKind.Style] = [.. StylesData.Styles.Select(style => style.Slug)],
        };

        foreach (var (kind, slugs) in lists)
        {
            var repeated = slugs.GroupBy(slug => slug).Where(group => group.Count() > 1).Select(group => group.Key);
            Assert.True(!repeated.Any(), $"{kind}: slugs repetidos: {string.Join(", ", repeated)}");
        }
    }

    [Fact]
    public void Every_Prerequisite_Link_Points_To_An_Existing_Element__SC010()
    {
        var segments = SkillsData.Skills.Select(skill => (skill.Name, skill.Prerequisite))
            .Concat(SuitesData.Suites.Select(suite => (suite.Name, suite.Prerequisite)))
            .Concat(StylesData.Styles.Select(style => (style.Name, style.Prerequisite)))
            .SelectMany(owner => owner.Prerequisite.Segments.Select(segment => (Owner: owner.Name, Segment: segment)))
            .Where(item => item.Segment.Target is not null)
            .ToList();

        Assert.NotEmpty(segments);
        var broken = segments
            .Where(item => !Exists(ToElementKind(item.Segment.Kind), item.Segment.Target!))
            .Select(item => $"{item.Owner} â†’ {item.Segment.Kind} Â«{item.Segment.Target}Â»");
        Assert.Empty(broken);
    }

    [Fact]
    public void Every_Suite_Component_Points_To_An_Existing_Element__SC010()
    {
        var components = SuitesData.Suites
            .SelectMany(suite => suite.Components.Select(component => (Suite: suite.Name, Component: component)))
            .ToList();

        Assert.NotEmpty(components);
        var broken = components
            .Where(item => !Exists(item.Component.Kind, item.Component.Target))
            .Select(item => $"{item.Suite} â†’ {item.Component.Kind} Â«{item.Component.Target}Â»");
        Assert.Empty(broken);
    }

    [Fact]
    public void Every_Doubt_Target_Exists__FR022()
    {
        var targeted = DoubtsData.Doubts.Where(doubt => doubt.Target is not null).ToList();

        Assert.NotEmpty(targeted);
        var broken = targeted
            .Where(doubt => !Exists(doubt.Target!.Kind, doubt.Target.Slug))
            .Select(doubt => $"{doubt.Id} â†’ {doubt.Target!.Kind} Â«{doubt.Target.Slug}Â»");
        Assert.Empty(broken);
    }

    [Fact]
    public void No_Data_File_Has_A_Manual_Text_Property__SC009()
    {
        foreach (var fileName in new[] { "skills.json", "suites.json", "styles.json", "doubts.json" })
        {
            Assert.DoesNotContain("manualText", ReadText(fileName), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Doubts_Document_Lists_The_Subject_Of_Every_Doubt__FR023()
    {
        var document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "docs", "dudas.md"));

        Assert.NotEmpty(DoubtsData.Doubts);
        var missing = DoubtsData.Doubts
            .Where(doubt => !document.Contains(doubt.Subject, StringComparison.Ordinal))
            .Select(doubt => doubt.Id);
        Assert.Empty(missing);
    }

    private static ElementKind ToElementKind(PrerequisiteSegmentKind kind) => kind switch
    {
        PrerequisiteSegmentKind.Skill => ElementKind.Skill,
        PrerequisiteSegmentKind.Suite => ElementKind.Suite,
        PrerequisiteSegmentKind.OfficialSuite => ElementKind.OfficialSuite,
        _ => throw new InvalidDataException($"Un segmento de tipo {kind} no deberÃ­a tener destino."),
    };
}
