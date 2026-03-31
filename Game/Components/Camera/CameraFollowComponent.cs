using System.Collections.Generic;
using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Components.Actor;
using IsometricMagic.Game.Components.Spatial;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Camera
{
    public class CameraFollowComponent : CameraInfluenceComponent
    {
        public int MinX { get; set; } = -200;
        public int MinY { get; set; } = -200;
        public int CenterYOffset { get; set; } = -100;

        private WorldPositionComponent? _positionComponent;
        private MotorComponent? _motorComponent;
        private IsoWorldPositionConverter? _converter;
        private bool _converterResolved;
        private Vector2 _targetCenter;
        private bool _hasTarget;

        public CameraFollowComponent()
        {
            Priority = 100;
        }

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
            _converterResolved = true;
        }

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<WorldPositionComponent>();
            _motorComponent = Entity?.GetComponent<MotorComponent>();
        }

        protected override void Update(float dt)
        {
            EnsureConverter();
            if (_positionComponent == null || _converter == null) return;
            
            var worldPos = _motorComponent?.PreciseWorldPosition ?? new Vector2(_positionComponent.WorldPosX, _positionComponent.WorldPosY);
            var pos = _converter.GetCanvasPosition(worldPos.X, worldPos.Y);
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

        private void EnsureConverter()
        {
            if (_converterResolved)
            {
                return;
            }

            _converterResolved = true;
            var provider = Scene?.FindComponent<WorldPositionConverterProviderComponent>();
            _converter = provider?.Converter;
        }
    }
}
