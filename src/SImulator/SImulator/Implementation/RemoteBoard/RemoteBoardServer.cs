using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Utils.Web;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;

namespace SImulator.Implementation.RemoteBoard;

/// <summary>
/// Serves the game board (webtable) to an external browser and bridges board messages over WebSocket.
/// </summary>
/// <remarks>
/// Replaces the embedded WebView2 where it cannot be rendered (e.g. under Wine/CrossOver on macOS).
/// The page gets a <c>window.chrome.webview</c> shim backed by WebSocket, so the board scripts stay unchanged.
/// </remarks>
internal sealed class RemoteBoardServer : IAsyncDisposable
{
    private const string Shim = """
        <script>
        (function () {
            var listeners = [];
            var queue = [];
            var ws = null;
            function connect() {
                ws = new WebSocket((location.protocol === 'https:' ? 'wss://' : 'ws://') + location.host + '/ws');
                ws.onopen = function () { while (queue.length) { ws.send(queue.shift()); } };
                ws.onmessage = function (e) {
                    var data = JSON.parse(e.data);
                    listeners.slice().forEach(function (l) { l({ data: data }); });
                };
                ws.onclose = function () { setTimeout(function () { location.reload(); }, 1000); };
            }
            window.chrome = window.chrome || {};
            window.chrome.webview = {
                postMessage: function (msg) {
                    var text = JSON.stringify(msg, function (k, v) { return v instanceof Error ? String(v) : v; });
                    if (ws && ws.readyState === 1) { ws.send(text); } else { queue.push(text); }
                },
                addEventListener: function (type, l) { if (type === 'message') { listeners.push(l); } },
                removeEventListener: function (type, l) { listeners = listeners.filter(function (x) { return x !== l; }); }
            };
            connect();
        })();
        </script>
        """;

    private readonly WebApplication _app;
    private readonly IWebInterop _interop;
    private readonly ConcurrentDictionary<WebSocket, SemaphoreSlim> _clients = new();

    public string Url { get; }

    private RemoteBoardServer(WebApplication app, IWebInterop interop, string url)
    {
        _app = app;
        _interop = interop;
        Url = url;

        _interop.SendJsonMessage += OnSendJsonMessage;
    }

    public static async Task<RemoteBoardServer> StartAsync(IWebInterop interop, int port)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = baseDir });
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}/");

        var app = builder.Build();
        var url = $"http://127.0.0.1:{port}/webtable/index.html";
        RemoteBoardServer? server = null;

        app.UseWebSockets();

        app.MapGet("/", () => Results.Redirect("/webtable/index.html"));

        app.MapGet("/webtable/index.html", async () =>
        {
            var html = await File.ReadAllTextAsync(Path.Combine(baseDir, "webtable", "index.html"));
            return Results.Content(html.Replace("<head>", "<head>" + Shim), "text/html; charset=utf-8");
        });

        var contentTypes = new FileExtensionContentTypeProvider();

        app.MapGet("/fs", (string p) =>
        {
            if (!File.Exists(p))
            {
                return Results.NotFound();
            }

            if (!contentTypes.TryGetContentType(p, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return Results.File(p, contentType, enableRangeProcessing: true);
        });

        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest || server == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await server.ServeClientAsync(socket, context.RequestAborted);
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(baseDir),
            ServeUnknownFileTypes = true,
        });

        server = new RemoteBoardServer(app, interop, url);
        await app.StartAsync();

        return server;
    }

    private async Task ServeClientAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        _clients[socket] = new SemaphoreSlim(1, 1);

        try
        {
            var buffer = new byte[64 * 1024];
            using var message = new MemoryStream();

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                message.Write(buffer, 0, result.Count);

                if (!result.EndOfMessage)
                {
                    continue;
                }

                var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                message.SetLength(0);

                // WebView2 raises WebMessageReceived on the UI thread; keep the same contract
                Application.Current.Dispatcher.Invoke(() => _interop.OnMessage(text));
            }
        }
        catch (Exception exc) when (exc is WebSocketException or OperationCanceledException)
        {
        }
        finally
        {
            _clients.TryRemove(socket, out _);
        }
    }

    private void OnSendJsonMessage(string json)
    {
        var payload = Encoding.UTF8.GetBytes(RewriteLocalPaths(json));

        foreach (var (socket, sendLock) in _clients)
        {
            _ = SendAsync(socket, sendLock, payload);
        }
    }

    private static async Task SendAsync(WebSocket socket, SemaphoreSlim sendLock, byte[] payload)
    {
        await sendLock.WaitAsync();

        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            sendLock.Release();
        }
    }

    /// <summary>
    /// Replaces local file paths and file:// URIs with URLs served by this server, so the external browser can load package media.
    /// </summary>
    private static string RewriteLocalPaths(string json)
    {
        var node = JsonNode.Parse(json);

        if (node == null)
        {
            return json;
        }

        Rewrite(node);
        return node.ToJsonString();
    }

    private static void Rewrite(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToArray())
                {
                    if (obj[key] is { } child && TryRewriteValue(child) is { } replacement)
                    {
                        obj[key] = replacement;
                    }
                    else if (obj[key] is { } nested)
                    {
                        Rewrite(nested);
                    }
                }

                break;

            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (array[i] is { } child && TryRewriteValue(child) is { } replacement)
                    {
                        array[i] = replacement;
                    }
                    else if (array[i] is { } nested)
                    {
                        Rewrite(nested);
                    }
                }

                break;
        }
    }

    private static JsonNode? TryRewriteValue(JsonNode node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) && TryGetLocalPath(text, out var path)
            ? JsonValue.Create("/fs?p=" + Uri.EscapeDataString(path))
            : null;

    private static bool TryGetLocalPath(string text, out string path)
    {
        path = "";

        if (text.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && uri.IsFile)
        {
            path = uri.LocalPath;
            return true;
        }

        if (text.Length > 3 && char.IsLetter(text[0]) && text[1] == ':' && (text[2] == '\\' || text[2] == '/'))
        {
            path = text;
            return true;
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        _interop.SendJsonMessage -= OnSendJsonMessage;

        foreach (var socket in _clients.Keys)
        {
            socket.Abort();
        }

        await _app.DisposeAsync();
    }
}
