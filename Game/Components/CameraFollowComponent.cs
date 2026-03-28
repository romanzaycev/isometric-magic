using System.Numerics;
using IsometricMagic.Engine;
using IsometricMagic.Game.Controllers.Camera;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components
{
    public class CameraFollowComponent : Component
    {
        private readonly LookAtController _controller = new();
        private WorldPositionComponent? _positionComponent;
        private IsoWorldPositionConverter? _converter;

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
        }

        private static readonly Application AppInstance = Application.GetInstance();

        protected override void Awake()
        {
            _positionComponent = Entity?.GetComponent<WorldPositionComponent>();
            AppInstance.GetRenderer().GetCamera().SetController(_controller);
        }

        protected override void Update(float dt)
        {
            if (_positionComponent == null || _converter == null) return;

            var pos = _converter.GetCanvasPosition(_positionComponent.WorldPosX, _positionComponent.WorldPosY);
            _controller.SetPos(pos);
        }

        protected override void OnDestroy()
        {
            AppInstance.GetRenderer().GetCamera().SetController(null);
        }
    }
}
