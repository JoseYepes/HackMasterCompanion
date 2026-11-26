using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.Published;

/// <summary>
/// Prueba del sitio publicado (F013): se omite si no está definida <c>HACKMASTER_PUBLISHED_DIR</c> con la
/// carpeta <c>wwwroot</c> publicada y preparada con <c>tools/pages/apply-base-path.sh</c>.
/// </summary>
public sealed class PublishedFactAttribute : FactAttribute
{
    /// <summary>Variable de entorno con la carpeta publicada.</summary>
    public const string DirVariable = "HACKMASTER_PUBLISHED_DIR";

    /// <summary>Marca la prueba como omitida si falta la carpeta.</summary>
    public PublishedFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DirVariable)))
        {
            Skip = $"Prueba del sitio publicado: define {DirVariable} (ver specs/011-instalable-sin-conexion/quickstart.md).";
        }
    }
}

/// <summary>
/// Sirve una copia de la aplicación publicada bajo <c>/HackMasterCompanion/</c> como GitHub Pages
/// (research R6): tipos de contenido correctos y <c>404.html</c> con estado 404 para las direcciones
/// internas. Apunta cada petición que llega, para saber qué pasó por la red y qué no. Cada prueba usa
/// su propio sitio, que puede cambiar de versión sin afectar a las demás.
/// </summary>
public sealed class PublishedSite : IDisposable
{
    /// <summary>Ruta base del sitio, como en <c>pages.yml</c>.</summary>
    public const string BasePath = "/HackMasterCompanion/";

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".mjs"] = "text/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".webmanifest"] = "application/manifest+json; charset=utf-8",
        [".wasm"] = "application/wasm",
        [".dat"] = "application/octet-stream",
        [".png"] = "image/png",
        [".ico"] = "image/x-icon",
        [".svg"] = "image/svg+xml",
        [".woff2"] = "font/woff2",
        [".woff"] = "font/woff",
        [".ogg"] = "audio/ogg",
        [".mp3"] = "audio/mpeg",
        [".md"] = "text/markdown; charset=utf-8",
    };

    private readonly HttpListener _listener = new();
    private readonly string _root;
    private readonly Task _loop;

    /// <summary>Copia la carpeta publicada a una temporal y empieza a servirla.</summary>
    public PublishedSite()
    {
        var source = Environment.GetEnvironmentVariable(PublishedFactAttribute.DirVariable)
            ?? throw new InvalidOperationException($"Falta {PublishedFactAttribute.DirVariable}.");
        _root = Path.Combine(Path.GetTempPath(), "hackmaster-published-" + Guid.NewGuid().ToString("N"));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_root, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        var port = FreePort();
        Origin = new Uri($"http://localhost:{port}/");
        _listener.Prefixes.Add(Origin.ToString());
        _listener.Start();
        _loop = Task.Run(ServeAsync);
    }

    /// <summary>Origen del sitio, terminado en «/».</summary>
    public Uri Origin { get; }

    /// <summary>Dirección de la aplicación (con la ruta base).</summary>
    public Uri AppUri => new(Origin, BasePath);

    /// <summary>Rutas pedidas al servidor, en orden.</summary>
    public ConcurrentQueue<string> Requests { get; } = new();

    /// <summary>Versión de los archivos que guarda el trabajador de servicio.</summary>
    public string AssetsVersion =>
        System.Text.RegularExpressions.Regex.Match(File.ReadAllText(Path.Combine(_root, "service-worker-assets.js")), "\"version\": \"([^\"]+)\"").Groups[1].Value;

    /// <summary>
    /// Publica una «versión nueva»: cambia la versión de la lista de archivos y el trabajador, como
    /// haría una publicación con cambios (SC-005). Los archivos y sus resúmenes no cambian.
    /// </summary>
    public string PublishNewVersion()
    {
        var assets = Path.Combine(_root, "service-worker-assets.js");
        var old = AssetsVersion;
        var next = old + "v2";
        File.WriteAllText(assets, File.ReadAllText(assets).Replace($"\"version\": \"{old}\"", $"\"version\": \"{next}\"", StringComparison.Ordinal));
        File.AppendAllText(Path.Combine(_root, "service-worker.js"), $"\n// Versión de prueba {next}\n");
        return next;
    }

    /// <summary>Abre una página nueva, con su propio almacenamiento y trabajador de servicio.</summary>
    public static async Task<IPage> NewPageAsync(IBrowser browser, ViewportSize viewport, string? userAgent = null)
    {
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = viewport,
            Locale = "es-ES",
            UserAgent = userAgent,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });
        return await context.NewPageAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
        try
        {
            _loop.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // El bucle termina con una excepción al cerrar el servidor.
        }

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Carpeta temporal: si queda algo, no importa.
        }
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }

            _ = Task.Run(() => Respond(context));
        }
    }

    private void Respond(HttpListenerContext context)
    {
        var response = context.Response;
        try
        {
            var path = Uri.UnescapeDataString(context.Request.Url!.AbsolutePath);
            Requests.Enqueue(path);
            var file = Resolve(path);
            var status = 200;
            if (file is null)
            {
                file = Path.Combine(_root, "404.html");
                status = 404;
            }

            var bytes = File.ReadAllBytes(file);
            response.StatusCode = status;
            response.ContentType = ContentTypes.GetValueOrDefault(Path.GetExtension(file), "application/octet-stream");
            response.Headers["Cache-Control"] = "no-cache";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes);
        }
        catch (Exception e) when (e is IOException or HttpListenerException)
        {
            // El navegador cortó la petición.
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch (Exception e) when (e is IOException or HttpListenerException or ObjectDisposedException)
            {
                // Ya cerrada.
            }
        }
    }

    private string? Resolve(string path)
    {
        if (!path.StartsWith(BasePath, StringComparison.Ordinal))
        {
            return null;
        }

        var relative = path[BasePath.Length..];
        if (relative.Length == 0)
        {
            relative = "index.html";
        }

        var full = Path.GetFullPath(Path.Combine(_root, relative));
        return full.StartsWith(_root, StringComparison.Ordinal) && File.Exists(full) ? full : null;
    }
}
