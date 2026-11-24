using HackMaster.Web.Account;
using Microsoft.JSInterop;

namespace HackMaster.Web;

/// <summary>Texto leído del dispositivo: el valor (nulo si no hay nada) o que el navegador no dejó leerlo.</summary>
public sealed record StoredText(string? Value, bool Failed);

/// <summary>
/// Guarda texto en IndexedDB con el módulo <c>js/characters.js</c> (F004, research §1). Como
/// <see cref="BrowserStore"/>: si el navegador no deja guardar, no falla, conserva los valores en
/// memoria mientras la página siga abierta y avisa con <see cref="AvailabilityChanged"/>.
/// </summary>
public sealed class IndexedStore(IJSRuntime js, AccountSession? session = null)
{
    /// <summary>Ruta del módulo, relativa a la base del sitio.</summary>
    public const string ModulePath = "./js/characters.js";

    private readonly Dictionary<string, string> _memory = new(StringComparer.Ordinal);
    private IJSObjectReference? _module;

    /// <summary>Falso desde que una lectura o escritura del navegador ha fallado.</summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>Se lanza cuando <see cref="IsAvailable"/> cambia.</summary>
    public event Action? AvailabilityChanged;

    /// <summary>Se lanza tras guardar, con sesión, una clave que se lleva a la cuenta (clave base y valor; F006).</summary>
    public event Func<string, string, Task>? Written;

    /// <summary>
    /// Valor guardado con esa clave; mientras el navegador deje, se lee de él (otra pestaña puede
    /// haber guardado). Si el navegador no deja leer y en memoria no hay nada de esa clave, el
    /// resultado lo dice (<see cref="StoredText.Failed"/>), para no confundirlo con «no hay nada».
    /// </summary>
    public async Task<StoredText> GetAsync(string key)
    {
        key = session?.Key(key) ?? key;
        if (IsAvailable)
        {
            try
            {
                var module = await ModuleAsync();
                return new StoredText(await module.InvokeAsync<string?>("get", key), Failed: false);
            }
            catch (JSException)
            {
                MarkUnavailable();
            }
        }

        return _memory.TryGetValue(key, out var value) ? new StoredText(value, Failed: false) : new StoredText(null, Failed: true);
    }

    /// <summary>Guarda el valor; si el navegador no deja, lo conserva en memoria y avisa.</summary>
    public async Task SetAsync(string key, string value)
    {
        await SetLocalAsync(key, value);
        if (session?.IsSynced(key) == true && Written is { } written)
        {
            await written(key, value);
        }
    }

    /// <summary>Guarda el valor sin anunciarlo: para lo que llega de la cuenta.</summary>
    public async Task SetLocalAsync(string key, string value)
    {
        key = session?.Key(key) ?? key;
        _memory[key] = value;
        if (!IsAvailable)
        {
            return;
        }

        try
        {
            var module = await ModuleAsync();
            await module.InvokeVoidAsync("set", key, value);
        }
        catch (JSException)
        {
            MarkUnavailable();
        }
    }

    /// <summary>Descarga el texto como un archivo con ese nombre.</summary>
    public async Task DownloadAsync(string fileName, string text)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("download", fileName, text);
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    private void MarkUnavailable()
    {
        if (!IsAvailable)
        {
            return;
        }

        IsAvailable = false;
        AvailabilityChanged?.Invoke();
    }
}
