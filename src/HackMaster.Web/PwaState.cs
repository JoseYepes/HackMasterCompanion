namespace HackMaster.Web;

/// <summary>
/// Lo que cuenta <c>js/pwa.js</c> de la aplicación instalable (F013, contracts/pwa.md): versión nueva
/// esperando, aviso de instalar del navegador, iPhone o iPad y si está abierta como aplicación.
/// </summary>
public sealed record PwaState(bool UpdateReady, bool CanInstall, bool IsIos, bool IsStandalone)
{
    /// <summary>Estado antes de que el script diga nada: no se muestra ningún aviso.</summary>
    public static PwaState None { get; } = new(false, false, false, false);
}
