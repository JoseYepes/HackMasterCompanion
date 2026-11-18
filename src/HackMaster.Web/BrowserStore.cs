using Microsoft.JSInterop;

namespace HackMaster.Web;

/// <summary>
/// Guarda texto en el <c>localStorage</c> del navegador. Si el navegador no deja guardar (modo
/// privado, espacio lleno), no falla: conserva los valores en memoria mientras la página siga
/// abierta y avisa con <see cref="AvailabilityChanged"/> (FR-019).
/// </summary>
public sealed class BrowserStore(IJSRuntime js)
{
    private readonly Dictionary<string, string> _memory = new(StringComparer.Ordinal);

    /// <summary>Falso desde que una lectura o escritura del navegador ha fallado.</summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>Se lanza cuando <see cref="IsAvailable"/> cambia.</summary>
    public event Action? AvailabilityChanged;

    /// <summary>Valor guardado con esa clave, o nulo si no hay ninguno.</summary>
    public async Task<string?> GetAsync(string key)
    {
        if (_memory.TryGetValue(key, out var remembered))
        {
            return remembered;
        }

        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (JSException)
        {
            MarkUnavailable();
            return null;
        }
    }

    /// <summary>Guarda el valor; si el navegador no deja, lo conserva en memoria y avisa.</summary>
    public async Task SetAsync(string key, string value)
    {
        _memory[key] = value;
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (JSException)
        {
            MarkUnavailable();
        }
    }

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
