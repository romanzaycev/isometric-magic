using System.Numerics;
using EngineTexture = IsometricMagic.Engine.Assets.Texture;

namespace IsometricMagic.Engine.Graphics.Materials
{
    public static class SpriteMaterialFactory
    {
        public static StandardSpriteMaterial Unlit()
        {
            return new StandardSpriteMaterial
            {
                ShadingModel = SpriteShadingModel.Unlit,
                NormalMapMode = SpriteNormalMapMode.None
            };
        }

        public static StandardSpriteMaterial LitAutoNormal()
        {
            return new StandardSpriteMaterial
            {
                ShadingModel = SpriteShadingModel.Lit,
                NormalMapMode = SpriteNormalMapMode.AutoFromAlbedo
            };
        }

        public static StandardSpriteMaterial LitNeutralNormal()
        {
            return new StandardSpriteMaterial
            {
                ShadingModel = SpriteShadingModel.Lit,
                NormalMapMode = SpriteNormalMapMode.Neutral
            };
        }

        public static StandardSpriteMaterial LitWithNormal(EngineTexture normalMapTexture)
        {
            return new StandardSpriteMaterial
            {
                ShadingModel = SpriteShadingModel.Lit,
                NormalMapMode = SpriteNormalMapMode.UseMaterial,
                NormalMapTexture = normalMapTexture
            };
        }

        public static StandardSpriteMaterial UnlitEmissive(
            Vector3 emissionColor,
            float emissionIntensity,
            EngineTexture? emissionMapTexture = null)
        {
            return new StandardSpriteMaterial
            {
                ShadingModel = SpriteShadingModel.Unlit,
                NormalMapMode = SpriteNormalMapMode.None,
                EmissionColor = emissionColor,
                EmissionIntensity = emissionIntensity,
                EmissionMapTexture = emissionMapTexture
            };
        }

        public static StandardSpriteMaterial LitEmissiveAutoNormal(
            Vector3 emissionColor,
            float emissionIntensity,
            EngineTexture? emissionMapTexture = null)
        {
            return new StandardSpriteMaterial
            {
                ShadingModel = SpriteShadingModel.Lit,
                NormalMapMode = SpriteNormalMapMode.AutoFromAlbedo,
                EmissionColor = emissionColor,
                EmissionIntensity = emissionIntensity,
                EmissionMapTexture = emissionMapTexture
            };
        }

        public static StandardSpriteMaterial LitEmissiveWithNormal(
            EngineTexture normalMapTexture,
            Vector3 emissionColor,
            float emissionIntensity,
            EngineTexture? emissionMapTexture = null)
        {
            return new StandardSpriteMaterial
            {
                ShadingModel = SpriteShadingModel.Lit,
                NormalMapMode = SpriteNormalMapMode.UseMaterial,
                NormalMapTexture = normalMapTexture,
                EmissionColor = emissionColor,
                EmissionIntensity = emissionIntensity,
                EmissionMapTexture = emissionMapTexture
            };
        }
    }
}
