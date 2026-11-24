using System.Text.Json;

namespace HackMaster.Domain.Sync;

/// <summary>Tipos de documento que se guardan en la cuenta (data-model.md de F006).</summary>
public static class SyncKinds
{
    public const string Character = "character";
    public const string Suite = "suite";
    public const string Rolls = "rolls";
    public const string Theme = "theme";

    /// <summary>Identificador del documento único de los tipos que solo tienen uno.</summary>
    public const string SingleItem = "main";

    /// <summary>Versión del modo de pantalla guardado en la cuenta.</summary>
    public const int ThemeVersion = 1;

    /// <summary>Versión que escribe y entiende esta aplicación para cada tipo, o null si no lo conoce.</summary>
    public static int? CurrentVersion(string kind) => kind switch
    {
        Character => StoredData.CharactersVersion,
        Suite => StoredData.SuitesVersion,
        Rolls => StoredData.RollsVersion,
        Theme => ThemeVersion,
        _ => null,
    };
}

/// <summary>
/// Un elemento guardado en la cuenta: una ficha, un conjunto, el historial de tiradas o el modo de
/// pantalla. <see cref="Payload"/> es el JSON del elemento tal como se guarda en el dispositivo;
/// <see cref="ETag"/> es la versión de la nube en la que se basa (null si nunca ha estado allí).
/// </summary>
public sealed record SyncDoc(
    string Id,
    string Kind,
    int SchemaVersion,
    DateTimeOffset UpdatedAt,
    bool Deleted,
    string? Payload,
    string? ETag = null)
{
    public static string MakeId(string kind, string itemId) => $"{kind}:{itemId}";

    /// <summary>Identificador del elemento dentro de su tipo (el <c>Id</c> de la ficha, el <c>Slug</c> del conjunto…).</summary>
    public string ItemId => Id.StartsWith(Kind + ":", StringComparison.Ordinal) ? Id[(Kind.Length + 1)..] : Id;

    /// <summary>Nombre del elemento para los avisos.</summary>
    public string Label => Kind switch
    {
        SyncKinds.Rolls => "el historial de tiradas",
        SyncKinds.Theme => "el modo de pantalla",
        _ => PayloadName() ?? ItemId,
    };

    private string? PayloadName()
    {
        if (Deleted || string.IsNullOrWhiteSpace(Payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(Payload);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.NameEquals("name") || property.NameEquals("Name"))
                {
                    var name = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
                    return string.IsNullOrWhiteSpace(name) ? null : name;
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
