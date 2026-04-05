using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using IsometricMagic.Engine.App;
using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Engine.Inputs;
using IsometricMagic.Engine.Rendering;
using IsometricMagic.Engine.SceneGraph;
using IsometricMagic.Engine.Scenes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;

namespace IsometricMagic.RuntimeEditor
{
    public sealed class RuntimeEditorService : IApplicationRuntimeService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly SceneManager SceneManager = SceneManager.GetInstance();

        private readonly ConcurrentQueue<IQueuedWorkItem> _mainThreadQueue = new();
        private readonly Dictionary<Entity, int> _entityIds = new();
        private readonly Dictionary<Sprite, int> _spriteIds = new();
        private readonly Dictionary<Light2D, int> _lightIds = new();
        private readonly object _stateLock = new();

        private RuntimeEditorWebServer? _server;
        private bool _initialized;
        private bool _enabled;
        private Key _toggleKey = Key.F4;
        private int _port = 5057;
        private bool _openBrowser = true;
        private bool _autostart = false;
        private bool _browserAppMode;
        private string _browserExecutable = "chromium";
        private System.Diagnostics.Process? _browserProcess;
        private int _mainThreadId;
        private Scene? _lastScene;

        public void Initialize(AppConfig config)
        {
            _enabled = config.RuntimeEditorEnabled;
            _toggleKey = config.RuntimeEditorToggleKey;
            _port = config.RuntimeEditorPort;
            _openBrowser = config.RuntimeEditorOpenBrowser;
            _autostart = config.RuntimeEditorAutostart;
            _browserAppMode = config.RuntimeEditorBrowserAppMode;
            _browserExecutable = config.RuntimeEditorBrowserExecutable;
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
            EnsureSceneState();

            if (!_enabled)
            {
                return;
            }

            if (Input.WasPressed(_toggleKey) || (_server == null && _autostart))
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

        private void EnsureSceneState()
        {
            var currentScene = SceneManager.GetCurrent();
            if (ReferenceEquals(currentScene, _lastScene))
            {
                return;
            }

            _lastScene = currentScene;
            _entityIds.Clear();
            _spriteIds.Clear();
            _lightIds.Clear();
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
                    OpenBrowser(_server.BaseUrl);
                }
            }
        }

        private void OpenBrowser(string url)
        {
            if (_browserAppMode)
            {
                TryOpenBrowserAppMode(url);
                return;
            }

            TryOpenBrowser(url);
        }

        private void TryOpenBrowserAppMode(string url)
        {
            if (IsBrowserAppWindowOpen())
            {
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _browserExecutable,
                    Arguments = BuildBrowserAppArguments(url),
                    UseShellExecute = false
                };

                _browserProcess = System.Diagnostics.Process.Start(psi);
                if (_browserProcess == null)
                {
                    Logger.Warn("Failed to start runtime editor in browser app mode. Executable={Executable}, Url={Url}",
                        _browserExecutable, url);
                    return;
                }
            }
            catch (Exception exception)
            {
                Logger.Warn(exception,
                    "Failed to start runtime editor in browser app mode. Executable={Executable}, Url={Url}",
                    _browserExecutable, url);
            }
        }

        private bool IsBrowserAppWindowOpen()
        {
            return _browserProcess != null && !_browserProcess.HasExited;
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

        private static string BuildBrowserAppArguments(string url)
        {
            return $"--app={url}";
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

        internal string BuildSceneJson()
        {
            EnsureSceneState();
            var scene = SceneManager.GetCurrent();
            var payload = new
            {
                scene = scene.Name,
                scenes = SceneManager.GetSceneNames(),
                root = BuildEntityTree(scene.Root)
            };

            return JsonConvert.SerializeObject(payload);
        }

        internal string BuildEntityInspectorJson(int entityId)
        {
            EnsureSceneState();
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
            EnsureSceneState();
            var scene = SceneManager.GetCurrent();
            var lighting = scene.Lighting;

            var lights = new List<object>(lighting.Lights.Count);
            for (var i = 0; i < lighting.Lights.Count; i++)
            {
                var light = lighting.Lights[i];
                lights.Add(new
                {
                    id = GetLightId(light),
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

        internal string BuildSpritesJson()
        {
            EnsureSceneState();
            var scene = SceneManager.GetCurrent();

            var mainSprites = scene.MainLayer.Sprites.Select(BuildSpriteNode).ToList();
            var uiSprites = scene.UiLayer.Sprites.Select(BuildSpriteNode).ToList();

            return JsonConvert.SerializeObject(new
            {
                scene = scene.Name,
                layers = new object[]
                {
                    new { name = scene.MainLayer.Name, sprites = mainSprites },
                    new { name = scene.UiLayer.Name, sprites = uiSprites }
                }
            });
        }

        internal string BuildSpriteInspectorJson(int spriteId)
        {
            EnsureSceneState();
            var sprite = TryResolveSprite(spriteId);
            if (sprite == null)
            {
                return JsonConvert.SerializeObject(new { found = false });
            }

            return JsonConvert.SerializeObject(new
            {
                found = true,
                sprite = new
                {
                    id = spriteId,
                    members = BuildMembersPayload(sprite)
                }
            });
        }

        internal string ApplySceneLoadJson(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Empty request body" });
            }

            try
            {
                var root = JObject.Parse(requestBody);
                var name = root.Value<string>("name") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                {
                    return JsonConvert.SerializeObject(new { ok = false, error = "Missing scene name" });
                }

                SceneManager.LoadByName(name);
                EnsureSceneState();
                return JsonConvert.SerializeObject(new { ok = true });
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Runtime editor scene load failed");
                return JsonConvert.SerializeObject(new { ok = false, error = exception.Message });
            }
        }

        internal string AddLightJson()
        {
            try
            {
                var scene = SceneManager.GetCurrent();
                var light = new Light2D(Vector2.Zero);
                scene.Lighting.Add(light);
                return JsonConvert.SerializeObject(new { ok = true, lightId = GetLightId(light) });
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Runtime editor add light failed");
                return JsonConvert.SerializeObject(new { ok = false, error = exception.Message });
            }
        }

        internal string RemoveLightJson(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Empty request body" });
            }

            try
            {
                var root = JObject.Parse(requestBody);
                var scene = SceneManager.GetCurrent();
                var lighting = scene.Lighting;

                var lightId = root.Value<int?>("lightId");
                if (lightId.HasValue)
                {
                    var light = TryResolveLight(lightId.Value);
                    if (light == null)
                    {
                        return JsonConvert.SerializeObject(new { ok = false, error = "Light not found" });
                    }

                    lighting.Remove(light);
                    return JsonConvert.SerializeObject(new { ok = true });
                }

                var lightIndex = root.Value<int?>("lightIndex");
                if (lightIndex == null || lightIndex.Value < 0 || lightIndex.Value >= lighting.Lights.Count)
                {
                    return JsonConvert.SerializeObject(new { ok = false, error = "Invalid lightIndex" });
                }

                lighting.Remove(lighting.Lights[lightIndex.Value]);
                return JsonConvert.SerializeObject(new { ok = true });
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Runtime editor remove light failed");
                return JsonConvert.SerializeObject(new { ok = false, error = exception.Message });
            }
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
                var path = root.Value<string>("path") ?? root.Value<string>("member") ?? string.Empty;
                var value = root["value"];

                if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(path) || value == null)
                {
                    return JsonConvert.SerializeObject(new { ok = false, error = "Missing target/path/value" });
                }

                return target switch
                {
                    "entity" => ApplyEntityEdit(root, path, value),
                    "transform" => ApplyTransformEdit(root, path, value),
                    "component" => ApplyComponentEdit(root, path, value),
                    "lighting" => ApplyLightingEdit(path, value),
                    "light" => ApplyLightEdit(root, path, value),
                    "sprite" => ApplySpriteEdit(root, path, value),
                    _ => JsonConvert.SerializeObject(new { ok = false, error = $"Unknown target '{target}'" })
                };
            }
            catch (Exception exception)
            {
                Logger.Warn(exception, "Runtime editor apply failed");
                return JsonConvert.SerializeObject(new { ok = false, error = exception.Message });
            }
        }

        private string ApplyEntityEdit(JObject root, string path, JToken value)
        {
            var entity = ResolveEntityFromRequest(root);
            if (entity == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Entity not found" });
            }

            if (!TrySetPathValue(entity, path, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyTransformEdit(JObject root, string path, JToken value)
        {
            var entity = ResolveEntityFromRequest(root);
            if (entity == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Entity not found" });
            }

            if (!TrySetPathValue(entity.Transform, path, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyComponentEdit(JObject root, string path, JToken value)
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
            if (!TrySetPathValue(component, path, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyLightingEdit(string path, JToken value)
        {
            var lighting = SceneManager.GetCurrent().Lighting;
            if (!TrySetPathValue(lighting, path, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplyLightEdit(JObject root, string path, JToken value)
        {
            var scene = SceneManager.GetCurrent();
            var lighting = scene.Lighting;

            Light2D? light = null;
            var lightId = root.Value<int?>("lightId");
            if (lightId.HasValue)
            {
                light = TryResolveLight(lightId.Value);
            }
            else
            {
                var lightIndex = root.Value<int?>("lightIndex");
                if (lightIndex != null && lightIndex.Value >= 0 && lightIndex.Value < lighting.Lights.Count)
                {
                    light = lighting.Lights[lightIndex.Value];
                }
            }

            if (light == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Light not found" });
            }

            if (!TrySetPathValue(light, path, value, out var error))
            {
                return JsonConvert.SerializeObject(new { ok = false, error });
            }

            return JsonConvert.SerializeObject(new { ok = true });
        }

        private string ApplySpriteEdit(JObject root, string path, JToken value)
        {
            var spriteId = root.Value<int?>("spriteId");
            if (spriteId == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Missing spriteId" });
            }

            var sprite = TryResolveSprite(spriteId.Value);
            if (sprite == null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "Sprite not found" });
            }

            if (!TrySetPathValue(sprite, path, value, out var error))
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
            if (GetEntityId(root) == entityId)
            {
                return root;
            }

            foreach (var child in root.Children)
            {
                var found = TryResolveEntity(child, entityId);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private Sprite? TryResolveSprite(int spriteId)
        {
            EnsureSceneState();
            var scene = SceneManager.GetCurrent();

            foreach (var sprite in scene.MainLayer.Sprites)
            {
                if (GetSpriteId(sprite) == spriteId)
                {
                    return sprite;
                }
            }

            foreach (var sprite in scene.UiLayer.Sprites)
            {
                if (GetSpriteId(sprite) == spriteId)
                {
                    return sprite;
                }
            }

            return null;
        }

        private Light2D? TryResolveLight(int lightId)
        {
            var scene = SceneManager.GetCurrent();
            foreach (var light in scene.Lighting.Lights)
            {
                if (GetLightId(light) == lightId)
                {
                    return light;
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
                entityMembers = BuildMembersPayload(entity),
                transformMembers = BuildMembersPayload(entity.Transform),
                components
            };
        }

        private object BuildSpriteNode(Sprite sprite)
        {
            return new
            {
                id = GetSpriteId(sprite),
                name = string.IsNullOrWhiteSpace(sprite.Name) ? "(unnamed)" : sprite.Name,
                sorting = sprite.Sorting,
                visible = sprite.Visible
            };
        }

        private IEnumerable<object> BuildMembersPayload(object target)
        {
            return RuntimeEditorMemberIntrospector.Instance.BuildMembersPayload(target);
        }

        private static bool TrySetPathValue(object target, string path, JToken token, out string error)
        {
            return RuntimeEditorPathValueSetter.TrySetPathValue(target, path, token, out error);
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

        private int GetSpriteId(Sprite sprite)
        {
            if (_spriteIds.TryGetValue(sprite, out var existing))
            {
                return existing;
            }

            var nextId = _spriteIds.Count + 1;
            _spriteIds[sprite] = nextId;
            return nextId;
        }

        private int GetLightId(Light2D light)
        {
            if (_lightIds.TryGetValue(light, out var existing))
            {
                return existing;
            }

            var nextId = _lightIds.Count + 1;
            _lightIds[light] = nextId;
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

}
