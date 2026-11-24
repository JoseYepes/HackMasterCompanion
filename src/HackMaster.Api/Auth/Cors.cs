using System.Net.Http.Headers;

namespace HackMaster.Api.Auth;

/// <summary>
/// CORS de la API (research.md R2): la interfaz vive en GitHub Pages, otro origen. Solo se admiten
/// los orígenes de <c>ALLOWED_ORIGINS</c> y <c>http://localhost</c> con cualquier puerto.
/// </summary>
public sealed class Cors(IReadOnlyList<string> allowedOrigins)
{
    public bool IsAllowed(string? origin)
    {
        if (string.IsNullOrEmpty(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && uri.Host == "localhost")
        {
            return true;
        }

        return allowedOrigins.Any(a => string.Equals(a.TrimEnd('/'), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }

    public void Apply(HttpHeaders headers, string? origin)
    {
        if (!IsAllowed(origin))
        {
            return;
        }

        headers.Add("Access-Control-Allow-Origin", origin!);
        headers.Add("Vary", "Origin");
        headers.Add("Access-Control-Allow-Methods", "GET, PUT, DELETE, OPTIONS");
        headers.Add("Access-Control-Allow-Headers", "Authorization, Content-Type, If-Match, If-None-Match");
        headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
        headers.Add("Access-Control-Max-Age", "600");
    }
}
