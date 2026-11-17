using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HackMaster.Domain;

/// <summary>Contenido de <c>data/skills.json</c>.</summary>
public sealed record SkillsFile(int SchemaVersion, IReadOnlyList<Skill> Skills);

/// <summary>Contenido de <c>data/suites.json</c>.</summary>
public sealed record SuitesFile(int SchemaVersion, IReadOnlyList<SkillSuite> Suites);

/// <summary>Contenido de <c>data/styles.json</c>.</summary>
public sealed record StylesFile(int SchemaVersion, IReadOnlyList<CombatStyle> Styles);

/// <summary>Contenido de <c>data/doubts.json</c>.</summary>
public sealed record DoubtsFile(int SchemaVersion, IReadOnlyList<Doubt> Doubts);

/// <summary>Formato compartido por quien escribe los datos (importador) y quien los lee (aplicación).</summary>
public static class GameDataJson
{
    /// <summary>Versión del esquema de los archivos de datos.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Opciones de serialización: camelCase, enumeraciones como texto, sangría de 2 espacios y LF.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        Converters = { new JsonStringEnumConverter() },
    };
}
