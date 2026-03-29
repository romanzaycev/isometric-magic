using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using IsometricMagic.Game.Components.Spatial;

namespace IsometricMagic.Game.Components.Camera
{
    public class CameraFollowComponent : CameraInfluenceComponent
    {
        public int MinX { get; set; } = -200;
        public int MinY { get; set; } = -200;
        public int CenterYOffset { get; set; } = -100;

        private WorldPositionComponent? _positionComponent;
        private IsoWorldPositionConverter? _converter;
        private Vector2 _targetCenter;
        private bool _hasTarget;

        public CameraFollowComponent()
        {
            Priority = 100;
        }

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
        }

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<WorldPositionComponent>();
        }

        protected override void Update(float dt)
        {
            if (_positionComponent == null || _converter == null) return;
            
            var pos = _converter.GetCanvasPosition(_positionComponent.WorldPosX, _positionComponent.WorldPosY);
            _targetCenter = new Vector2(pos.X, pos.Y + CenterYOffset);
            _hasTarget = true;
        }

        public override void CollectInfluence(List<CameraInfluence> buffer)
        {
            if (!_hasTarget) return;

            var camera = Application.GetInstance().GetRenderer().GetCamera();
            var rect = camera.Rect;
            var nextX = _targetCenter.X - rect.Width / 2f;
            var nextY = _targetCenter.Y - rect.Height / 2f;

            if (nextX < MinX)
            {
                _targetCenter.X = MinX + rect.Width / 2f;
            }

            if (nextY < MinY)
            {
                _targetCenter.Y = MinY + rect.Height / 2f;
            }

            AddSetCenter(buffer, _targetCenter, Priority);
        }
    }
}
