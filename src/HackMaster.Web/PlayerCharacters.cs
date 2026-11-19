using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>Archivo de ficha leído para importar: la ficha, o el motivo por el que no se puede importar.</summary>
public sealed record CharacterImport(Character? Character, string? Error, bool AlreadyExists);

/// <summary>
/// Fichas de PJ guardadas en el dispositivo (clave <c>hackmaster.characters</c> en IndexedDB; F004).
/// Mismo patrón que <see cref="PlayerSuites"/>: se relee antes de escribir para no borrar lo que
/// haya guardado otra pestaña, y si lo guardado no se pudo leer no se escribe nada (FR-010).
/// </summary>
public sealed class PlayerCharacters(IndexedStore store)
{
    private const string StorageKey = "hackmaster.characters";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CharactersFile _file = CharactersFile.Empty;
    private bool _loaded;

    /// <summary>Motivo por el que no se pudieron leer las fichas guardadas; mientras exista no se guarda nada.</summary>
    public string? LoadError { get; private set; }

    /// <summary>Las fichas, por nombre (sin importar acentos ni mayúsculas); las de igual nombre, por fecha de creación.</summary>
    public IReadOnlyList<Character> Characters =>
        [.. _file.Characters
            .OrderBy(character => TextNormalizer.Normalize(character.Name), StringComparer.Ordinal)
            .ThenBy(character => character.CreatedAt)
            .ThenBy(character => character.Id, StringComparer.Ordinal)];

    public Character? Find(string id) => _file.Characters.FirstOrDefault(character => character.Id == id);

    /// <summary>Lee las fichas guardadas la primera vez; las siguientes no hace nada.</summary>
    public async Task LoadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!_loaded)
            {
                await ReadAsync(keepIfMissing: false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Crea o sustituye (por identificador) una ficha. Devuelve el motivo si no se guarda.</summary>
    public async Task<string?> SaveAsync(Character character)
    {
        var problem = character.Problems().FirstOrDefault();
        return problem ?? await ChangeAsync(characters => [.. characters.Where(other => other.Id != character.Id), character]);
    }

    /// <summary>Copia una ficha con otro identificador (FR-005); nulo si no existe o no se pudo guardar.</summary>
    public async Task<Character?> DuplicateAsync(string id, string newId, DateTimeOffset now)
    {
        await LoadAsync();
        if (Find(id) is not { } original)
        {
            return null;
        }

        var copy = original.Duplicate(newId, now);
        return await SaveAsync(copy) is null ? copy : null;
    }

    /// <summary>Borra una ficha (FR-006). Devuelve el motivo si no se pudo.</summary>
    public Task<string?> DeleteAsync(string id) =>
        ChangeAsync(characters => [.. characters.Where(character => character.Id != id)]);

    /// <summary>Descarga el archivo de la ficha, con su nombre pasado a slug (FR-007).</summary>
    public Task ExportAsync(Character character)
    {
        var slug = TextNormalizer.ToSlug(character.Name);
        return store.DownloadAsync($"{(slug.Length == 0 ? "ficha" : slug)}.hackmaster.json", StoredData.WriteCharacterFile(character));
    }

    /// <summary>Lee un archivo para importarlo y dice si la ficha ya está en el dispositivo (FR-008, FR-009).</summary>
    public CharacterImport ReadImport(string? json)
    {
        var read = StoredData.ReadCharacterFile(json);
        return read.Value is { } file
            ? new CharacterImport(file.Character, null, Find(file.Character.Id) is not null)
            : new CharacterImport(null, read.Error, false);
    }

    /// <summary>
    /// Importa la ficha tal cual (sustituye la del mismo identificador, si la hay) o como copia
    /// nueva. Devuelve el motivo si no se guarda.
    /// </summary>
    public Task<string?> ImportAsync(Character character, bool asCopy, string newId, DateTimeOffset now) =>
        SaveAsync(asCopy ? character.Duplicate(newId, now) : character);

    private async Task<string?> ChangeAsync(Func<IReadOnlyList<Character>, IReadOnlyList<Character>> change)
    {
        await _gate.WaitAsync();
        try
        {
            await ReadAsync(keepIfMissing: true);
            if (LoadError is not null)
            {
                return $"No se guarda para no borrar las fichas que no se pudieron leer ({LoadError}).";
            }

            _file = _file with { Characters = change(_file.Characters) };
            await store.SetAsync(StorageKey, StoredData.Write(_file));
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Lee lo guardado. Con <paramref name="keepIfMissing"/>, si no hay nada guardado (datos del
    /// sitio borrados) se conserva lo que hay en memoria para volver a escribirlo.
    /// </summary>
    private async Task ReadAsync(bool keepIfMissing)
    {
        var json = await store.GetAsync(StorageKey);
        _loaded = true;
        if (json is null && keepIfMissing)
        {
            return;
        }

        var read = StoredData.ReadCharacters(json);
        _file = read.Value ?? _file;
        LoadError = read.Error;
    }
}
