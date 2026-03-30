using System.Collections;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Lighting;
using IsometricMagic.Game.Prefabs;
using IsometricMagic.Game.Scenes.IsoTestPrefabs;

namespace IsometricMagic.Game.Scenes
{
    public class IsoTest : Scene
    {
        public IsoTest() : base("iso_test", true)
        {
        }

        protected override IEnumerator InitializeAsync()
        {
            IsoMapInstance mapInstance = default;
            var mapPrefab = new IsoMapPrefab(new IsoMapPrefabSpec("map1"));
            yield return mapPrefab.InstantiateAsync(this, instance => mapInstance = instance);

            var collisionWorldPrefab = new CollisionWorldPrefab(new CollisionWorldPrefabSpec(
                EntityName: "CollisionWorld",
                CellSize: 128,
                ContactSkin: 0.1f
            ));
            collisionWorldPrefab.Instantiate(this);
            yield return true;

            var playerPrefab = new HumanoidPlayerPrefab(new HumanoidPlayerPrefabSpec(
                EntityName: "Player",
                WorldPosX: 470,
                WorldPosY: 470,
                WorldLayerBase: mapInstance.WorldLayerBase
            ));
            playerPrefab.Instantiate(this);
            yield return true;

            var stonePrefab = new StoneWithMagicPrefab(new StoneWithMagicPrefabSpec(
                StoneEntityName: "stone0",
                WorldPosition: new Vector2(410, 410),
                WorldLayerBase: mapInstance.WorldLayerBase,
                EmissionColor: new Vector3(0.37f, 0.81f, 0.51f)
            ));
            stonePrefab.Instantiate(this);
            yield return true;

            var movingLightPrefab = new MovingLightPrefab(new MovingLightPrefabSpec(
                EntityName: "MovingLight",
                OrbitCenterCanvas: mapInstance.Converter.GetCanvasPosition(new Vector2(600, 600)),
                OrbitRadius: 300f,
                OrbitSpeed: 0.8f,
                LayerBase: mapInstance.WorldLayerBase + mapInstance.LayerStride
            ));
            movingLightPrefab.Instantiate(this);
            yield return true;

            Lighting.AmbientIntensity = 0.45f;
            Lighting.Add(
                new Light2D(mapInstance.Converter.GetCanvasPosition(new Vector2(410, 410)))
                {
                    Intensity = 5f,
                    Radius = 512f,
                    Height = 2f,
                    Falloff = 2f,
                    InnerRadius = 64f,
                    CenterAttenuation = 0.1f,
                    Color = new Vector3(0.37f, 0.81f, 0.51f),
                }
            );

            PostProcess.Add(new BloomEffect
            {
                Threshold = 1.2f,
                Knee = 0.6f,
                Intensity = 0.95f,
                BlurIterations = 4
            });
            PostProcess.Add(new ToneMapEffect
            {
                Exposure = 0.6f,
                Gamma = 1.3f,
                Contrast = 1.05f,
            });
            PostProcess.Add(new VignetteEffect
            {
                Intensity = 0.2f
            });
        }

        public override void Update()
        {
        }

        protected override void DeInitialize()
        {
        }
    }
}
