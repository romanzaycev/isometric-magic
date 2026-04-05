using System.Globalization;
using System.Numerics;
using System.Reflection;

using IsometricMagic.Engine.Diagnostics;

using Newtonsoft.Json.Linq;

namespace IsometricMagic.RuntimeEditor
{
    internal static class RuntimeEditorPathValueSetter
    {
        public static bool TrySetPathValue(object target, string path, JToken token, out string error)
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

            if (segmentIndex == segments.Length - 1)
            {
                var editableAttribute = field?.GetCustomAttribute<RuntimeEditorEditableAttribute>()
                    ?? property?.GetCustomAttribute<RuntimeEditorEditableAttribute>();
                var inspectableAttribute = targetType.GetCustomAttribute<RuntimeEditorInspectableAttribute>();
                var canEditByPolicy = editableAttribute != null || (inspectableAttribute?.EditableByDefault ?? false);
                if (!canEditByPolicy)
                {
                    error = $"Member '{segment}' is read-only in runtime editor";
                    return false;
                }

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

            var nestedTargetType = memberValue.GetType();
            if (!TrySetPathValueRecursive(memberValue, nestedTargetType, segments, segmentIndex + 1, token, out error))
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
    }
}
