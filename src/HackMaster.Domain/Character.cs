using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HackMaster.Domain;

/// <summary>
/// Valor de una característica: la puntuación y, si el jugador la escribe, su parte fraccionaria
/// (por ejemplo, 15/47). F004 solo guarda lo que escribe el jugador; los rangos de las reglas
/// quedan para F005.
/// </summary>
public sealed record AbilityScore(int? Score = null, int? Fraction = null)
{
    public static AbilityScore Empty { get; } = new();

    /// <summary>La característica como se escribe en la ficha: «15/47», «12/05», «11» o vacío.</summary>
    public override string ToString() => Score is null
        ? string.Empty
        : Fraction is null
            ? Score.Value.ToString(CultureInfo.InvariantCulture)
            : $"{Score.Value.ToString(CultureInfo.InvariantCulture)}/{Fraction.Value.ToString("00", CultureInfo.InvariantCulture)}";
}

/// <summary>Las siete características de una ficha, con las siglas de los manuales en castellano.</summary>
public sealed record CharacterAbilities
{
    public static CharacterAbilities Empty { get; } = new();

    [JsonPropertyName("FUE")]
    public AbilityScore FUE { get; init; } = AbilityScore.Empty;

    [JsonPropertyName("DES")]
    public AbilityScore DES { get; init; } = AbilityScore.Empty;

    [JsonPropertyName("CON")]
    public AbilityScore CON { get; init; } = AbilityScore.Empty;

    [JsonPropertyName("INT")]
    public AbilityScore INT { get; init; } = AbilityScore.Empty;

    [JsonPropertyName("SAB")]
    public AbilityScore SAB { get; init; } = AbilityScore.Empty;

    [JsonPropertyName("CAR")]
    public AbilityScore CAR { get; init; } = AbilityScore.Empty;

    [JsonPropertyName("ASP")]
    public AbilityScore ASP { get; init; } = AbilityScore.Empty;

    public AbilityScore this[Ability ability] => ability switch
    {
        Ability.FUE => FUE,
        Ability.DES => DES,
        Ability.CON => CON,
        Ability.INT => INT,
        Ability.SAB => SAB,
        Ability.CAR => CAR,
        Ability.ASP => ASP,
        _ => throw new ArgumentOutOfRangeException(nameof(ability)),
    };

    /// <summary>Las mismas características con una cambiada.</summary>
    public CharacterAbilities With(Ability ability, AbilityScore score) => ability switch
    {
        Ability.FUE => this with { FUE = score },
        Ability.DES => this with { DES = score },
        Ability.CON => this with { CON = score },
        Ability.INT => this with { INT = score },
        Ability.SAB => this with { SAB = score },
        Ability.CAR => this with { CAR = score },
        Ability.ASP => this with { ASP = score },
        _ => throw new ArgumentOutOfRangeException(nameof(ability)),
    };
}

/// <summary>
/// Una habilidad de la ficha: el slug de la habilidad de los datos, su nombre cuando se añadió (para
/// mostrarla si desaparece de los datos), la maestría escrita a mano (porcentaje; desde F005 solo si
/// el jugador la escribe, y manda sobre la calculada), los puntos y las mejoras tiradas (F005).
/// </summary>
public sealed record CharacterSkill(string Slug, string Name, int? Mastery = null, int? Points = null)
{
    private static readonly IReadOnlyList<SkillImprovement> NoImprovements = [];

    /// <summary>Tiradas del dado de maestría y del Dado de Honor sumadas a la maestría (MJ p. 100–101).</summary>
    public IReadOnlyList<SkillImprovement> Improvements { get; init; } = NoImprovements;

    /// <summary>Compara también las mejoras una a una (la igualdad del registro compararía la lista como objeto).</summary>
    public bool Equals(CharacterSkill? other) =>
        other is not null && Slug == other.Slug && Name == other.Name && Mastery == other.Mastery && Points == other.Points
        && Improvements.SequenceEqual(other.Improvements);

    public override int GetHashCode() => HashCode.Combine(Slug, Name, Mastery, Points, Improvements.Count);
}

/// <summary>Escribe el Dado de Honor como «d12+1»; uno que no esté en la Tabla 5C se lee como vacío.</summary>
public sealed class HonorDieJsonConverter : JsonConverter<HonorDie>
{
    public override bool HandleNull => false;

    public override HonorDie? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            reader.Skip();
            return null;
        }

        return HonorDie.TryParse(reader.GetString(), out var die) ? die : null;
    }

    public override void Write(Utf8JsonWriter writer, HonorDie value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}

/// <summary>Ficha de un personaje jugador (F004). Guarda lo que escribe el jugador, sin aplicar reglas.</summary>
public sealed record Character
{
    /// <summary>Identificador que no cambia al editar; los nombres se pueden repetir.</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Race { get; init; }

    public string? Class { get; init; }

    public int? Level { get; init; }

    public string? Alignment { get; init; }

    public CharacterAbilities Abilities { get; init; } = CharacterAbilities.Empty;

    public int? HitPoints { get; init; }

    public int? Honor { get; init; }

    /// <summary>Dado de Honor que elige el jugador (Tabla 5C, MJ p. 85); F005.</summary>
    [JsonConverter(typeof(HonorDieJsonConverter))]
    public HonorDie? HonorDie { get; init; }

    public IReadOnlyList<CharacterSkill> Skills { get; init; } = [];

    public string? Equipment { get; init; }

    public string? Notes { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Lo que impide guardar la ficha, en castellano; vacío si se puede guardar (FR-002).</summary>
    public IReadOnlyList<string> Problems()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            problems.Add("Escribe un nombre para la ficha.");
        }

        if (Level < 0)
        {
            problems.Add("El nivel no puede ser negativo.");
        }

        foreach (var ability in Enum.GetValues<Ability>())
        {
            var score = Abilities[ability];
            if (score.Score < 0)
            {
                problems.Add($"{ability} no puede ser negativa.");
            }

            if (score.Fraction is < 0 or > 99)
            {
                problems.Add($"La fracción de {ability} va de 0 a 99.");
            }
        }

        foreach (var skill in Skills)
        {
            if (skill.Mastery < 0)
            {
                problems.Add($"La maestría de {skill.Name} no puede ser negativa.");
            }

            if (skill.Mastery > SkillRules.MaxMastery)
            {
                problems.Add($"La maestría de {skill.Name} no puede pasar de {SkillRules.MaxMastery} %.");
            }

            if (skill.Points < 0)
            {
                problems.Add($"Los puntos de {skill.Name} no pueden ser negativos.");
            }

            if (skill.Improvements.Any(improvement => improvement.Mastery < 0 || improvement.Honor < 0))
            {
                problems.Add($"Las mejoras de {skill.Name} no pueden ser negativas.");
            }
        }

        return problems;
    }

    /// <summary>Copia independiente con otro identificador y «(copia)» al final del nombre (FR-005).</summary>
    public Character Duplicate(string newId, DateTimeOffset now) => this with
    {
        Id = newId,
        Name = $"{Name} (copia)",
        Skills = [.. Skills],
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>
    /// Verdadero si las dos fichas tienen los mismos datos, comparando las habilidades una a una (la
    /// igualdad del registro compara la lista como objeto). Sirve para comprobar que exportar e
    /// importar deja la ficha idéntica (SC-001).
    /// </summary>
    private static readonly IReadOnlyList<CharacterSkill> NoSkills = [];

    public bool HasSameData(Character other) =>
        this with { Skills = NoSkills } == other with { Skills = NoSkills } && Skills.SequenceEqual(other.Skills);
}
