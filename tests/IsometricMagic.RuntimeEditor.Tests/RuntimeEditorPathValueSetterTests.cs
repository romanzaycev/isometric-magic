using System.Numerics;

using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.RuntimeEditor;

using Newtonsoft.Json.Linq;

using Xunit;

namespace IsometricMagic.RuntimeEditor.Tests
{
    public sealed class RuntimeEditorPathValueSetterTests
    {
        [Fact]
        public void TrySetPathValue_AllowsInt64FromString()
        {
            var target = new LongTarget();

            var ok = RuntimeEditorPathValueSetter.TrySetPathValue(target, nameof(LongTarget.Counter), JToken.FromObject("42"), out var error);

            Assert.True(ok, error);
            Assert.Equal(42L, target.Counter);
        }

        [Fact]
        public void TrySetPathValue_UpdatesNestedStructPath()
        {
            var target = new StructRoot
            {
                Data = new TransformData
                {
                    Position = Vector2.Zero
                }
            };

            var ok = RuntimeEditorPathValueSetter.TrySetPathValue(target, "Data.Position", JObject.FromObject(new { x = 3f, y = -2f }), out var error);

            Assert.True(ok, error);
            Assert.Equal(new Vector2(3f, -2f), target.Data.Position);
        }

        [Fact]
        public void TrySetPathValue_FailsForReadonlyByPolicyMember()
        {
            var target = new ReadOnlyByPolicyTarget();

            var ok = RuntimeEditorPathValueSetter.TrySetPathValue(target, nameof(ReadOnlyByPolicyTarget.Value), JToken.FromObject(7), out var error);

            Assert.False(ok);
            Assert.Contains("read-only", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TrySetPathValue_FailsForUnsupportedType()
        {
            var target = new UnsupportedTypeTarget();

            var ok = RuntimeEditorPathValueSetter.TrySetPathValue(target, nameof(UnsupportedTypeTarget.Id), JToken.FromObject(Guid.NewGuid().ToString()), out var error);

            Assert.False(ok);
            Assert.Contains("Unsupported", error, StringComparison.OrdinalIgnoreCase);
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private sealed class LongTarget
        {
            [RuntimeEditorEditable]
            public long Counter = 0L;
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private sealed class StructRoot
        {
            [RuntimeEditorEditable]
            public TransformData Data;
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private struct TransformData
        {
            [RuntimeEditorEditable]
            public Vector2 Position;
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private sealed class ReadOnlyByPolicyTarget
        {
            public int Value = 0;
        }

        [RuntimeEditorInspectable(EditableByDefault = false)]
        private sealed class UnsupportedTypeTarget
        {
            [RuntimeEditorEditable]
            public Guid Id = Guid.Empty;
        }
    }
}
