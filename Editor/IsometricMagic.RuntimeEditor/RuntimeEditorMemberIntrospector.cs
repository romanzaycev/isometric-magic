using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

using IsometricMagic.Engine.Diagnostics;

namespace IsometricMagic.RuntimeEditor
{
    internal sealed class RuntimeEditorMemberIntrospector
    {
        public static readonly RuntimeEditorMemberIntrospector Instance = new();

        public IEnumerable<object> BuildMembersPayload(object target)
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
                Min = NormalizeLimit(editableAttribute?.Min),
                Max = NormalizeLimit(editableAttribute?.Max),
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

        private static double? NormalizeLimit(double? value)
        {
            if (!value.HasValue || double.IsNaN(value.Value))
            {
                return null;
            }

            return value.Value;
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
}
