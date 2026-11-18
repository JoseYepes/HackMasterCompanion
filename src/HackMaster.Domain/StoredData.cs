using System.Text.Json;

namespace HackMaster.Domain;

/// <summary>Conjuntos del jugador guardados en el dispositivo (clave <c>hackmaster.suites</c>).</summary>
public sealed record PlayerSuitesFile(int SchemaVersion, IReadOnlyList<PlayerSuite> Suites)
{
    public static PlayerSuitesFile Empty { get; } = new(StoredData.CurrentVersion, []);
}

/// <summary>Un dado de una tirada guardada.</summary>
public sealed record StoredDie(int Value, bool Penetration);

/// <summary>Un grupo de dados de una tirada guardada.</summary>
public sealed record StoredGroup(int Count, int Sides, int Sign, int Subtotal, bool Capped, IReadOnlyList<StoredDie> Dice);

/// <summary>Una tirada del historial.</summary>
public sealed record StoredRoll(
    string Formula,
    bool Penetrating,
    int Total,
    DateTimeOffset RolledAt,
    int Constant,
    IReadOnlyList<StoredGroup> Groups);

/// <summary>Historial de tiradas y opción de dados que penetran (clave <c>hackmaster.rolls</c>).</summary>
public sealed record RollHistoryFile(int SchemaVersion, bool Penetrating, IReadOnlyList<StoredRoll> Rolls)
{
    public static RollHistoryFile Empty { get; } = new(StoredData.CurrentVersion, false, []);
}

/// <summary>Resultado de leer algo guardado: el valor, o el motivo por el que no se pudo leer.</summary>
public sealed record StoredRead<T>(T? Value, string? Error)
    where T : class;

/// <summary>
/// Lectura, escritura y migración de lo que se guarda en el dispositivo (FR-018, principio V). Cada
/// archivo lleva <c>schemaVersion</c>; los de una versión anterior se migran a la actual.
/// </summary>
public static class StoredData
{
    /// <summary>Versión del formato que escribe esta aplicación.</summary>
    public const int CurrentVersion = 1;

    public static StoredRead<PlayerSuitesFile> ReadSuites(string? json) => Read(json, PlayerSuitesFile.Empty, file => file.Suites is not null);

    public static StoredRead<RollHistoryFile> ReadRolls(string? json) => Read(json, RollHistoryFile.Empty, file => file.Rolls is not null);

    public static string Write(PlayerSuitesFile file) => JsonSerializer.Serialize(file with { SchemaVersion = CurrentVersion }, GameDataJson.Options);

    public static string Write(RollHistoryFile file) => JsonSerializer.Serialize(file with { SchemaVersion = CurrentVersion }, GameDataJson.Options);

    private static StoredRead<T> Read<T>(string? json, T empty, Func<T, bool> isComplete)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StoredRead<T>(empty, null);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("schemaVersion", out var versionElement)
                || versionElement.ValueKind != JsonValueKind.Number
                || !versionElement.TryGetInt32(out var version))
            {
                return new StoredRead<T>(null, "Los datos guardados no indican su versión de formato.");
            }

            if (version > CurrentVersion)
            {
                return new StoredRead<T>(null, $"Los datos guardados son de una versión más reciente de la aplicación ({version}); esta entiende hasta la {CurrentVersion}.");
            }

            var migrated = Migrate(document.RootElement, version);
            var value = migrated.Deserialize<T>(GameDataJson.Options);
            return value is not null && isComplete(value)
                ? new StoredRead<T>(value, null)
                : new StoredRead<T>(null, "Los datos guardados están incompletos.");
        }
        catch (JsonException exception)
        {
            return new StoredRead<T>(null, $"Los datos guardados están dañados: {exception.Message}");
        }
    }

    /// <summary>
    /// Lleva los datos de <paramref name="version"/> a la versión actual. Con la versión 1 no hay
    /// nada que migrar; cada versión nueva añade aquí su paso, con su prueba.
    /// </summary>
    private static JsonElement Migrate(JsonElement root, int version) => version switch
    {
        CurrentVersion => root,
        _ => throw new JsonException($"No se sabe migrar la versión {version} del formato."),
    };
}
