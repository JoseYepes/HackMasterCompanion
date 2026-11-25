using HackMaster.Domain;
using HackMaster.Web.Account;
using Microsoft.JSInterop;

namespace HackMaster.Web;

/// <summary>Archivo de ficha leído para importar: la ficha, o el motivo por el que no se puede importar.</summary>
public sealed record CharacterImport(Character? Character, string? Error, bool AlreadyExists);

/// <summary>Qué se espera al guardar una ficha, comprobado con lo guardado justo antes de escribir.</summary>
public enum SaveMode
{
    /// <summary>Ficha nueva: si ya hay una con su identificador, no se guarda (<see cref="PlayerCharacters.AlreadyExists"/>).</summary>
    Create,

    /// <summary>Ficha que se editaba: si ya no está (se borró en otra pestaña), no se guarda.</summary>
    Update,

    /// <summary>Se crea o se sustituye, sin comprobar (importar eligiendo «Sustituir»).</summary>
    Upsert,
}

/// <summary>
/// Fichas de PJ guardadas en el dispositivo (clave <c>hackmaster.characters</c> en IndexedDB; F004).
/// Lo guardado se vuelve a leer al abrir cada página y justo antes de escribir, para ver y no borrar
/// lo que haya cambiado otra pestaña; si lo guardado no se pudo leer no se escribe nada (FR-010).
/// </summary>
public sealed class PlayerCharacters(IndexedStore store, AccountSession? session = null)
{
    /// <summary>Motivo que devuelve guardar una ficha nueva cuyo identificador ya existe.</summary>
    public const string AlreadyExists = "Ya hay una ficha con el mismo identificador en este dispositivo.";

    private const string StorageKey = "hackmaster.characters";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CharactersFile _file = CharactersFile.Empty;
    private string? _account;

    /// <summary>Motivo por el que no se pudieron leer las fichas guardadas; mientras exista no se guarda nada.</summary>
    public string? LoadError { get; private set; }

    /// <summary>Las fichas, por nombre (sin importar acentos ni mayúsculas); las de igual nombre, por fecha de creación.</summary>
    public IReadOnlyList<Character> Characters =>
        [.. _file.Characters
            .OrderBy(character => TextNormalizer.Normalize(character.Name), StringComparer.Ordinal)
            .ThenBy(character => character.CreatedAt)
            .ThenBy(character => character.Id, StringComparer.Ordinal)];

    public Character? Find(string id) => _file.Characters.FirstOrDefault(character => character.Id == id);

    /// <summary>Lee las fichas guardadas; cada vez, para ver lo que haya guardado otra pestaña.</summary>
    public async Task LoadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await ReadAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Guarda una ficha según <paramref name="mode"/>. Devuelve el motivo si no se guarda.</summary>
    public async Task<string?> SaveAsync(Character character, SaveMode mode = SaveMode.Upsert)
    {
        var problem = character.Problems().FirstOrDefault();
        return problem ?? await ChangeAsync(characters =>
        {
            var exists = characters.Any(other => other.Id == character.Id);
            return (mode, exists) switch
            {
                (SaveMode.Create, true) => (null, AlreadyExists),
                (SaveMode.Update, false) => (null, "Esta ficha se borró en otra pestaña o ventana. Si quieres conservarla, expórtala a un archivo e impórtala."),
                _ => ([.. characters.Where(other => other.Id != character.Id), character], null),
            };
        });
    }

    /// <summary>Copia una ficha con otro identificador (FR-005). Devuelve la copia, o el motivo si no se pudo.</summary>
    public async Task<(Character? Copy, string? Problem)> DuplicateAsync(string id, string newId, DateTimeOffset now)
    {
        await LoadAsync();
        if (LoadError is not null)
        {
            return (null, $"No se pudieron leer las fichas guardadas ({LoadError}).");
        }

        if (Find(id) is not { } original)
        {
            return (null, "La ficha ya no está en este dispositivo.");
        }

        var copy = original.Duplicate(newId, now);
        var problem = await SaveAsync(copy, SaveMode.Create);
        return problem is null ? (copy, null) : (null, problem);
    }

