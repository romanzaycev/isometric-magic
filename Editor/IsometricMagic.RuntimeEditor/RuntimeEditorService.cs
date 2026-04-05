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
            var descriptors = BuildMemberDescriptors(target, null, new HashSet<object>(ReferenceEqualityComparer.Instance));
            return descriptors.Select(descriptor => descriptor.ToPayload());
        }

        private List<MemberDescriptor> BuildMemberDescriptors(object target, string? parentPath, HashSet<object> cycleGuard)
        {
            var members = new List<MemberDescriptor>();
            var type = target.GetType();

            if (!type.IsValueType)
            {
                cycleGuard.Add(target);
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var path = string.IsNullOrWhiteSpace(parentPath) ? field.Name : parentPath + "." + field.Name;
                members.Add(BuildMemberDescriptor(target, type, field, null, path, cycleGuard));
            }

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var path = string.IsNullOrWhiteSpace(parentPath) ? property.Name : parentPath + "." + property.Name;
                members.Add(BuildMemberDescriptor(target, type, null, property, path, cycleGuard));
            }

            members.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return members;
        }

        private MemberDescriptor BuildMemberDescriptor(
            object target,
            Type declaringType,
            FieldInfo? field,
            PropertyInfo? property,
            string path,
            HashSet<object> cycleGuard)
        {
            var memberType = field?.FieldType ?? property!.PropertyType;
            var name = field?.Name ?? property!.Name;
            var editableAttribute = GetEditableAttribute(field, property);
            var inspectableAttribute = declaringType.GetCustomAttribute<RuntimeEditorInspectableAttribute>();

            object? value = null;
            try
            {
                value = field != null ? field.GetValue(target) : property!.GetValue(target);
            }
            catch
            {
            }

            var writable = field != null || (property != null && property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic);
            var canEditByPolicy = editableAttribute != null || (inspectableAttribute?.EditableByDefault ?? false);

            var descriptor = new MemberDescriptor
            {
                Name = name,
                Path = path,
                Type = ToFriendlyTypeName(memberType),
                Editable = writable && canEditByPolicy && IsSupportedSimpleType(memberType),
                Value = SerializeValue(value, memberType),
                EnumValues = GetEnumValues(memberType),
                Step = ResolveStep(memberType, editableAttribute),
                Min = editableAttribute?.Min,
                Max = editableAttribute?.Max,
                Children = null
            };

            if (!IsSupportedSimpleType(memberType)
                && value != null
                && ShouldExpandMember(memberType)
                && (memberType.IsValueType || !cycleGuard.Contains(value)))
            {
                descriptor.Children = BuildMemberDescriptors(value, path, cycleGuard)
                    .Select(child => child.ToPayload())
                    .ToList();
            }

            return descriptor;
        }

        private static RuntimeEditorEditableAttribute? GetEditableAttribute(FieldInfo? field, PropertyInfo? property)
        {
            if (field != null)
            {
                return field.GetCustomAttribute<RuntimeEditorEditableAttribute>();
            }

            return property?.GetCustomAttribute<RuntimeEditorEditableAttribute>();
        }

        private static bool ShouldExpandMember(Type type)
        {
            if (type.IsValueType)
            {
                return !type.IsPrimitive && !type.IsEnum;
            }

            return type.GetCustomAttribute<RuntimeEditorInspectableAttribute>() != null;
        }

        private static string ToFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(Nullable<>))
                {
                    return ToFriendlyTypeName(type.GetGenericArguments()[0]) + "?";
                }
            }

            return type.Name;
        }

        private static object? SerializeValue(object? value, Type type)
        {
            if (value == null)
            {
                return null;
            }

            var baseType = Nullable.GetUnderlyingType(type) ?? type;
            if (baseType == typeof(Vector2))
            {
                var vector = (Vector2)value;
                return new { x = vector.X, y = vector.Y };
            }

            if (baseType == typeof(Vector3))
            {
                var vector = (Vector3)value;
                return new { x = vector.X, y = vector.Y, z = vector.Z };
            }

            if (baseType == typeof(Vector4))
            {
                var vector = (Vector4)value;
                return new { x = vector.X, y = vector.Y, z = vector.Z, w = vector.W };
            }

            if (baseType.IsEnum)
            {
                return value.ToString();
            }

            if (IsSupportedSimpleType(type))
            {
                return value;
            }

            return value.ToString();
        }

        private static bool IsSupportedSimpleType(Type type)
        {
            var baseType = Nullable.GetUnderlyingType(type) ?? type;
            if (baseType.IsEnum)
            {
                return true;
            }

            return baseType == typeof(bool)
                || baseType == typeof(int)
                || baseType == typeof(uint)
                || baseType == typeof(long)
                || baseType == typeof(float)
                || baseType == typeof(double)
                || baseType == typeof(string)
                || baseType == typeof(Vector2)
                || baseType == typeof(Vector3)
                || baseType == typeof(Vector4);
        }

        private static double? ResolveStep(Type type, RuntimeEditorEditableAttribute? attribute)
        {
            if (attribute != null && !double.IsNaN(attribute.Step))
            {
                return attribute.Step;
            }

            var baseType = Nullable.GetUnderlyingType(type) ?? type;
            if (baseType == typeof(float) || baseType == typeof(double))
            {
                return 0.1;
            }

            if (baseType == typeof(int) || baseType == typeof(uint) || baseType == typeof(long))
            {
                return 1;
            }

            return null;
        }

        private static string[]? GetEnumValues(Type type)
        {
            var baseType = Nullable.GetUnderlyingType(type) ?? type;
            if (!baseType.IsEnum)
            {
                return null;
            }

            return Enum.GetNames(baseType);
        }

        private static bool TrySetPathValue(object target, string path, JToken token, out string error)
        {
            error = string.Empty;
            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                error = "Invalid empty path";
                return false;
            }

            return TrySetPathValueRecursive(target, target.GetType(), segments, 0, token, out error);
        }

        private static bool TrySetPathValueRecursive(
            object target,
            Type targetType,
            string[] segments,
            int segmentIndex,
            JToken token,
            out string error)
        {
            error = string.Empty;

            var segment = segments[segmentIndex];
            if (!TryGetWritableMember(targetType, segment, out var field, out var property, out var memberType))
            {
                error = $"Member '{segment}' not found or not writable";
                return false;
            }

            var editableAttribute = field?.GetCustomAttribute<RuntimeEditorEditableAttribute>()
                ?? property?.GetCustomAttribute<RuntimeEditorEditableAttribute>();
            var inspectableAttribute = targetType.GetCustomAttribute<RuntimeEditorInspectableAttribute>();
            var canEditByPolicy = editableAttribute != null || (inspectableAttribute?.EditableByDefault ?? false);

            if (!canEditByPolicy)
            {
                error = $"Member '{segment}' is read-only in runtime editor";
                return false;
            }

            if (segmentIndex == segments.Length - 1)
            {
                if (!IsSupportedSimpleType(memberType))
                {
                    error = $"Unsupported member type '{memberType.Name}'";
                    return false;
                }

                if (!TryConvertValue(token, memberType, out var converted, out error))
                {
                    return false;
                }

                SetMemberValue(target, field, property, converted);
                return true;
            }

            object? memberValue;
            try
            {
                memberValue = GetMemberValue(target, field, property);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (memberValue == null)
            {
                error = $"Member '{segment}' is null";
                return false;
            }

            if (!TrySetPathValueRecursive(memberValue, memberType, segments, segmentIndex + 1, token, out error))
            {
                return false;
            }

            if (memberType.IsValueType)
            {
                SetMemberValue(target, field, property, memberValue);
            }

            return true;
        }

        private static bool TryGetWritableMember(
            Type targetType,
            string memberName,
            out FieldInfo? field,
            out PropertyInfo? property,
            out Type memberType)
        {
            field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                property = null;
                memberType = field.FieldType;
                return true;
            }

            property = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic)
            {
                memberType = property.PropertyType;
                return true;
            }

            memberType = typeof(void);
            return false;
        }

        private static object? GetMemberValue(object target, FieldInfo? field, PropertyInfo? property)
        {
            if (field != null)
            {
                return field.GetValue(target);
            }

            return property!.GetValue(target);
        }

        private static void SetMemberValue(object target, FieldInfo? field, PropertyInfo? property, object? value)
        {
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            property!.SetValue(target, value);
        }

        private static bool TryConvertValue(JToken token, Type targetType, out object? value, out string error)
        {
            error = string.Empty;
            value = null;

            var nullableType = Nullable.GetUnderlyingType(targetType);
            var baseType = nullableType ?? targetType;
            if (nullableType != null && token.Type == JTokenType.Null)
            {
                return true;
            }

            try
            {
                if (baseType == typeof(bool))
                {
                    value = token.Value<bool>();
                    return true;
                }

                if (baseType == typeof(int))
                {
                    value = token.Value<int>();
                    return true;
                }

                if (baseType == typeof(uint))
                {
                    value = token.Value<uint>();
                    return true;
                }

                if (baseType == typeof(long))
                {
                    if (token.Type == JTokenType.String)
                    {
                        var text = token.Value<string>() ?? string.Empty;
                        if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
                        {
                            error = "Invalid Int64 value";
                            return false;
                        }

                        value = parsedLong;
                        return true;
                    }

                    value = token.Value<long>();
                    return true;
                }

                if (baseType == typeof(float))
                {
                    value = token.Value<float>();
                    return true;
                }

                if (baseType == typeof(double))
                {
                    value = token.Value<double>();
                    return true;
                }

                if (baseType == typeof(string))
                {
                    value = token.Value<string>() ?? string.Empty;
                    return true;
                }

                if (baseType == typeof(Vector2))
                {
                    if (token is not JObject obj)
                    {
                        error = "Vector2 expects object with x and y";
                        return false;
                    }

                    value = new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
                    return true;
                }

                if (baseType == typeof(Vector3))
                {
                    if (token is not JObject obj)
                    {
                        error = "Vector3 expects object with x, y and z";
                        return false;
                    }

                    value = new Vector3(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"));
                    return true;
                }

                if (baseType == typeof(Vector4))
                {
                    if (token is not JObject obj)
                    {
                        error = "Vector4 expects object with x, y, z and w";
                        return false;
                    }

                    value = new Vector4(
                        obj.Value<float>("x"),
                        obj.Value<float>("y"),
                        obj.Value<float>("z"),
                        obj.Value<float>("w"));
                    return true;
                }

                if (baseType.IsEnum)
                {
                    var text = token.Value<string>();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        error = $"Enum '{baseType.Name}' expects non-empty string";
                        return false;
                    }

                    if (!Enum.TryParse(baseType, text, true, out var parsedEnum))
                    {
                        error = $"Cannot parse '{text}' as {baseType.Name}";
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

            error = $"Unsupported type '{baseType.Name}'";
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

        private sealed class MemberDescriptor
        {
            public string Name { get; init; } = string.Empty;

            public string Path { get; init; } = string.Empty;

            public string Type { get; init; } = string.Empty;

            public bool Editable { get; init; }

            public object? Value { get; init; }

            public double? Step { get; init; }

            public string[]? EnumValues { get; init; }

            public double? Min { get; init; }

            public double? Max { get; init; }

            public List<object>? Children { get; set; }

            public object ToPayload()
            {
                return new
                {
                    name = Name,
                    path = Path,
                    type = Type,
                    editable = Editable,
                    value = Value,
                    enumValues = EnumValues,
                    step = Step,
                    min = Min,
                    max = Max,
                    children = Children
                };
            }
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();

            public new bool Equals(object? x, object? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
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

    internal static class RuntimeEditorPage
    {
        private static readonly Lazy<string> HtmlContent = new(LoadHtml);

        public static string Html => HtmlContent.Value;

        private static string LoadHtml()
        {
            var assembly = typeof(RuntimeEditorPage).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("Web.dist.index.html", StringComparison.Ordinal));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return "<!doctype html><html><body><h1>Runtime Editor</h1><p>Missing embedded SPA.</p></body></html>";
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return "<!doctype html><html><body><h1>Runtime Editor</h1><p>Missing embedded SPA stream.</p></body></html>";
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
