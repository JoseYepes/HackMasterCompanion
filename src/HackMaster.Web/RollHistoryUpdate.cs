using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>
/// Cambia el historial del tirador guardado en el dispositivo (clave <c>hackmaster.rolls</c>). Lo usan
/// el tirador (F002) y las tiradas de habilidad desde la ficha (F005).
/// </summary>
public static class RollHistoryUpdate
{
    public const string StorageKey = "hackmaster.rolls";

    /// <summary>
    /// Aplica el cambio sobre el historial guardado, que se vuelve a leer justo antes para no borrar
    /// lo que haya guardado otra pestaña (SC-005 de F002). Si no hay nada guardado, se parte de
    /// <paramref name="current"/>. Si lo guardado no se puede leer, el cambio se devuelve pero no se
    /// escribe nada, para no perderlo (FR-019 de F002), y se devuelve el motivo.
    /// </summary>
    public static async Task<(RollHistoryFile History, string? Error)> ApplyAsync(
        BrowserStore store,
        RollHistoryFile current,
        Func<RollHistoryFile, RollHistoryFile> change)
    {
        var json = await store.GetAsync(StorageKey);
        var read = json is null ? new StoredRead<RollHistoryFile>(current, null) : StoredData.ReadRolls(json);
        if (read.Value is null)
        {
            return (change(current), read.Error);
        }

        var history = change(read.Value);
        await store.SetAsync(StorageKey, StoredData.Write(history));
        return (history, null);
    }
}
