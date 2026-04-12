using System.Numerics;
using IonMotion.Engine.Assets;

namespace IonMotion.Engine.Graphics.Materials
{
    public enum SpriteShadingModel
    {
        Unlit,
        Lit,
    }

    public enum SpriteNormalMapMode
    {
        None,
        UseMaterial,
        AutoFromAlbedo,
        Neutral,
    }

    public interface ISpriteMaterialCapabilities : IMaterial
    {
        SpriteShadingModel ShadingModel { get; }
        SpriteNormalMapMode NormalMapMode { get; }
        Texture? NormalMapTexture { get; }
        Texture? EmissionMapTexture { get; }
        Vector3 EmissionColor { get; }
        float EmissionIntensity { get; }
    }
}
