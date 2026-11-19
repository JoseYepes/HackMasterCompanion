using Microsoft.JSInterop;

namespace HackMaster.Web;

/// <summary>
/// Guarda texto en IndexedDB con el módulo <c>js/characters.js</c> (F004, research §1). Como
/// <see cref="BrowserStore"/>: si el navegador no deja guardar, no falla, conserva los valores en
/// memoria mientras la página siga abierta y avisa con <see cref="AvailabilityChanged"/>.
/// </summary>
public sealed class IndexedStore(IJSRuntime js)
{
    /// <summary>Ruta del módulo, relativa a la base del sitio.</summary>
    public const string ModulePath = "./js/characters.js";

    private readonly Dictionary<string, string> _memory = new(StringComparer.Ordinal);
    private IJSObjectReference? _module;

    /// <summary>Falso desde que una lectura o escritura del navegador ha fallado.</summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>Se lanza cuando <see cref="IsAvailable"/> cambia.</summary>
    public event Action? AvailabilityChanged;

    /// <summary>Valor guardado con esa clave, o nulo; mientras el navegador deje, se lee de él (otra pestaña puede haber guardado).</summary>
    public async Task<string?> GetAsync(string key)
    {
        if (!IsAvailable)
        {
            return _memory.GetValueOrDefault(key);
        }

        try
        {
            var module = await ModuleAsync();
            return await module.InvokeAsync<string?>("get", key);
        }
        catch (JSException)
        {
            MarkUnavailable();
            return _memory.GetValueOrDefault(key);
        }
    }

    /// <summary>Guarda el valor; si el navegador no deja, lo conserva en memoria y avisa.</summary>
    public async Task SetAsync(string key, string value)
    {
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
