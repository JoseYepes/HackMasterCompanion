namespace HackMaster.Api;

/// <summary>
/// Ajustes de la API. Se leen de los ajustes de la aplicación en Azure (nunca del repositorio):
/// <c>AUTH_AUTHORITY</c>, <c>AUTH_AUDIENCE</c>, <c>ALLOWED_ORIGINS</c>, <c>ADMIN_EMAILS</c>,
/// <c>COSMOS_ENDPOINT</c>, <c>COSMOS_KEY</c> y <c>COSMOS_DATABASE</c>.
/// </summary>
public sealed record ApiSettings
{
    public IReadOnlyList<string> AdminEmails { get; init; } = [];

    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    public static IReadOnlyList<string> SplitList(string? value) =>
        (value ?? "").Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
