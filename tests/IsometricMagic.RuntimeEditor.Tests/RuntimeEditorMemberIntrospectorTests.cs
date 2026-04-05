using System.Numerics;

using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.RuntimeEditor;

using Newtonsoft.Json.Linq;

using Xunit;

namespace IsometricMagic.RuntimeEditor.Tests
{
    public sealed class RuntimeEditorMemberIntrospectorTests
    {
        [Fact]
        public void BuildMembersPayload_MapsNaNBoundsToNull()
        {
            var target = new BoundsTarget();

            var payload = RuntimeEditorMemberIntrospector.Instance.BuildMembersPayload(target)
                .Select(JObject.FromObject)
                .Single(x => x.Value<string>("path") == nameof(BoundsTarget.Speed));

            Assert.Equal(JTokenType.Null, payload["min"]?.Type);
            Assert.Equal(JTokenType.Null, payload["max"]?.Type);
            Assert.Equal(0.25d, payload.Value<double>("step"));
        }

        [Fact]
        public void BuildMembersPayload_ProvidesEnumValuesAndEditableFlag()
        {
            var target = new EnumTarget
            {
                Mode = TestMode.Beta
            };

            var payload = RuntimeEditorMemberIntrospector.Instance.BuildMembersPayload(target)
                .Select(JObject.FromObject)
                .Single(x => x.Value<string>("path") == nameof(EnumTarget.Mode));

            Assert.True(payload.Value<bool>("editable"));
            Assert.Equal(nameof(TestMode.Beta), payload.Value<string>("value"));

            var enumValues = payload["enumValues"] as JArray;
            Assert.NotNull(enumValues);
            Assert.Contains(nameof(TestMode.Alpha), enumValues!.Select(x => x.Value<string>()));
            Assert.Contains(nameof(TestMode.Beta), enumValues.Select(x => x.Value<string>()));
        }

        [Fact]
        public void BuildMembersPayload_SerializesVectorValue()
        {
            var target = new VectorTarget
            {
                Position = new Vector3(1f, 2f, 3f)
            };

            var payload = RuntimeEditorMemberIntrospector.Instance.BuildMembersPayload(target)
                .Select(JObject.FromObject)
                .Single(x => x.Value<string>("path") == nameof(VectorTarget.Position));

            var value = payload["value"] as JObject;
            Assert.NotNull(value);
            Assert.Equal(1f, value!.Value<float>("x"));
            Assert.Equal(2f, value.Value<float>("y"));
            Assert.Equal(3f, value.Value<float>("z"));
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private sealed class BoundsTarget
        {
            [RuntimeEditorEditable(Step = 0.25)]
            public float Speed = 2f;
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private sealed class EnumTarget
        {
            [RuntimeEditorEditable]
            public TestMode Mode;
        }

        private enum TestMode
        {
            Alpha,
            Beta
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private sealed class VectorTarget
        {
            [RuntimeEditorEditable]
            public Vector3 Position;
        }
    }
}
