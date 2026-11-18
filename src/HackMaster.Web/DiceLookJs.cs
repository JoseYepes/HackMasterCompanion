using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>
/// El aspecto de los dados tal como lo espera <c>dice3d.js</c>: enumeraciones como texto
/// (specs/006-dados-personalizables/contracts/dice3d-look.md).
/// </summary>
public static class DiceLookJs
{
    public static object From(DiceLook look) => new
    {
        material = look.Material.ToString(),
        color = look.Color,
        ink = look.Ink,
        effect = look.Effect.ToString(),
        sound = look.Sound,
        quality = look.Quality.ToString(),
    };
}
