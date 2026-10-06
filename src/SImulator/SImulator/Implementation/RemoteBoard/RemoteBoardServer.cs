using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using SImulator.Properties;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Utils;
using Utils.Web;

namespace SImulator.Implementation.RemoteBoard;

/// <summary>
/// Serves the game board (webtable) to an external browser and bridges board messages over WebSocket.
/// </summary>
/// <remarks>
/// Replaces the embedded WebView2 where it cannot be rendered (e.g. under Wine/CrossOver on macOS).
/// The page gets a <c>window.chrome.webview</c> shim backed by WebSocket, so the board scripts stay unchanged.
/// Security:
/// <list type="bullet">
/// <item>the server listens on the loopback interface only;</item>
/// <item>the WebSocket requires the random session token from the board URL and the board origin,
/// so other pages opened in the browser cannot control the game;</item>
/// <item>only one board client is allowed at a time;</item>
/// <item>only the board files, the sounds folder and the media files sent to the board by the game are served.</item>
/// </list>
/// </remarks>
internal sealed class RemoteBoardServer : IAsyncDisposable
{
    /// <summary>
    /// WebSocket close status sent to a second board client.
    /// </summary>
    private const int BoardAlreadyOpenedCloseStatus = 4001;

    private const string ShimTemplate = """
        <script>
        (function () {
            var texts = __TEXTS__;
            var token = new URLSearchParams(location.hash.substring(1)).get('token') || '';
            var listeners = [];
            var queue = [];
            var ws = new WebSocket('ws://' + location.host + '/ws?token=' + encodeURIComponent(token));
            function showMessage(text) {
                var overlay = document.createElement('div');
                overlay.textContent = text;
                overlay.style.cssText = 'position:fixed;inset:0;z-index:100000;display:flex;align-items:center;justify-content:center;' +
                    'padding:2em;text-align:center;font:2em sans-serif;color:#fff;background:rgba(0,0,40,.92)';
                document.body.appendChild(overlay);
            }
            ws.onopen = function () { while (queue.length) { ws.send(queue.shift()); } };
            ws.onmessage = function (e) {
                var data = JSON.parse(e.data);
                listeners.slice().forEach(function (l) { l({ data: data }); });
            };
            ws.onclose = function (e) { showMessage(e.code === __BUSY__ ? texts.alreadyOpened : texts.disconnected); };
            window.chrome = window.chrome || {};
            window.chrome.webview = {
                postMessage: function (msg) {
                    var text = JSON.stringify(msg, function (k, v) { return v instanceof Error ? String(v) : v; });
                    if (ws.readyState === 1) { ws.send(text); } else if (ws.readyState === 0) { queue.push(text); }
                },
                addEventListener: function (type, l) { if (type === 'message') { listeners.push(l); } },
                removeEventListener: function (type, l) { listeners = listeners.filter(function (x) { return x !== l; }); }
            };
        })();
        </script>
        """;

    private readonly WebApplication _app;
    private readonly IWebInterop _interop;
    private readonly byte[] _token;
    private readonly string _origin;
    private readonly object _clientLock = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<string, string> _mediaIdsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _mediaPathsById = new();
    private WebSocket? _client;

    /// <summary>
    /// Board URL. Contains the session token in the fragment, so it is not sent in HTTP requests.
    /// </summary>
    public string Url { get; }

    private RemoteBoardServer(WebApplication app, IWebInterop interop, int port, string token)
    {
        _app = app;
        _interop = interop;
        _token = Encoding.ASCII.GetBytes(token);
        _origin = $"http://127.0.0.1:{port}";
        Url = $"{_origin}/webtable/index.html#token={token}";

        _interop.SendJsonMessage += OnSendJsonMessage;
    }

