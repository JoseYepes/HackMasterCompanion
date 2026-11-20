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

/// <summary>Fichas guardadas en el dispositivo (clave <c>hackmaster.characters</c>, IndexedDB; F004).</summary>
public sealed record CharactersFile(int SchemaVersion, IReadOnlyList<Character> Characters)
{
    public static CharactersFile Empty { get; } = new(StoredData.CharactersVersion, []);
}

/// <summary>Archivo de una ficha para exportar e importar (contracts/character-file.md de F004).</summary>
public sealed record CharacterFile(int SchemaVersion, string Kind, Character Character)
{
    /// <summary>Valor de <see cref="Kind"/> que identifica un archivo de ficha.</summary>
    public const string CharacterKind = "hackmaster.character";
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

    /// <summary>
    /// Versión del formato de las fichas, el mismo en lo guardado y en el archivo de exportar. La 1 es
    /// la del borrador de PRODUCTO.md B3 (seis características); la 2 añade Aspecto (F004); la 3 añade
    /// el Dado de Honor y las mejoras de cada habilidad (F005).
    /// </summary>
    public const int CharactersVersion = 3;

    public static StoredRead<CharactersFile> ReadCharacters(string? json) =>
        Read(json, CharactersFile.Empty, IsComplete, CharactersVersion, MigrateCharacters);

    public static string Write(CharactersFile file) => JsonSerializer.Serialize(file with { SchemaVersion = CharactersVersion }, GameDataJson.Options);

    /// <summary>Lee un archivo de ficha importado; si no es una ficha o no se entiende, da el motivo (FR-009).</summary>
    public static StoredRead<CharacterFile> ReadCharacterFile(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StoredRead<CharacterFile>(null, "El archivo está vacío.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && (!root.TryGetProperty("kind", out var kind) || kind.ValueKind != JsonValueKind.String || kind.GetString() != CharacterFile.CharacterKind))
            {
                return new StoredRead<CharacterFile>(null, "El archivo no es una ficha de HackMaster Companion.");
            }
        }
        catch (JsonException exception)
        {
            return new StoredRead<CharacterFile>(null, $"El archivo está dañado: {exception.Message}");
        }

        return Read<CharacterFile>(json, null!, file => file.Character is not null && IsComplete(file.Character), CharactersVersion, MigrateCharacterFile, isFile: true);
    }

    /// <summary>Archivo de exportar de una ficha, en la versión actual.</summary>
    public static string WriteCharacterFile(Character character) =>
        JsonSerializer.Serialize(new CharacterFile(CharactersVersion, CharacterFile.CharacterKind, character), GameDataJson.Options);

    private static bool IsComplete(CharactersFile file) =>
        file.Characters is not null && file.Characters.All(character => character is not null && IsComplete(character));

    private static bool IsComplete(Character character) =>
        !string.IsNullOrWhiteSpace(character.Id) && !string.IsNullOrWhiteSpace(character.Name)
        && character.Abilities is not null && character.Skills is not null
        && character.Skills.All(skill => skill is not null && skill.Slug is not null && skill.Name is not null);

    private static JsonElement MigrateCharacters(JsonElement root, int version)
    {
        var node = ToObject(root);
        if (node["characters"] is JsonArray characters)
        {
            foreach (var character in characters)
            {
                MigrateCharacter(character as JsonObject, version);
            }
        }

        node["schemaVersion"] = CharactersVersion;
        return JsonSerializer.SerializeToElement(node);
    }

    private static JsonElement MigrateCharacterFile(JsonElement root, int version)
    {
        var node = ToObject(root);
        MigrateCharacter(node["character"] as JsonObject, version);
        node["schemaVersion"] = CharactersVersion;
        return JsonSerializer.SerializeToElement(node);
    }

    /// <summary>
    /// Lleva una ficha de <paramref name="version"/> a la actual, paso a paso, y deja las siete
    /// características presentes (vacías las que falten). Cada versión nueva añade aquí su paso.
    /// </summary>
    private static void MigrateCharacter(JsonObject? character, int version)
    {
        if (character is null)
        {
            return;
        }

        for (; version < CharactersVersion; version++)
        {
            switch (version)
            {
                case 1:
                    // 1 → 2 (F004): se añade Aspecto, vacío; lo completa el bucle de abajo.
                    break;
                case 2:
                    // 2 → 3 (F005): sin Dado de Honor y sin mejoras; la maestría de F004 queda como
                    // maestría escrita a mano. Las mejoras vacías las completa el bucle de abajo.
                    break;
                default:
                    throw new JsonException($"No se sabe migrar la versión {version} del formato de las fichas.");
            }
        }

        if (character["abilities"] is not JsonObject abilities)
        {
            abilities = [];
            character["abilities"] = abilities;
        }

        foreach (var ability in Enum.GetNames<Ability>())
        {
            if (abilities[ability] is not JsonObject)
            {
                abilities[ability] = new JsonObject();
            }
        }

        if (character["skills"] is JsonArray skills)
        {
            foreach (var skill in skills.OfType<JsonObject>())
            {
                if (skill["improvements"] is not JsonArray)
                {
                    skill["improvements"] = new JsonArray();
                }
            }
        }
    }

    private static JsonObject ToObject(JsonElement root) =>
        JsonNode.Parse(root.GetRawText())?.AsObject() ?? throw new JsonException("Los datos guardados no son un objeto.");

    private static StoredRead<T> Read<T>(string? json, T empty, Func<T, bool> isComplete, int currentVersion, Func<JsonElement, int, JsonElement> migrate, bool isFile = false)
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
                return new StoredRead<T>(null, isFile ? "El archivo no indica su versión de formato." : "Los datos guardados no indican su versión de formato.");
            }

            if (version > currentVersion)
            {
                return new StoredRead<T>(null, $"{(isFile ? "El archivo es" : "Los datos guardados son")} de una versión más reciente de la aplicación ({version}); esta entiende hasta la {currentVersion}.");
            }

            var migrated = migrate(document.RootElement, version);
            var value = migrated.Deserialize<T>(GameDataJson.Options);
            return value is not null && isComplete(value)
                ? new StoredRead<T>(value, null)
                : new StoredRead<T>(null, isFile ? "El archivo está incompleto." : "Los datos guardados están incompletos.");
        }
        catch (JsonException exception)
        {
            return new StoredRead<T>(null, $"{(isFile ? "El archivo está dañado" : "Los datos guardados están dañados")}: {exception.Message}");
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
