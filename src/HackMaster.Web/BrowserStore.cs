using HackMaster.Web.Account;
using Microsoft.JSInterop;

namespace HackMaster.Web;

/// <summary>
/// Guarda texto en el <c>localStorage</c> del navegador. Si el navegador no deja guardar (modo
/// privado, espacio lleno), no falla: conserva los valores en memoria mientras la página siga
/// abierta y avisa con <see cref="AvailabilityChanged"/> (FR-019). Con sesión, cada cuenta usa sus
/// propias claves y los cambios se anuncian con <see cref="Written"/> para subirlos (F006).
/// </summary>
public sealed class BrowserStore(IJSRuntime js, AccountSession? session = null)
{
    private readonly Dictionary<string, string> _memory = new(StringComparer.Ordinal);

    /// <summary>Falso desde que una lectura o escritura del navegador ha fallado.</summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>Se lanza cuando <see cref="IsAvailable"/> cambia.</summary>
    public event Action? AvailabilityChanged;

    /// <summary>Se lanza tras guardar, con sesión, una clave que se lleva a la cuenta (clave base y valor).</summary>
    public event Func<string, string, Task>? Written;

    /// <summary>
    /// Valor guardado con esa clave, o nulo si no hay ninguno. Mientras el navegador deje guardar se
    /// lee siempre de él, para ver lo que haya guardado otra pestaña; si no, de la memoria.
    /// </summary>
    public async Task<string?> GetAsync(string key)
    {
        key = session?.Key(key) ?? key;
        if (!IsAvailable)
        {
            return _memory.GetValueOrDefault(key);
        }

        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
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