    public static async Task<RemoteBoardServer> StartAsync(IWebInterop interop, int port)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = baseDir });
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}/");

        var app = builder.Build();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var server = new RemoteBoardServer(app, interop, port, token);

        var shim = ShimTemplate
            .Replace("__TEXTS__", JsonSerializer.Serialize(new
            {
                alreadyOpened = Resources.BrowserBoardAlreadyOpened,
                disconnected = Resources.BrowserBoardDisconnected,
            }))
            .Replace("__BUSY__", BoardAlreadyOpenedCloseStatus.ToString());

        app.UseWebSockets();

        app.MapGet("/webtable/index.html", async () =>
        {
            var html = await File.ReadAllTextAsync(Path.Combine(baseDir, "webtable", "index.html"));
            return Results.Content(html.Replace("<head>", "<head>" + shim), "text/html; charset=utf-8");
        });

        var contentTypes = new FileExtensionContentTypeProvider();

        app.MapGet("/media/{id}", (string id) =>
        {
            if (!server._mediaPathsById.TryGetValue(id, out var path) || !File.Exists(path))
            {
                return Results.NotFound();
            }

            if (!contentTypes.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return Results.File(path, contentType, enableRangeProcessing: true);
        });

        app.Map("/ws", server.HandleWebSocketAsync);

        foreach (var folder in new[] { "webtable", "sounds" })
        {
            var folderPath = Path.Combine(baseDir, folder);

            if (Directory.Exists(folderPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(folderPath),
                    RequestPath = "/" + folder,
                    ServeUnknownFileTypes = true,
                });
            }
        }

        await app.StartAsync();

        return server;
    }

    private async Task HandleWebSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var token = Encoding.ASCII.GetBytes(context.Request.Query["token"].ToString());

        if (context.Request.Headers.Origin != _origin || !CryptographicOperations.FixedTimeEquals(token, _token))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        lock (_clientLock)
        {
            if (_client == null)
            {
                _client = socket;
            }
        }

        if (_client != socket)
        {
            await socket.CloseAsync((WebSocketCloseStatus)BoardAlreadyOpenedCloseStatus, "Board is already opened", context.RequestAborted);
            return;
        }

        try
        {
            await ReceiveMessagesAsync(socket, context.RequestAborted);
        }
        finally
        {
            lock (_clientLock)
            {
                _client = null;
            }
        }
    }

    private async Task ReceiveMessagesAsync(WebSocket socket, CancellationToken cancellationToken)
    {
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
                await UI.ExecuteAsync(
                    () =>
                    {
                        _interop.OnMessage(text);
                        return true;
                    },
                    exc => Trace.TraceError(exc.ToString()));
            }
        }
        catch (Exception exc) when (exc is WebSocketException or OperationCanceledException)
        {
        }
    }

    private void OnSendJsonMessage(string json)
    {
        var client = _client;

        if (client == null)
        {
            return;
        }

        _ = SendAsync(client, Encoding.UTF8.GetBytes(RewriteLocalPaths(json)));
    }

    private async Task SendAsync(WebSocket socket, byte[] payload)
    {
        await _sendLock.WaitAsync();

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
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Replaces local file paths and file:// URIs with media URLs served by this server, so the external browser can load package media.
    /// </summary>
    /// <remarks>
    /// Only the files referenced in the board messages become available to the browser.
    /// </remarks>
    private string RewriteLocalPaths(string json)
    {
        var node = JsonNode.Parse(json);

        if (node == null)
        {
            return json;
        }

        Rewrite(node);
        return node.ToJsonString();
    }

    private void Rewrite(JsonNode node)
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

    private JsonNode? TryRewriteValue(JsonNode node)
    {
        if (node is not JsonValue value || !value.TryGetValue<string>(out var text) || !TryGetLocalPath(text, out var path))
        {
            return null;
        }

        var id = _mediaIdsByPath.GetOrAdd(path, _ => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
        _mediaPathsById[id] = path;

        return JsonValue.Create($"/media/{id}");
    }

    private static bool TryGetLocalPath(string text, out string path)
    {
        path = "";

        if (text.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || !uri.IsFile || uri.IsUnc)
            {
                return false;
            }

            text = uri.LocalPath;
        }

        // Local drive paths only (C:\... or C:/...); network (UNC) paths are not allowed
        if (text.Length <= 3 || !char.IsAsciiLetter(text[0]) || text[1] != ':' || (text[2] != '\\' && text[2] != '/'))
        {
            return false;
        }

        try
        {
            path = Path.GetFullPath(text);
            return true;
        }
        catch (Exception exc) when (exc is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _interop.SendJsonMessage -= OnSendJsonMessage;
        _client?.Abort();

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
