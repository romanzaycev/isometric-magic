using System;

namespace IsometricMagic.Engine.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RuntimeEditorInspectableAttribute : Attribute
    {
        public bool EditableByDefault { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RuntimeEditorEditableAttribute : Attribute
    {
        public double Step { get; set; } = double.NaN;

        public double Min { get; set; } = double.NaN;

        public double Max { get; set; } = double.NaN;
    }
}
