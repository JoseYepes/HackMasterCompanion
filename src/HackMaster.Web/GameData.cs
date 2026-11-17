using System.Text.Json;
using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>
/// Datos de juego cargados una sola vez al arrancar, con índices por <c>slug</c> y las relaciones
/// inversas (habilidad → conjuntos, elemento → dudas). Si los archivos no se pueden leer, no hay
/// datos parciales: todas las listas quedan vacías y <see cref="LoadError"/> explica el problema.
/// </summary>
public sealed class GameData
{
    private const string ResourcePrefix = "HackMaster.Data.";

    private readonly Dictionary<string, Skill> _skills;
    private readonly Dictionary<(SuiteKind, string), SkillSuite> _suites;
    private readonly Dictionary<string, CombatStyle> _styles;
    private readonly Dictionary<string, List<SkillSuite>> _suitesBySkill;
    private readonly Dictionary<(ElementKind, string), List<Doubt>> _doubtsByTarget;

    /// <summary>Crea los datos a partir de listas ya cargadas (también para las pruebas).</summary>
    public GameData(IReadOnlyList<Skill> skills, IReadOnlyList<SkillSuite> suites, IReadOnlyList<CombatStyle> styles, IReadOnlyList<Doubt> doubts)
    {
        Skills = skills.OrderBy(skill => skill.SearchName, StringComparer.Ordinal).ToList();
        Suites = suites.OrderBy(suite => suite.SearchName, StringComparer.Ordinal).ToList();
        Styles = styles.OrderBy(style => style.SearchName, StringComparer.Ordinal).ToList();
        Doubts = doubts;
        SkillFacets = SkillFacets.From(Skills);

        _skills = Skills.ToDictionary(skill => skill.Slug, StringComparer.Ordinal);
        _suites = Suites.ToDictionary(suite => (suite.Kind, suite.Slug));
        _styles = Styles.ToDictionary(style => style.Slug, StringComparer.Ordinal);

        _suitesBySkill = new Dictionary<string, List<SkillSuite>>(StringComparer.Ordinal);
        foreach (var suite in Suites)
        {
            foreach (var component in suite.Components.Where(component => component.Kind == ElementKind.Skill))
            {
                GetOrAdd(_suitesBySkill, component.Target).Add(suite);
            }
        }

        _doubtsByTarget = [];
        foreach (var doubt in Doubts.Where(doubt => doubt.Target is not null))
        {
            GetOrAdd(_doubtsByTarget, (doubt.Target!.Kind, doubt.Target.Slug)).Add(doubt);
        }
    }

    private GameData(string error)
        : this([], [], [], []) => LoadError = error;

    /// <summary>Explicación del fallo de carga; nulo si los datos se cargaron bien.</summary>
    public string? LoadError { get; }

    /// <summary>Habilidades, por nombre.</summary>
    public IReadOnlyList<Skill> Skills { get; }

    /// <summary>Conjuntos de habilidades y conjuntos oficiales, por nombre.</summary>
    public IReadOnlyList<SkillSuite> Suites { get; }

    /// <summary>Estilos de combate, por nombre.</summary>
    public IReadOnlyList<CombatStyle> Styles { get; }

    /// <summary>Dudas abiertas, en el orden de los datos.</summary>
    public IReadOnlyList<Doubt> Doubts { get; }

    /// <summary>Opciones de los filtros de habilidades.</summary>
    public SkillFacets SkillFacets { get; }

    /// <summary>Carga los archivos de <c>data/</c> incrustados en la aplicación.</summary>
    public static GameData LoadEmbedded()
    {
        try
        {
            return FromJson(
                ReadResource("skills.json"),
                ReadResource("suites.json"),
                ReadResource("styles.json"),
                ReadResource("doubts.json"));
        }
        catch (InvalidOperationException exception)
        {
            return new GameData(exception.Message);
        }
    }

    /// <summary>Lee el contenido de los cuatro archivos de datos.</summary>
    public static GameData FromJson(string skills, string suites, string styles, string doubts)
    {
        try
        {
            var skillsFile = Read<SkillsFile>(skills, "skills.json", file => file.SchemaVersion);
            var suitesFile = Read<SuitesFile>(suites, "suites.json", file => file.SchemaVersion);
            var stylesFile = Read<StylesFile>(styles, "styles.json", file => file.SchemaVersion);
            var doubtsFile = Read<DoubtsFile>(doubts, "doubts.json", file => file.SchemaVersion);

            return new GameData(skillsFile.Skills ?? [], suitesFile.Suites ?? [], stylesFile.Styles ?? [], doubtsFile.Doubts ?? []);
        }
        catch (InvalidDataException exception)
        {
            return new GameData(exception.Message);
        }
    }

    /// <summary>Habilidad con ese <c>slug</c>, o nulo.</summary>
    public Skill? FindSkill(string slug) => _skills.GetValueOrDefault(slug);

    /// <summary>Conjunto de ese tipo con ese <c>slug</c>, o nulo.</summary>
    public SkillSuite? FindSuite(SuiteKind kind, string slug) => _suites.GetValueOrDefault((kind, slug));

    /// <summary>Estilo con ese <c>slug</c>, o nulo.</summary>
    public CombatStyle? FindStyle(string slug) => _styles.GetValueOrDefault(slug);

    /// <summary>Conjuntos que contienen la habilidad, por nombre.</summary>
    public IReadOnlyList<SkillSuite> SuitesContaining(string skillSlug) =>
        _suitesBySkill.TryGetValue(skillSlug, out var suites) ? suites : [];

    /// <summary>Dudas que afectan al elemento.</summary>
    public IReadOnlyList<Doubt> DoubtsFor(ElementKind kind, string slug) =>
        _doubtsByTarget.TryGetValue((kind, slug), out var doubts) ? doubts : [];

    private static T Read<T>(string json, string fileName, Func<T, int> version)
    {
        T? file;
        try
        {
            file = JsonSerializer.Deserialize<T>(json, GameDataJson.Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"No se pudo leer {fileName}: {exception.Message}", exception);
        }

        if (file is null)
        {
            throw new InvalidDataException($"No se pudo leer {fileName}: el archivo está vacío.");
        }

        if (version(file) != GameDataJson.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"{fileName} tiene la versión de datos {version(file)} y la aplicación espera la {GameDataJson.CurrentSchemaVersion}.");
        }

        return file;
    }

    private static string ReadResource(string fileName)
    {
        using var stream = typeof(GameData).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Falta el archivo de datos {fileName} en la aplicación.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static List<TValue> GetOrAdd<TKey, TValue>(Dictionary<TKey, List<TValue>> dictionary, TKey key)
        where TKey : notnull
    {
        if (!dictionary.TryGetValue(key, out var list))
        {
            list = [];
            dictionary[key] = list;
        }

        return list;
    }
}
