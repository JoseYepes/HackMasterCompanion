using System.Text.Json;
using System.Text.Json.Nodes;

namespace HackMaster.Domain.Sync;

/// <summary>
/// Paso entre los archivos que se guardan en el dispositivo y los documentos de la cuenta
/// (research.md R4): una ficha o un conjunto por documento; el historial y el modo de pantalla,
/// enteros. Los documentos antiguos se migran con <see cref="StoredData"/> al aplicarlos.
/// </summary>
public static class LocalDocs
{
    public const string CharactersKey = "hackmaster.characters";
    public const string SuitesKey = "hackmaster.suites";
    public const string RollsKey = "hackmaster.rolls";
    public const string ThemeKey = "hackmaster.theme";

    /// <summary>Claves del dispositivo que se guardan en la cuenta.</summary>
    public static IReadOnlyList<string> SyncedKeys { get; } = [CharactersKey, SuitesKey, RollsKey, ThemeKey];

    public static string? KindForKey(string baseKey) => baseKey switch
    {
        CharactersKey => SyncKinds.Character,
        SuitesKey => SyncKinds.Suite,
        RollsKey => SyncKinds.Rolls,
        ThemeKey => SyncKinds.Theme,
        _ => null,
    };

    public static string? KeyForKind(string kind) => kind switch
    {
        SyncKinds.Character => CharactersKey,
        SyncKinds.Suite => SuitesKey,
        SyncKinds.Rolls => RollsKey,
        SyncKinds.Theme => ThemeKey,
        _ => null,
    };

    /// <summary>
    /// Los documentos de un archivo del dispositivo. Sin nada guardado, ninguno; si no se puede
    /// leer, null (no se sube nada para no estropear la cuenta).
    /// </summary>
    public static IReadOnlyList<SyncDoc>? Split(string baseKey, string? json, DateTimeOffset now)
    {
        if (KindForKey(baseKey) is not { } kind)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        switch (kind)
        {
            case SyncKinds.Character:
                return StoredData.ReadCharacters(json).Value is { } characters
                    ? [.. characters.Characters.Select(c => Doc(kind, c.Id, StoredData.CharactersVersion, c.UpdatedAt, Serialize(c)))]
                    : null;
            case SyncKinds.Suite:
                return StoredData.ReadSuites(json).Value is { } suites
                    ? [.. suites.Suites.Select(s => Doc(kind, s.Slug, StoredData.SuitesVersion, s.UpdatedAt, Serialize(s)))]
                    : null;
            case SyncKinds.Rolls:
                return StoredData.ReadRolls(json).Value is { } rolls
                    ? [Doc(kind, SyncKinds.SingleItem, StoredData.RollsVersion, now, StoredData.Write(rolls))]
                    : null;
            default:
                return IsTheme(json) ? [Doc(kind, SyncKinds.SingleItem, SyncKinds.ThemeVersion, now, json)] : null;
        }
    }

    /// <summary>
    /// Lo que cambió en un archivo al guardarlo: elementos nuevos o cambiados, y marcas de borrado de
    /// los que ya no están, todos con la hora <paramref name="now"/>.
    /// </summary>
    public static IReadOnlyList<SyncDoc> Changes(string baseKey, string? oldJson, string? newJson, DateTimeOffset now)
    {
        if (Split(baseKey, newJson, now) is not { } after)
        {
            return [];
        }

        var before = (Split(baseKey, oldJson, now) ?? []).ToDictionary(d => d.Id, d => d.Payload, StringComparer.Ordinal);
        var changed = after
            .Where(d => !before.TryGetValue(d.Id, out var payload) || payload != d.Payload)
            .Select(d => d with { UpdatedAt = now });
        var afterIds = after.Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
        var removed = before.Keys
            .Where(id => !afterIds.Contains(id))
            .Select(id => new SyncDoc(id, KindForKey(baseKey)!, SyncKinds.CurrentVersion(KindForKey(baseKey)!)!.Value, now, true, null));
        return [.. changed, .. removed];
    }

    /// <summary>
    /// Aplica los documentos de la nube al archivo del dispositivo y devuelve el archivo nuevo. Los
    /// documentos que no se pueden leer se saltan; si el archivo del dispositivo no se puede leer,
    /// se devuelve tal cual, para no perderlo.
    /// </summary>
    public static string? Apply(string baseKey, string? json, IEnumerable<SyncDoc> docs)
    {
        var kind = KindForKey(baseKey);
        var mine = docs.Where(d => d.Kind == kind).ToList();
        if (kind is null || mine.Count == 0)
        {
            return json;
        }

        switch (kind)
        {
            case SyncKinds.Character:
            {
                if ((string.IsNullOrWhiteSpace(json) ? CharactersFile.Empty : StoredData.ReadCharacters(json).Value) is not { } file)
                {
                    return json;
                }

                var items = file.Characters.ToList();
                foreach (var doc in mine)
                {
                    var item = doc.Deleted ? null : StoredData.ReadCharacters(Wrap(doc, "characters")).Value?.Characters.SingleOrDefault();
                    Merge(items, doc, item, c => c.Id);
                }

                return StoredData.Write(file with { Characters = items });
            }

            case SyncKinds.Suite:
            {
                if ((string.IsNullOrWhiteSpace(json) ? PlayerSuitesFile.Empty : StoredData.ReadSuites(json).Value) is not { } file)
                {
                    return json;
                }

                var items = file.Suites.ToList();
                foreach (var doc in mine)
                {
                    var item = doc.Deleted ? null : StoredData.ReadSuites(Wrap(doc, "suites")).Value?.Suites.SingleOrDefault();
                    Merge(items, doc, item, s => s.Slug);
                }

                return StoredData.Write(file with { Suites = items });
            }

            case SyncKinds.Rolls:
            {
                var last = mine[^1];
                return !last.Deleted && StoredData.ReadRolls(last.Payload).Value is { } rolls ? StoredData.Write(rolls) : json;
            }

            default:
            {
                var last = mine[^1];
                return !last.Deleted && IsTheme(last.Payload) ? last.Payload : json;
            }
        }
    }

    private static void Merge<T>(List<T> items, SyncDoc doc, T? item, Func<T, string> id)
        where T : class
    {
        var index = items.FindIndex(i => id(i) == doc.ItemId);
        if (doc.Deleted)
        {
            if (index >= 0)
            {
                items.RemoveAt(index);
            }
        }
        else if (item is not null && id(item) == doc.ItemId)
        {
            if (index >= 0)
            {
                items[index] = item;
            }
            else
            {
                items.Add(item);
            }
        }
    }

    /// <summary>Un elemento suelto dentro de un archivo de su versión, para leerlo y migrarlo como siempre.</summary>
    private static string Wrap(SyncDoc doc, string listName)
    {
        try
        {
            var item = JsonNode.Parse(doc.Payload ?? "");
            return new JsonObject { ["schemaVersion"] = doc.SchemaVersion, [listName] = new JsonArray(item) }.ToJsonString();
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static bool IsTheme(string? json)
    {
        try
        {
            var node = JsonNode.Parse(json ?? "");
            return node?["schemaVersion"]?.GetValue<int>() == SyncKinds.ThemeVersion
                && node["mode"]?.GetValue<string>() is "light" or "dark";
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return false;
        }
    }

    private static SyncDoc Doc(string kind, string itemId, int version, DateTimeOffset updatedAt, string payload) =>
        new(SyncDoc.MakeId(kind, itemId), kind, version, updatedAt, false, payload);

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, GameDataJson.Options);
}
