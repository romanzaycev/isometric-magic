using IonMotion.Game.Model;

namespace IonMotion.Game.Components.Spatial
{
    public class WorldPositionConverterProviderComponent : Component
    {
        public virtual IsoWorldPositionConverter? Converter => null;
    }
}
