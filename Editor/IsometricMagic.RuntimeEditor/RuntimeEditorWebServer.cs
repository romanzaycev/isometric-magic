using System.Net;
using System.Text;

using Newtonsoft.Json;

using NLog;

namespace IsometricMagic.RuntimeEditor
{
    internal sealed class RuntimeEditorWebServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly int _port;
        private readonly RuntimeEditorService _service;

        private HttpListener? _listener;
        private CancellationTokenSource? _cancellation;

        public RuntimeEditorWebServer(int port, RuntimeEditorService service)
        {
            _port = port;
            _service = service;
        }

        public string BaseUrl => $"http://127.0.0.1:{_port}/";

        public bool Start()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();
                _cancellation = new CancellationTokenSource();
                _ = Task.Run(() => AcceptLoop(_cancellation.Token));

                Logger.Info("Runtime editor web server started at {Url}", BaseUrl);
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to start runtime editor web server at {Url}", BaseUrl);
                return false;
            }
        }

        public void Stop()
        {
            try
            {
                _cancellation?.Cancel();
                _listener?.Stop();
                _listener?.Close();
                _listener = null;
                _cancellation?.Dispose();
                _cancellation = null;

                Logger.Info("Runtime editor web server stopped");
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Failed to stop runtime editor web server cleanly");
            }
        }

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext? context = null;
                try
                {
                    if (_listener == null)
                    {
                        return;
                    }

                    context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleContext(context));
                }
                catch (HttpListenerException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Logger.Warn(exception, "Runtime editor accept loop error");
                    context?.Response.OutputStream.Close();
                }
            }
        }

        private async Task HandleContext(HttpListenerContext context)
        {
            try
            {
                if (!IsLoopback(context.Request.RemoteEndPoint))
                {
                    await WriteText(context.Response, 403, "text/plain", "Forbidden");
                    return;
                }

                var path = context.Request.Url?.AbsolutePath ?? "/";
                var method = context.Request.HttpMethod;

                if (method == "GET" && path == "/")
                {
                    await WriteText(context.Response, 200, "text/html; charset=utf-8", RuntimeEditorPage.Html);
                    return;
                }

                if (method == "GET" && path == "/api/scene")
                {
                    var json = await _service.RunOnMainThread(_service.BuildSceneJson);
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "POST" && path == "/api/scenes/load")
                {
                    var body = await ReadBody(context.Request);
                    var json = await _service.RunOnMainThread(() => _service.ApplySceneLoadJson(body));
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "GET" && path.StartsWith("/api/entity/", StringComparison.Ordinal))
                {
                    var idText = path.Substring("/api/entity/".Length);
                    if (!int.TryParse(idText, out var entityId))
                    {
                        await WriteText(context.Response, 400, "application/json; charset=utf-8",
                            JsonConvert.SerializeObject(new { found = false, error = "Invalid entity id" }));
                        return;
                    }

                    var json = await _service.RunOnMainThread(() => _service.BuildEntityInspectorJson(entityId));
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "GET" && path == "/api/lighting")
                {
                    var json = await _service.RunOnMainThread(_service.BuildLightingJson);
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "POST" && path == "/api/light/add")
                {
                    var json = await _service.RunOnMainThread(_service.AddLightJson);
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "POST" && path == "/api/light/remove")
                {
                    var body = await ReadBody(context.Request);
                    var json = await _service.RunOnMainThread(() => _service.RemoveLightJson(body));
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "GET" && path == "/api/sprites")
                {
                    var json = await _service.RunOnMainThread(_service.BuildSpritesJson);
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "GET" && path.StartsWith("/api/sprite/", StringComparison.Ordinal))
                {
                    var idText = path.Substring("/api/sprite/".Length);
                    if (!int.TryParse(idText, out var spriteId))
                    {
                        await WriteText(context.Response, 400, "application/json; charset=utf-8",
                            JsonConvert.SerializeObject(new { found = false, error = "Invalid sprite id" }));
                        return;
                    }

                    var json = await _service.RunOnMainThread(() => _service.BuildSpriteInspectorJson(spriteId));
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                if (method == "POST" && path == "/api/set")
                {
                    var body = await ReadBody(context.Request);
                    var json = await _service.RunOnMainThread(() => _service.ApplyEditJson(body));
                    await WriteText(context.Response, 200, "application/json; charset=utf-8", json);
                    return;
                }

                await WriteText(context.Response, 404, "text/plain; charset=utf-8", "Not Found");
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Runtime editor request handling error");
                await WriteText(context.Response, 500, "text/plain; charset=utf-8", "Internal Server Error");
            }
        }

        private static async Task<string> ReadBody(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private static bool IsLoopback(IPEndPoint? endpoint)
        {
            if (endpoint == null)
            {
                return false;
            }

            return IPAddress.IsLoopback(endpoint.Address);
        }

        private static async Task WriteText(HttpListenerResponse response, int statusCode, string contentType, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            response.StatusCode = statusCode;
            response.ContentType = contentType;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = bytes.Length;

            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.OutputStream.Close();
        }
    }
}
