using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HackMaster.Web.Components;

/// <summary>
/// Componente que escucha a <c>js/pwa.js</c> (F013, contracts/pwa.md). Lo usan el aviso de versión
/// nueva y el botón «Instalar». Sin el script (pruebas, navegador antiguo) no muestra nada.
/// </summary>
public abstract class PwaComponent : ComponentBase, IAsyncDisposable
{
    private DotNetObjectReference<PwaComponent>? _reference;
    private int? _subscription;
    private bool _disposed;

    /// <summary>Interoperabilidad con el navegador.</summary>
    [Inject]
    protected IJSRuntime JS { get; set; } = default!;

    /// <summary>Último estado recibido.</summary>
    protected PwaState Pwa { get; private set; } = PwaState.None;

    /// <summary>El script avisa de un cambio.</summary>
    [JSInvokable]
    public Task OnPwaState(PwaState state)
    {
        Pwa = state;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _reference = DotNetObjectReference.Create(this);
        try
        {
            var id = await JS.InvokeAsync<int>("hackmasterPwa.subscribe", _reference);
            if (_disposed)
            {
                // Se quitó mientras el script respondía: se deja de escuchar ya.
                await UnsubscribeAsync(id);
                return;
            }

            _subscription = id;
        }
        catch (JSException)
        {
            // Sin js/pwa.js no hay avisos.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_subscription is int id)
        {
            await UnsubscribeAsync(id);
        }

        _reference?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task UnsubscribeAsync(int id)
    {
        try
        {
            await JS.InvokeVoidAsync("hackmasterPwa.unsubscribe", id);
        }
        catch (JSDisconnectedException)
        {
            // La página se está cerrando.
        }
        catch (JSException)
        {
            // Sin script no hay nada que quitar.
        }
    }
}