    /// <summary>Borra una ficha (FR-006). Devuelve el motivo si no se pudo.</summary>
    public Task<string?> DeleteAsync(string id) =>
        ChangeAsync(characters => ([.. characters.Where(character => character.Id != id)], null));

    /// <summary>Descarga el archivo de la ficha, con su nombre pasado a slug (FR-007). Devuelve el motivo si falla.</summary>
    public async Task<string?> ExportAsync(Character character)
    {
        var slug = TextNormalizer.ToSlug(character.Name);
        try
        {
            await store.DownloadAsync($"{(slug.Length == 0 ? "ficha" : slug)}.hackmaster.json", StoredData.WriteCharacterFile(character));
            return null;
        }
        catch (JSException exception)
        {
            return $"No se pudo exportar la ficha: {exception.Message}";
        }
    }

    /// <summary>
    /// Lee un archivo para importarlo y dice si la ficha ya está en el dispositivo, mirando lo
    /// guardado en ese momento (FR-008, FR-009).
    /// </summary>
    public async Task<CharacterImport> ReadImportAsync(string? json)
    {
        var read = StoredData.ReadCharacterFile(json);
        if (read.Value is not { } file)
        {
            return new CharacterImport(null, read.Error, false);
        }

        await LoadAsync();
        return new CharacterImport(file.Character, null, Find(file.Character.Id) is not null);
    }

    /// <summary>
    /// Importa la ficha como copia nueva, sustituyendo la del mismo identificador
    /// (<paramref name="replace"/>) o solo si no existe; en ese caso, si otra pestaña la añadió
    /// entretanto, devuelve <see cref="AlreadyExists"/> para que se pregunte. Devuelve el motivo si
    /// no se guarda.
    /// </summary>
    public Task<string?> ImportAsync(Character character, bool asCopy, string newId, DateTimeOffset now, bool replace) =>
        asCopy ? SaveAsync(character.Duplicate(newId, now), SaveMode.Create)
        : SaveAsync(character, replace ? SaveMode.Upsert : SaveMode.Create);

    private async Task<string?> ChangeAsync(Func<IReadOnlyList<Character>, (IReadOnlyList<Character>? Changed, string? Problem)> change)
    {
        if (session is not null)
        {
            await session.DataGate.WaitAsync();
        }

        try
        {
            return await ChangeLockedAsync(change);
        }
        finally
        {
            session?.DataGate.Release();
        }
    }

    private async Task<string?> ChangeLockedAsync(Func<IReadOnlyList<Character>, (IReadOnlyList<Character>? Changed, string? Problem)> change)
    {
        await _gate.WaitAsync();
        try
        {
            await ReadAsync();
            if (LoadError is not null)
            {
                return $"No se guarda para no borrar las fichas que no se pudieron leer ({LoadError}).";
            }

            var (changed, problem) = change(_file.Characters);
            if (changed is null)
            {
                return problem;
            }

            _file = _file with { Characters = changed };
            await store.SetAsync(StorageKey, StoredData.Write(_file));
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Lee lo guardado. Si no hay nada guardado (primera vez, o datos del sitio borrados con la
    /// página abierta) se conserva lo que hay en memoria, para volver a escribirlo.
    /// </summary>
    private async Task ReadAsync()
    {
        if (session?.AccountId != _account)
        {
            // Otra cuenta, o sin sesión: lo que había en memoria era de la anterior (F006 FR-009).
            _account = session?.AccountId;
            _file = CharactersFile.Empty;
        }

        var stored = await store.GetAsync(StorageKey);
        if (stored.Failed)
        {
            LoadError = "el navegador no dejó leer las fichas guardadas";
            return;
        }

        if (stored.Value is null)
        {
            LoadError = null;
            return;
        }

        var read = StoredData.ReadCharacters(stored.Value);
        _file = read.Value ?? _file;
        LoadError = read.Error;
    }
}
