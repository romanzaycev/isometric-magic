using IsometricMagic.Engine;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Spatial
{
    public class WorldPositionConverterProviderComponent : Component
    {
        public virtual IsoWorldPositionConverter? Converter => null;
    }
}
