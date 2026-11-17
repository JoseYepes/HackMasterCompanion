namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Prueba de interfaz en un navegador real. Se omite si no está definida la variable de entorno
/// <c>HACKMASTER_E2E_URL</c> con la dirección de la aplicación en marcha, de modo que
/// <c>dotnet test</c> sigue pasando en un equipo sin navegadores instalados.
/// </summary>
public sealed class E2EFactAttribute : FactAttribute
{
    /// <summary>Nombre de la variable de entorno con la dirección de la aplicación.</summary>
    public const string UrlVariable = "HACKMASTER_E2E_URL";

    /// <summary>Marca la prueba como omitida si falta la dirección.</summary>
    public E2EFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(UrlVariable)))
        {
            Skip = $"Prueba de interfaz: define {UrlVariable} con la dirección de la aplicación (ver tests/README.md).";
        }
    }
}
