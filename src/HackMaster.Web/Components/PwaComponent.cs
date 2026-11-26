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
            _subscription = await JS.InvokeAsync<int>("hackmasterPwa.subscribe", _reference);
        }
        catch (JSException)
        {
            // Sin js/pwa.js no hay avisos.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_subscription is int id)
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

        _reference?.Dispose();
        GC.SuppressFinalize(this);
    }
}
