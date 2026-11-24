using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HackMaster.Web.Tests.Account;

/// <summary>
/// API de cuentas falsa en memoria (contracts/api.md), para probar la sincronización sin red. Se
/// puede dejar sin conexión (<see cref="Offline"/>) o caída (<see cref="Down"/>).
/// </summary>
public sealed class FakeApi : HttpMessageHandler
{
    private int _etag;

    public Dictionary<string, JsonObject> Docs { get; } = new(StringComparer.Ordinal);

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string?> Bodies { get; } = [];

    public bool Offline { get; set; }

    public bool Down { get; set; }

    public bool Invited { get; set; } = true;

    public bool IsAdmin { get; set; }

    public long ServerTime { get; set; } = 1000;

    public string? LastAuthorization { get; private set; }

    public HttpClient Client() => new(this) { BaseAddress = new Uri("https://api.pruebas.example/") };

    public string NextETag() => $"e{++_etag}";

    /// <summary>Guarda un documento como si viniera de otro dispositivo.</summary>
    public void Seed(string id, string kind, int schemaVersion, DateTimeOffset updatedAt, string? payload, bool deleted = false)
    {
        Docs[id] = new JsonObject
        {
            ["id"] = id,
            ["kind"] = kind,
            ["schemaVersion"] = schemaVersion,
            ["updatedAt"] = updatedAt.ToString("O"),
            ["deleted"] = deleted,
            ["payload"] = payload,
            ["etag"] = NextETag(),
            ["ts"] = ++ServerTime,
        };
    }

    public IEnumerable<HttpRequestMessage> Puts => Requests.Where(r => r.Method == HttpMethod.Put && r.RequestUri!.AbsolutePath.StartsWith("/api/data/", StringComparison.Ordinal));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Bodies.Add(body);
        LastAuthorization = request.Headers.Authorization?.ToString();
        if (Offline)
        {
            throw new HttpRequestException("Sin conexión");
        }

        if (Down)
        {
            return Json(HttpStatusCode.ServiceUnavailable, new JsonObject { ["error"] = "unavailable" });
        }

        if (!Invited)
        {
            return Json(HttpStatusCode.Forbidden, new JsonObject { ["error"] = "not-invited" });
        }

        var path = request.RequestUri!.AbsolutePath;
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
        switch (request.Method.Method, path)
        {
            case ("GET", "/api/me"):
                return Json(HttpStatusCode.OK, new JsonObject { ["name"] = "Ana", ["email"] = "ana@pruebas.example", ["isAdmin"] = IsAdmin });
            case ("GET", "/api/data"):
                var since = long.TryParse(query["since"], out var s) ? s : 0;
                var items = new JsonArray([.. Docs.Values.Where(d => (long)d["ts"]! >= since).Select(Public)]);
                return Json(HttpStatusCode.OK, new JsonObject { ["items"] = items, ["serverTime"] = ++ServerTime });
            case ("GET", "/api/export"):
                return Json(HttpStatusCode.OK, new JsonObject { ["schemaVersion"] = 1, ["items"] = new JsonArray([.. Docs.Values.Select(Public)]) });
            case ("DELETE", "/api/account"):
                Docs.Clear();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            case ("GET", "/api/invitations"):
                return Json(HttpStatusCode.OK, new JsonArray(new JsonObject { ["email"] = "bo@pruebas.example", ["invitedAt"] = "2026-11-24T10:00:00Z" }));
        }

        if (request.Method == HttpMethod.Put && path.StartsWith("/api/data/", StringComparison.Ordinal))
        {
            var id = Uri.UnescapeDataString(path["/api/data/".Length..]);
            var doc = JsonNode.Parse(body!)!.AsObject();
            var ifMatch = request.Headers.TryGetValues("If-Match", out var match) ? match.Single() : null;
            var create = request.Headers.TryGetValues("If-None-Match", out var none) && none.Single() == "*";
            Docs.TryGetValue(id, out var current);
            if ((create && current is not null) || (!create && current?["etag"]?.GetValue<string>() != ifMatch))
            {
                return current is null ? Json(HttpStatusCode.Conflict, new JsonObject { ["error"] = "conflict" }) : Json(HttpStatusCode.Conflict, Public(current));
            }

            doc["etag"] = NextETag();
            doc["ts"] = ++ServerTime;
            Docs[id] = doc;
            return Json(HttpStatusCode.OK, Public(doc));
        }

        if (path.StartsWith("/api/invitations/", StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static JsonObject Public(JsonObject doc)
    {
        var copy = doc.DeepClone().AsObject();
        copy.Remove("ts");
        return copy;
    }

    private static HttpResponseMessage Json(HttpStatusCode status, JsonNode body) =>
        new(status) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };

    public static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
