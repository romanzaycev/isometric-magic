using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Engine.Graphics.Materials;
using IsometricMagic.Game.Animation;
using IsometricMagic.Game.Components.Spatial;

namespace IsometricMagic.Game.Components.Vfx.Light;

public class FireCircleComponent : Component
{
    private WorldPositionComponent? _worldPosition;
    
    public SceneLayer? TargetLayer { get; set; }
    public int Sorting { get; set; } = 1100;
    
    private Sequence? _sequence;
    private readonly List<Sprite> _sprites = new();

    protected override void Awake()
    {
        if (TargetLayer == null) return;

        _worldPosition = Entity?.GetComponent<WorldPositionComponent>();
        
        const int totalFrames = 61;
        const string animationPath = "./resources/data/textures/vfx/fire_circle_01/fire_circle_{0}.png";
        
        for (var i = 0; i < totalFrames; i++)
        {
            var tex = new Texture(50, 50);
            var sprite = new Sprite
            {
                Width = 50,
                Height = 50,
                Texture = tex,
                OriginPoint = OriginPoint.Centered,
                Sorting = 2000,
            };
            tex.LoadImage(string.Format(animationPath, i.ToString().PadLeft(3, '0')));
            sprite.Material = new EmissiveNormalMappedLitSpriteMaterial
            {
                EmissionColor = new Vector3(1f, 0.6f, 0.2f),
                EmissionIntensity = 2.5f
            };
            //sprite.Visible = false;

            _sprites.Add(sprite);
            TargetLayer.Add(sprite);
        }

        _sequence = new Sequence("circle", _sprites) { FrameDelay = 0.08f };
        _sequence.Play();
    }

    protected override void Update(float dt)
    {
        if (_sequence != null)
        {
            _sequence.Update(dt);
            
            if (_worldPosition != null)
            {
                _sequence.CurrentSprite.Position = new Vector2(_worldPosition.WorldPosX, _worldPosition.WorldPosY);
            }
        }
    }
}
