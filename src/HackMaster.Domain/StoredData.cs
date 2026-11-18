using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace HackMaster.Domain;

/// <summary>Conjuntos del jugador guardados en el dispositivo (clave <c>hackmaster.suites</c>).</summary>
public sealed record PlayerSuitesFile(int SchemaVersion, IReadOnlyList<PlayerSuite> Suites)
{
    public static PlayerSuitesFile Empty { get; } = new(StoredData.SuitesVersion, []);
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

/// <summary>
/// Historial de tiradas y opciones del tirador: dados que penetran, desde la versión 2 dados 3D y
/// desde la 3 el aspecto de los dados (F008) (clave <c>hackmaster.rolls</c>). Al leer,
/// <see cref="Look"/> siempre está puesto.
/// </summary>
public sealed record RollHistoryFile(
    int SchemaVersion,
    bool Penetrating,
    IReadOnlyList<StoredRoll> Rolls,
    [property: JsonPropertyName("dice3d")] bool Dice3D = false,
    DiceLook? Look = null)
{
    public static RollHistoryFile Empty { get; } = new(StoredData.RollsVersion, false, [], Look: DiceLook.Default);
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
    /// <summary>
    /// Versión del formato de los conjuntos que escribe esta aplicación. Cada formato lleva la suya:
    /// así, cambiar uno no hace que una versión anterior de la aplicación rechace el otro.
    /// </summary>
    public const int SuitesVersion = 1;

    /// <summary>
    /// Versión del formato del historial: la 2 (F003) añade la opción de dados 3D y la 3 (F008) el
    /// aspecto de los dados.
    /// </summary>
    public const int RollsVersion = 3;

    public static StoredRead<PlayerSuitesFile> ReadSuites(string? json) =>
        Read(json, PlayerSuitesFile.Empty, IsComplete, SuitesVersion, (root, _) => root);

    public static StoredRead<RollHistoryFile> ReadRolls(string? json) =>
        Read(json, RollHistoryFile.Empty, IsComplete, RollsVersion, MigrateRolls);

    // El deserializador deja en nulo lo que falta o vale null en el JSON aunque el tipo no lo admita;
    // se rechaza aquí para no fallar después.
    private static bool IsComplete(PlayerSuitesFile file) =>
        file.Suites is not null && file.Suites.All(suite =>
            suite is not null && suite.Slug is not null && suite.Name is not null && suite.Abilities is not null
            && suite.Components is not null && suite.Components.All(component => component is not null && component.Slug is not null));

    private static bool IsComplete(RollHistoryFile file) =>
        file.Rolls is not null && file.Rolls.All(roll =>
            roll is not null && roll.Formula is not null && roll.Groups is not null
            && roll.Groups.All(group => group is not null && group.Dice is not null && group.Dice.All(die => die is not null)));

    public static string Write(PlayerSuitesFile file) => JsonSerializer.Serialize(file with { SchemaVersion = SuitesVersion }, GameDataJson.Options);

    public static string Write(RollHistoryFile file) => JsonSerializer.Serialize(file with { SchemaVersion = RollsVersion }, GameDataJson.Options);

    private static StoredRead<T> Read<T>(string? json, T empty, Func<T, bool> isComplete, int currentVersion, Func<JsonElement, int, JsonElement> migrate)
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

            if (version > currentVersion)
            {
                return new StoredRead<T>(null, $"Los datos guardados son de una versión más reciente de la aplicación ({version}); esta entiende hasta la {currentVersion}.");
            }

            var migrated = migrate(document.RootElement, version);
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
    /// Lleva un historial de <paramref name="version"/> a la versión actual, paso a paso. Cada versión
    /// nueva añade aquí su paso, con su prueba.
    /// </summary>
    private static JsonElement MigrateRolls(JsonElement root, int version)
    {
        var node = JsonNode.Parse(root.GetRawText())?.AsObject()
            ?? throw new JsonException("Los datos guardados no son un objeto.");
        for (; version < RollsVersion; version++)
        {
            switch (version)
            {
                case 1:
                    // 1 → 2 (F003): el historial gana la opción de dados 3D, desactivada.
                    node["dice3d"] ??= false;
                    break;
                case 2:
                    // 2 → 3 (F008): el aspecto de los dados, el de por defecto (lo completa RepairLook).
                    node["look"] = null;
                    break;
                default:
                    throw new JsonException($"No se sabe migrar la versión {version} del formato.");
            }

            node["schemaVersion"] = version + 1;
        }

        node["look"] = RepairLook(node["look"]);
        return JsonSerializer.SerializeToElement(node);
    }

    /// <summary>
    /// El aspecto guardado, con los valores que falten o no se entiendan cambiados por los de por
    /// defecto: un aspecto roto no debe hacer perder el historial (FR-012 de F008).
    /// </summary>
    private static JsonObject RepairLook(JsonNode? stored)
    {
        var look = stored as JsonObject;
        var material = Enumerated(look?["material"], DiceLook.Default.Material);
        var colors = DiceLook.DefaultColors(material);
        var repaired = DiceLook.Default with
        {
            Material = material,
            Color = Color(look?["color"]) ?? colors.Color,
            Ink = Color(look?["ink"]) ?? colors.Ink,
            Effect = Enumerated(look?["effect"], DiceLook.Default.Effect),
            Sound = look?["sound"] is JsonValue sound && sound.TryGetValue<bool>(out var on) ? on : DiceLook.Default.Sound,
            Quality = Enumerated(look?["quality"], DiceLook.Default.Quality),
        };
        return JsonSerializer.SerializeToNode(repaired, GameDataJson.Options)!.AsObject();

        static T Enumerated<T>(JsonNode? value, T fallback)
            where T : struct, Enum =>
            value is JsonValue text && text.TryGetValue<string>(out var name) && Enum.TryParse<T>(name, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : fallback;

        static string? Color(JsonNode? value) =>
            value is JsonValue text && text.TryGetValue<string>(out var color) && DiceLook.IsColor(color) ? color : null;
    }
}
