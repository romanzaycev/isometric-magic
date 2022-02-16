using System;

namespace IsometricMagic.Engine
{
    public class NativeTexture
    {
        public IntPtr Holder { get; }

        public NativeTexture(IntPtr holder)
        {
            Holder = holder;
        }
    }
}