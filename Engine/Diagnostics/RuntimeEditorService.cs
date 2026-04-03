#if DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IsometricMagic.Engine.App;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Inputs;
using IsometricMagic.Engine.SceneGraph;
using IsometricMagic.Engine.Scenes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace IsometricMagic.Engine.Diagnostics
{
    public sealed class RuntimeEditorService
    {
        private static readonly RuntimeEditorService Instance = new();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly SceneManager SceneManager = SceneManager.GetInstance();

        private readonly ConcurrentQueue<IQueuedWorkItem> _mainThreadQueue = new();
        private readonly Dictionary<Entity, int> _entityIds = new();
        private readonly object _stateLock = new();

        private RuntimeEditorWebServer? _server;
        private bool _initialized;
        private bool _enabled;
        private Key _toggleKey = Key.F4;
        private int _port = 5057;
        private bool _openBrowser = true;
        private int _mainThreadId;

        public static RuntimeEditorService GetInstance()
        {
            return Instance;
        }

        public void Initialize(AppConfig config)
        {
            _enabled = config.RuntimeEditorEnabled;
            _toggleKey = config.RuntimeEditorToggleKey;
            _port = config.RuntimeEditorPort;
            _openBrowser = config.RuntimeEditorOpenBrowser;
            _mainThreadId = Environment.CurrentManagedThreadId;
            _initialized = true;

            Logger.Info("Runtime editor initialized. Enabled={Enabled}, ToggleKey={ToggleKey}, Port={Port}",
                _enabled, _toggleKey, _port);
        }

        public void Update()
        {
            if (!_initialized)
            {
                return;
            }

            DrainMainThreadQueue();

            if (!_enabled)
            {
                return;
            }

            if (Input.WasPressed(_toggleKey))
            {
                RestartServer();
            }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                _server?.Stop();
                _server = null;
            }
        }

        private void RestartServer()
        {
            lock (_stateLock)
            {
                _server?.Stop();
                _server = new RuntimeEditorWebServer(_port, this);

                if (!_server.Start())
                {
                    _server = null;
                    return;
                }

                if (_openBrowser)
                {
                    TryOpenBrowser(_server.BaseUrl);
                }
            }
        }

        private static void TryOpenBrowser(string url)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Failed to open runtime editor URL: {Url}", url);
            }
        }

        private void DrainMainThreadQueue()
        {
            while (_mainThreadQueue.TryDequeue(out var workItem))
            {
                workItem.Execute();
            }
        }

        internal Task<T> RunOnMainThread<T>(Func<T> action)
        {
            if (Environment.CurrentManagedThreadId == _mainThreadId)
            {
                return Task.FromResult(action());
            }

            var workItem = new QueuedWorkItem<T>(action);
            _mainThreadQueue.Enqueue(workItem);
            return workItem.Task;
        }

        internal string BuildSceneGraphJson()
        {
            var scene = SceneManager.GetCurrent();
            var root = BuildEntityTree(scene.Root);
            var payload = new
            {
                scene = scene.Name,
                root
            };

            return JsonConvert.SerializeObject(payload);
        }

        internal string BuildEntityInspectorJson(int entityId)
        {
            var scene = SceneManager.GetCurrent();
            var entity = TryResolveEntity(scene.Root, entityId);
            if (entity == null)
            {
                return JsonConvert.SerializeObject(new { found = false });
            }

            var payload = new
            {
                found = true,
                entity = BuildEntityInspector(entity)
            };

            return JsonConvert.SerializeObject(payload);
        }

        internal string BuildLightingJson()
        {
            var scene = SceneManager.GetCurrent();
            var lighting = scene.Lighting;

            var lights = new List<object>(lighting.Lights.Count);
            for (var i = 0; i < lighting.Lights.Count; i++)
            {
                var light = lighting.Lights[i];
                lights.Add(new
                {
                    index = i,
                    members = BuildMembersPayload(light)
                });
            }

            var payload = new
            {
                scene = scene.Name,
                ambientMembers = BuildMembersPayload(lighting),
                lights
            };

            return JsonConvert.SerializeObject(payload);
        }

        internal string ApplyEditJson(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Empty request body" });
            }

            try
            {
                var root = JObject.Parse(requestBody);
                var target = root.Value<string>("target") ?? string.Empty;
                var memberName = root.Value<string>("member") ?? string.Empty;
                var value = root["value"];

                if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(memberName) || value == null)
                {
                    return JsonConvert.SerializeObject(new { ok = false, error = "Missing target/member/value" });
                }

                return target switch
                {
                    "entity" => ApplyEntityEdit(root, memberName, value),
                    "transform" => ApplyTransformEdit(root, memberName, value),
                    "component" => ApplyComponentEdit(root, memberName, value),
                    "lighting" => ApplyLightingEdit(memberName, value),
                    "light" => ApplyLightEdit(root, memberName, value),
                    _ => JsonConvert.SerializeObject(new { ok = false, error = $"Unknown target '{target}'" })
                };
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Runtime editor apply failed");
                return JsonConvert.SerializeObject(new { ok = false, error = exception.Message });
            }
        }

        private string ApplyEntityEdit(JObject root, string memberName, JToken value)
        {
            var entity = ResolveEntityFromRequest(root);
            if (entity == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Entity not found" });
            }

            switch (memberName)
            {
                case nameof(Entity.Name):
                    entity.Name = value.Value<string>() ?? entity.Name;
                    break;
                case nameof(Entity.Tag):
                    entity.Tag = value.Value<string>() ?? entity.Tag;
                    break;
                case nameof(Entity.ActiveSelf):
                    entity.ActiveSelf = value.Value<bool>();
                    break;
                default:
                    return JsonConvert.SerializeObject(new { ok = false, error = $"Unsupported entity member '{memberName}'" });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyTransformEdit(JObject root, string memberName, JToken value)
        {
            var entity = ResolveEntityFromRequest(root);
            if (entity == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Entity not found" });
            }

            var transform = entity.Transform;
            if (!TrySetMemberValue(transform, memberName, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyComponentEdit(JObject root, string memberName, JToken value)
        {
            var entity = ResolveEntityFromRequest(root);
            if (entity == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Entity not found" });
            }

            var componentIndex = root.Value<int?>("componentIndex");
            if (componentIndex == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Missing componentIndex" });
            }

            if (componentIndex < 0 || componentIndex >= entity.Components.Count)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Invalid componentIndex" });
            }

            var component = entity.Components[componentIndex.Value];
            if (!TrySetMemberValue(component, memberName, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyLightingEdit(string memberName, JToken value)
        {
            var scene = SceneManager.GetCurrent();
            var lighting = scene.Lighting;

            if (!TrySetMemberValue(lighting, memberName, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyLightEdit(JObject root, string memberName, JToken value)
        {
            var scene = SceneManager.GetCurrent();
            var lighting = scene.Lighting;

            var lightIndex = root.Value<int?>("lightIndex");
            if (lightIndex == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Missing lightIndex" });
            }

            if (lightIndex < 0 || lightIndex >= lighting.Lights.Count)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Invalid lightIndex" });
            }

            var light = lighting.Lights[lightIndex.Value];
            if (!TrySetMemberValue(light, memberName, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private Entity? ResolveEntityFromRequest(JObject root)
        {
            var entityId = root.Value<int?>("entityId");
            if (entityId == null)
            {
                return null;
            }

            var scene = SceneManager.GetCurrent();
            return TryResolveEntity(scene.Root, entityId.Value);
        }

        private Entity? TryResolveEntity(Entity root, int entityId)
        {
            var candidate = TryFindEntityById(root, entityId);
            if (candidate != null)
            {
                return candidate;
            }

            return null;
        }

        private Entity? TryFindEntityById(Entity entity, int entityId)
        {
            if (GetEntityId(entity) == entityId)
            {
                return entity;
            }

            foreach (var child in entity.Children)
            {
                var found = TryFindEntityById(child, entityId);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private object BuildEntityTree(Entity entity)
        {
            var children = new List<object>(entity.Children.Count);
            foreach (var child in entity.Children)
            {
                children.Add(BuildEntityTree(child));
            }

            var componentTypes = new List<string>(entity.Components.Count);
            foreach (var component in entity.Components)
            {
                componentTypes.Add(component.GetType().Name);
            }

            return new
            {
                id = GetEntityId(entity),
                name = entity.Name,
                tag = entity.Tag,
                activeSelf = entity.ActiveSelf,
                activeInHierarchy = entity.ActiveInHierarchy,
                components = componentTypes,
                children
            };
        }

        private object BuildEntityInspector(Entity entity)
        {
            var components = new List<object>(entity.Components.Count);
            for (var i = 0; i < entity.Components.Count; i++)
            {
                var component = entity.Components[i];
                components.Add(new
                {
                    index = i,
                    type = component.GetType().Name,
                    members = BuildMembersPayload(component)
                });
            }

            return new
            {
                id = GetEntityId(entity),
                name = entity.Name,
                tag = entity.Tag,
                activeSelf = entity.ActiveSelf,
                activeInHierarchy = entity.ActiveInHierarchy,
                entityMembers = new object[]
                {
                    BuildSimpleEditableMember(nameof(Entity.Name), typeof(string), entity.Name),
                    BuildSimpleEditableMember(nameof(Entity.Tag), typeof(string), entity.Tag),
                    BuildSimpleEditableMember(nameof(Entity.ActiveSelf), typeof(bool), entity.ActiveSelf)
                },
                transformMembers = BuildMembersPayload(entity.Transform),
                components
            };
        }

        private IEnumerable<object> BuildMembersPayload(object target)
        {
            var members = new List<MemberPayload>();

            foreach (var field in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                members.Add(BuildMemberDescriptor(field.FieldType, field.Name, field.GetValue(target), editable: IsEditableType(field.FieldType)));
            }

            foreach (var property in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                object? value;
                try
                {
                    value = property.GetValue(target);
                }
                catch
                {
                    continue;
                }

                var editable = property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic && IsEditableType(property.PropertyType);
                members.Add(BuildMemberDescriptor(property.PropertyType, property.Name, value, editable));
            }

            return members.OrderBy(member => member.Name).Select(member => member.ToPayload());
        }

        private static object BuildSimpleEditableMember(string name, Type type, object value)
        {
            return new
            {
                name,
                type = type.Name,
                editable = true,
                value = SerializeValue(value, type)
            };
        }

        private static MemberPayload BuildMemberDescriptor(Type memberType, string name, object? value, bool editable)
        {
            return new MemberPayload
            {
                Name = name,
                Type = memberType.Name,
                Editable = editable,
                Value = SerializeValue(value, memberType)
            };
        }

        private sealed class MemberPayload
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool Editable { get; set; }
            public object? Value { get; set; }

            public object ToPayload()
            {
                return new
                {
                    name = Name,
                    type = Type,
                    editable = Editable,
                    value = Value
                };
            }
        }

        private static object? SerializeValue(object? value, Type type)
        {
            if (value == null)
            {
                return null;
            }

            if (type == typeof(Vector2))
            {
                var vector = (Vector2)value;
                return new { x = vector.X, y = vector.Y };
            }

            if (type == typeof(Vector3))
            {
                var vector = (Vector3)value;
                return new { x = vector.X, y = vector.Y, z = vector.Z };
            }

            if (type.IsEnum)
            {
                return value.ToString();
            }

            if (IsEditableType(type))
            {
                return value;
            }

            return value.ToString();
        }

        private static bool IsEditableType(Type type)
        {
            if (type.IsEnum)
            {
                return true;
            }

            return type == typeof(bool)
                || type == typeof(int)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(string)
                || type == typeof(Vector2)
                || type == typeof(Vector3);
        }

        private static bool TrySetMemberValue(object target, string memberName, JToken valueToken, out string error)
        {
            error = string.Empty;
            var type = target.GetType();

            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                if (!IsEditableType(field.FieldType))
                {
                    error = $"Member '{memberName}' is read-only in runtime editor";
                    return false;
                }

                if (!TryConvertValue(valueToken, field.FieldType, out var converted, out error))
                {
                    return false;
                }

                field.SetValue(target, converted);
                return true;
            }

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || property.SetMethod == null || !property.SetMethod.IsPublic)
            {
                error = $"Member '{memberName}' not found or not writable";
                return false;
            }

            if (!IsEditableType(property.PropertyType))
            {
                error = $"Member '{memberName}' is read-only in runtime editor";
                return false;
            }

            if (!TryConvertValue(valueToken, property.PropertyType, out var convertedProperty, out error))
            {
                return false;
            }

            property.SetValue(target, convertedProperty);
            return true;
        }

        private static bool TryConvertValue(JToken token, Type targetType, out object? value, out string error)
        {
            error = string.Empty;
            value = null;

            try
            {
                if (targetType == typeof(bool))
                {
                    value = token.Value<bool>();
                    return true;
                }

                if (targetType == typeof(int))
                {
                    value = token.Value<int>();
                    return true;
                }

                if (targetType == typeof(float))
                {
                    value = token.Value<float>();
                    return true;
                }

                if (targetType == typeof(double))
                {
                    value = token.Value<double>();
                    return true;
                }

                if (targetType == typeof(string))
                {
                    value = token.Value<string>() ?? string.Empty;
                    return true;
                }

                if (targetType == typeof(Vector2))
                {
                    if (token is not JObject obj)
                    {
                        error = "Vector2 expects object with x and y";
                        return false;
                    }

                    value = new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
                    return true;
                }

                if (targetType == typeof(Vector3))
                {
                    if (token is not JObject obj)
                    {
                        error = "Vector3 expects object with x, y and z";
                        return false;
                    }

                    value = new Vector3(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"));
                    return true;
                }

                if (targetType.IsEnum)
                {
                    var text = token.Value<string>();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        error = $"Enum '{targetType.Name}' expects non-empty string";
                        return false;
                    }

                    if (!Enum.TryParse(targetType, text, true, out var parsedEnum))
                    {
                        error = $"Cannot parse '{text}' as {targetType.Name}";
                        return false;
                    }

                    value = parsedEnum;
                    return true;
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            error = $"Unsupported type '{targetType.Name}'";
            return false;
        }

        private int GetEntityId(Entity entity)
        {
            if (_entityIds.TryGetValue(entity, out var existing))
            {
                return existing;
            }

            var nextId = _entityIds.Count + 1;
            _entityIds[entity] = nextId;
            return nextId;
        }

        private interface IQueuedWorkItem
        {
            void Execute();
        }

        private sealed class QueuedWorkItem<T> : IQueuedWorkItem
        {
            private readonly Func<T> _action;
            private readonly TaskCompletionSource<T> _completionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public QueuedWorkItem(Func<T> action)
            {
                _action = action;
            }

            public Task<T> Task => _completionSource.Task;

            public void Execute()
            {
                try
                {
                    var result = _action();
                    _completionSource.TrySetResult(result);
                }
                catch (Exception exception)
                {
                    _completionSource.TrySetException(exception);
                }
            }
        }
    }

    internal sealed class RuntimeEditorWebServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly int _port;
        private readonly RuntimeEditorService _service;

        private HttpListener? _listener;
        private CancellationTokenSource? _cancellation;
        private Task? _acceptLoopTask;

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
                _acceptLoopTask = Task.Run(() => AcceptLoop(_cancellation.Token));

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
                    var json = await _service.RunOnMainThread(_service.BuildSceneGraphJson);
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

                if (method == "POST" && path == "/api/set")
                {
                    string body;
                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
                    {
                        body = await reader.ReadToEndAsync();
                    }

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

    internal static class RuntimeEditorPage
    {
        public const string Html = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Runtime Editor</title>
  <style>
    :root { color-scheme: light; }
    * { box-sizing: border-box; }
    body { margin: 0; font: 14px/1.4 monospace; background: #f6f7fb; color: #1b1f2a; }
    header { padding: 10px 14px; border-bottom: 1px solid #d8dce6; background: #fff; display: flex; gap: 8px; align-items: center; }
    button { font: inherit; padding: 6px 10px; cursor: pointer; }
    .layout { display: grid; grid-template-columns: 320px 1fr; gap: 0; height: calc(100vh - 48px); }
    .pane { overflow: auto; padding: 10px; border-right: 1px solid #d8dce6; }
    .pane:last-child { border-right: none; }
    .card { background: #fff; border: 1px solid #d8dce6; border-radius: 6px; margin-bottom: 10px; padding: 10px; }
    .tree ul { margin: 2px 0 2px 14px; padding: 0; }
    .tree li { list-style: none; margin: 2px 0; }
    .tree .node { cursor: pointer; padding: 2px 4px; border-radius: 4px; display: inline-block; }
    .tree .node:hover { background: #e8ecf7; }
    .tree .selected { background: #cfd9f5; }
    .row { display: grid; grid-template-columns: 180px 1fr; gap: 8px; margin: 6px 0; align-items: center; }
    .row input { width: 100%; padding: 4px 6px; font: inherit; }
    .row .vec { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px; }
    .readonly { opacity: 0.7; }
    h3, h4 { margin: 6px 0; }
    .muted { opacity: 0.75; }
  </style>
</head>
<body>
<header>
  <strong>Runtime Editor</strong>
  <button id="refreshAll">Refresh All</button>
  <button id="refreshScene">Refresh Graph</button>
  <button id="refreshInspector">Refresh Inspector</button>
  <button id="refreshLighting">Refresh Lighting</button>
  <input id="treeFilter" type="text" placeholder="Filter name/tag/component" style="min-width: 240px; padding: 6px 8px;" />
  <span id="status" class="muted"></span>
</header>
<div class="layout">
  <div class="pane">
    <div class="card tree">
      <h3>Entity Graph</h3>
      <div id="sceneName" class="muted"></div>
      <div id="tree"></div>
    </div>
  </div>
  <div class="pane">
    <div class="card">
      <h3>Inspector</h3>
      <div id="inspector">Select entity in graph.</div>
    </div>
    <div class="card">
      <h3>Scene Lighting</h3>
      <div id="lighting"></div>
    </div>
  </div>
</div>
<script>
let selectedEntityId = null;
let currentSceneRoot = null;
let currentFilter = '';

const statusEl = document.getElementById('status');
const treeEl = document.getElementById('tree');
const inspectorEl = document.getElementById('inspector');
const lightingEl = document.getElementById('lighting');
const sceneNameEl = document.getElementById('sceneName');
const treeFilterEl = document.getElementById('treeFilter');

document.getElementById('refreshAll').addEventListener('click', refreshAll);
document.getElementById('refreshScene').addEventListener('click', loadScene);
document.getElementById('refreshInspector').addEventListener('click', () => selectedEntityId && loadInspector(selectedEntityId));
document.getElementById('refreshLighting').addEventListener('click', loadLighting);
treeFilterEl.addEventListener('input', () => {
  currentFilter = treeFilterEl.value || '';
  renderTree();
});

function setStatus(text) { statusEl.textContent = text; }

async function api(path, options) {
  const response = await fetch(path, options);
  const json = await response.json();
  return json;
}

function makeInput(member, onCommit) {
  if (!member.editable) {
    const div = document.createElement('div');
    div.className = 'readonly';
    div.textContent = JSON.stringify(member.value);
    return div;
  }

  if (member.type === 'Boolean') {
    const input = document.createElement('input');
    input.type = 'checkbox';
    input.checked = !!member.value;
    input.addEventListener('change', () => onCommit(input.checked));
    return input;
  }

  if (member.type === 'Vector2' || member.type === 'Vector3') {
    const wrap = document.createElement('div');
    wrap.className = 'vec';
    const x = document.createElement('input');
    const y = document.createElement('input');
    x.type = y.type = 'number';
    x.step = y.step = 'any';
    x.value = member.value?.x ?? 0;
    y.value = member.value?.y ?? 0;
    wrap.appendChild(x);
    wrap.appendChild(y);

    let z = null;
    if (member.type === 'Vector3') {
      z = document.createElement('input');
      z.type = 'number';
      z.step = 'any';
      z.value = member.value?.z ?? 0;
      wrap.appendChild(z);
    }

    const commit = async () => {
      const payload = { x: parseFloat(x.value), y: parseFloat(y.value) };
      if (z) payload.z = parseFloat(z.value);
      await onCommit(payload);
    };

    x.addEventListener('change', commit);
    y.addEventListener('change', commit);
    if (z) z.addEventListener('change', commit);
    return wrap;
  }

  const input = document.createElement('input');
  input.type = (member.type === 'Single' || member.type === 'Double' || member.type === 'Int32') ? 'number' : 'text';
  if (input.type === 'number') {
    input.step = 'any';
  }
  input.value = member.value ?? '';
  input.addEventListener('change', async () => {
    let value = input.value;
    if (member.type === 'Single' || member.type === 'Double') value = parseFloat(value);
    if (member.type === 'Int32') value = parseInt(value, 10);
    await onCommit(value);
  });
  return input;
}

async function setValue(payload) {
  const result = await api('/api/set', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  if (!result.ok) {
    setStatus('Apply failed: ' + result.error);
    return;
  }

  setStatus('Applied: ' + payload.member);
}

function renderMembers(container, title, members, commitBuilder) {
  const h = document.createElement('h4');
  h.textContent = title;
  container.appendChild(h);

  for (const member of members) {
    const row = document.createElement('div');
    row.className = 'row';

    const name = document.createElement('div');
    name.textContent = member.name + ' : ' + member.type;

    const editor = makeInput(member, async (value) => {
      const payload = commitBuilder(member.name, value);
      await setValue(payload);
    });

    row.appendChild(name);
    row.appendChild(editor);
    container.appendChild(row);
  }
}

function renderTreeNode(node) {
  const li = document.createElement('li');
  const line = document.createElement('span');
  line.className = 'node' + (node.id === selectedEntityId ? ' selected' : '');
  line.textContent = `${node.name} [${node.components.join(', ')}]`;
  line.addEventListener('click', async () => {
    selectedEntityId = node.id;
    await loadInspector(node.id);
    await loadScene();
  });
  li.appendChild(line);

  if (node.children && node.children.length > 0) {
    const ul = document.createElement('ul');
    for (const child of node.children) {
      ul.appendChild(renderTreeNode(child));
    }
    li.appendChild(ul);
  }

  return li;
}

function filterTree(node, filterText) {
  if (!filterText) {
    return node;
  }

  const lowered = filterText.toLowerCase();
  const componentText = (node.components || []).join(' ').toLowerCase();
  const selfMatches =
    (node.name || '').toLowerCase().includes(lowered)
    || (node.tag || '').toLowerCase().includes(lowered)
    || componentText.includes(lowered);

  const filteredChildren = [];
  for (const child of (node.children || [])) {
    const filtered = filterTree(child, filterText);
    if (filtered) {
      filteredChildren.push(filtered);
    }
  }

  if (selfMatches || filteredChildren.length > 0) {
    return {
      ...node,
      children: filteredChildren
    };
  }

  return null;
}

function renderTree() {
  treeEl.innerHTML = '';
  if (!currentSceneRoot) {
    return;
  }

  const filteredRoot = filterTree(currentSceneRoot, currentFilter);
  if (!filteredRoot) {
    const empty = document.createElement('div');
    empty.className = 'muted';
    empty.textContent = 'No entities match the filter.';
    treeEl.appendChild(empty);
    return;
  }

  const ul = document.createElement('ul');
  ul.appendChild(renderTreeNode(filteredRoot));
  treeEl.appendChild(ul);
}

async function loadScene() {
  const data = await api('/api/scene');
  sceneNameEl.textContent = 'Scene: ' + data.scene;
  currentSceneRoot = data.root;
  renderTree();
}

async function loadInspector(entityId) {
  const data = await api('/api/entity/' + entityId);
  inspectorEl.innerHTML = '';
  if (!data.found) {
    inspectorEl.textContent = 'Entity not found.';
    return;
  }

  const entity = data.entity;
  renderMembers(inspectorEl, 'Entity', entity.entityMembers, (member, value) => ({
    target: 'entity', entityId, member, value
  }));

  renderMembers(inspectorEl, 'Transform2D', entity.transformMembers, (member, value) => ({
    target: 'transform', entityId, member, value
  }));

  for (const component of entity.components) {
    renderMembers(inspectorEl, component.type, component.members, (member, value) => ({
      target: 'component', entityId, componentIndex: component.index, member, value
    }));
  }
}

async function loadLighting() {
  const data = await api('/api/lighting');
  lightingEl.innerHTML = '';
  renderMembers(lightingEl, 'Ambient', data.ambientMembers, (member, value) => ({
    target: 'lighting', member, value
  }));

  for (const light of data.lights) {
    renderMembers(lightingEl, 'Point Light #' + light.index, light.members, (member, value) => ({
      target: 'light', lightIndex: light.index, member, value
    }));
  }
}

async function refreshAll() {
  await loadScene();
  if (selectedEntityId) {
    await loadInspector(selectedEntityId);
  }
  await loadLighting();
}

(async function init() {
  try {
    await refreshAll();
    setStatus('Ready');
  } catch (e) {
    setStatus('Init failed: ' + e);
  }
})();
</script>
</body>
</html>
""";
    }
}
#endif

#if !DEBUG
using IsometricMagic.Engine.App;

namespace IsometricMagic.Engine.Diagnostics
{
    public sealed class RuntimeEditorService
    {
        private static readonly RuntimeEditorService Instance = new();

        public static RuntimeEditorService GetInstance()
        {
            return Instance;
        }

        public void Initialize(AppConfig config)
        {
        }

        public void Update()
        {
        }

        public void Stop()
        {
        }
    }
}
#endif
