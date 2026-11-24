using System.Net;
using HackMaster.Api.Auth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace HackMaster.Api.Functions;

/// <summary>
/// Rutas HTTP de la API (contracts/api.md). Solo traducen la petición a <see cref="DataApi"/> y
/// añaden CORS; la lógica y sus tests están en <see cref="DataApi"/>.
/// </summary>
public sealed class HttpFunctions(DataApi api, Cors cors)
{

    [Function("Me")]
    public Task<HttpResponseData> Me([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "me")] HttpRequestData req) =>
        HandleAsync(req, () => api.MeAsync(Auth(req), req.FunctionContext.CancellationToken));

    [Function("Data")]
    public Task<HttpResponseData> Data([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "data")] HttpRequestData req) =>
        HandleAsync(req, () =>
        {
            var since = long.TryParse(Query(req, "since"), out var value) ? value : (long?)null;
            return api.GetDataAsync(Auth(req), since, req.FunctionContext.CancellationToken);
        });

    [Function("PutData")]
    public Task<HttpResponseData> PutData([HttpTrigger(AuthorizationLevel.Anonymous, "put", "options", Route = "data/{id}")] HttpRequestData req, string id) =>
        HandleAsync(req, async () =>
        {
            var body = await req.ReadAsStringAsync();
            return await api.PutAsync(Auth(req), Uri.UnescapeDataString(id), Header(req, "If-Match"), Header(req, "If-None-Match"), body, req.FunctionContext.CancellationToken);
        });

    [Function("Export")]
    public Task<HttpResponseData> Export([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "export")] HttpRequestData req) =>
        HandleAsync(req, () => api.ExportAsync(Auth(req), req.FunctionContext.CancellationToken));

    [Function("DeleteAccount")]
    public Task<HttpResponseData> DeleteAccount([HttpTrigger(AuthorizationLevel.Anonymous, "delete", "options", Route = "account")] HttpRequestData req) =>
        HandleAsync(req, () => api.DeleteAccountAsync(Auth(req), req.FunctionContext.CancellationToken));

    [Function("Invitations")]
    public Task<HttpResponseData> Invitations([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "invitations")] HttpRequestData req) =>
        HandleAsync(req, () => api.ListInvitationsAsync(Auth(req), req.FunctionContext.CancellationToken));

    [Function("Invitation")]
    public Task<HttpResponseData> Invitation([HttpTrigger(AuthorizationLevel.Anonymous, "put", "delete", "options", Route = "invitations/{email}")] HttpRequestData req, string email) =>
        HandleAsync(req, () =>
        {
            var address = Uri.UnescapeDataString(email);
            var ct = req.FunctionContext.CancellationToken;
            return req.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                ? api.InviteAsync(Auth(req), address, ct)
                : api.UninviteAsync(Auth(req), address, ct);
        });

    private async Task<HttpResponseData> HandleAsync(HttpRequestData req, Func<Task<ApiResult>> action)
    {
        var origin = Header(req, "Origin");
        if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            var preflight = req.CreateResponse(cors.IsAllowed(origin) ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
            cors.Apply(preflight.Headers, origin);
            return preflight;
        }

        var result = await action();
        var response = req.CreateResponse((HttpStatusCode)result.Status);
        cors.Apply(response.Headers, origin);
        if (result.Json is not null)
        {
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            if (result.FileName is not null)
            {
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
            }

            await response.WriteStringAsync(result.Json);
        }

        return response;
    }

    private static string? Auth(HttpRequestData req) => Header(req, "Authorization");

    private static string? Header(HttpRequestData req, string name) =>
        req.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    private static string? Query(HttpRequestData req, string name) =>
        System.Web.HttpUtility.ParseQueryString(req.Url.Query)[name];
}
