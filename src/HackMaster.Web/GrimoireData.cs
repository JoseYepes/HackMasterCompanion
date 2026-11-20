using System.Text.Json;
using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>
/// Conjuros y reglas del Grimorio (F009), de <c>data/spells.json</c> incrustado. Va aparte de
/// <see cref="GameData"/> porque se genera con otra hoja y llega por tandas: si el archivo aún no
/// existe, el Grimorio está vacío, sin error.
/// </summary>
public sealed class GrimoireData
{
    private const string ResourceName = "HackMaster.Data.spells.json";

    private readonly Dictionary<(SpellClass, string), Spell> _spells;

    public GrimoireData(IReadOnlyList<Spell> spells, IReadOnlyList<GrimoireRule> rules)
    {
        Spells = SpellQuery.Filter(spells, new SpellCriteria());
        Rules = rules;
        Facets = SpellFacets.From(Spells);
        _spells = Spells.ToDictionary(spell => (spell.Class, spell.Slug));
    }

    private GrimoireData(string error)
        : this([], []) => LoadError = error;

    /// <summary>Explicación del fallo de carga; nulo si se cargó bien (o si aún no hay conjuros).</summary>
    public string? LoadError { get; }

    /// <summary>Conjuros, por nombre.</summary>
    public IReadOnlyList<Spell> Spells { get; }

    public IReadOnlyList<GrimoireRule> Rules { get; }

    /// <summary>Opciones de los filtros.</summary>
    public SpellFacets Facets { get; }

    public Spell? Find(SpellClass spellClass, string slug) => _spells.GetValueOrDefault((spellClass, slug));

    /// <summary>Carga <c>spells.json</c> incrustado; si no está, el Grimorio queda vacío.</summary>
    public static GrimoireData LoadEmbedded()
    {
        using var stream = typeof(GrimoireData).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return new GrimoireData([], []);
        }

        using var reader = new StreamReader(stream);
        return FromJson(reader.ReadToEnd());
    }

    /// <summary>
    /// Lee el contenido de <c>spells.json</c>. Si es de otra versión, está dañado o repite un
    /// conjuro, el Grimorio queda vacío con el motivo en <see cref="LoadError"/>: nunca rompe el
    /// arranque de la aplicación (constitución V).
    /// </summary>
    public static GrimoireData FromJson(string json)
    {
        try
        {
            var file = JsonSerializer.Deserialize<SpellsFile>(json, GameDataJson.Options);
            if (file?.Spells is null || file.Rules is null || file.Spells.Any(spell => spell is null) || file.Rules.Any(rule => rule is null))
            {
                return new GrimoireData("El archivo de conjuros está incompleto.");
            }

            if (file.SchemaVersion != GameDataJson.CurrentSchemaVersion)
            {
                return new GrimoireData($"El archivo de conjuros es de la versión {file.SchemaVersion}; la aplicación entiende la {GameDataJson.CurrentSchemaVersion}.");
            }

            var repeated = file.Spells.GroupBy(spell => (spell.Class, spell.Slug)).FirstOrDefault(group => group.Count() > 1);
            return repeated is not null
                ? new GrimoireData($"El archivo de conjuros tiene repetido «{repeated.First().Name}».")
                : new GrimoireData(file.Spells, file.Rules);
        }
        catch (JsonException exception)
        {
            return new GrimoireData($"El archivo de conjuros está dañado: {exception.Message}");
        }
    }
}
