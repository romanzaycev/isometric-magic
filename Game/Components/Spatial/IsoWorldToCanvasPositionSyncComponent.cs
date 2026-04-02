using IsometricMagic.Game.Components.Actor;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Components.Spatial
{
    public class IsoWorldToCanvasPositionSyncComponent : Component
    {
        private static readonly IEngineLogger Logger = Log.For<IsoWorldToCanvasPositionSyncComponent>();

        private IsoWorldPositionComponent? _isoWorldPosition;
        private CanvasPositionComponent? _canvasPosition;
        private MotorComponent? _motor;
        private IsoWorldPositionConverter? _converter;
        private bool _converterResolved;
        private bool _missingConverterWarned;

        public override ComponentUpdateGroup UpdateGroup => ComponentUpdateGroup.Early;

        public override int UpdateOrder => 100;

        public void SetConverter(IsoWorldPositionConverter converter)
        {
            _converter = converter;
            _converterResolved = true;
            _missingConverterWarned = false;
        }

        protected override void Awake()
        {
            ResolveDependencies();
        }

        protected override void Update(float dt)
        {
            ResolveDependencies();

            if (_isoWorldPosition == null || _canvasPosition == null)
            {
                return;
            }

            EnsureConverter();
            if (_converter == null)
            {
                if (!_missingConverterWarned)
                {
                    var entityName = Entity?.Name ?? "unknown";
                    Logger.Warn($"IsoWorldToCanvasPositionSyncComponent on '{entityName}' has no converter. Position sync skipped.");
                    _missingConverterWarned = true;
                }
                return;
            }

            var sourcePosition = _motor?.PreciseWorldPosition ?? _isoWorldPosition.Position;
            _canvasPosition.Position = _converter.ToCanvas(sourcePosition);
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

        private void ResolveDependencies()
        {
            if (_isoWorldPosition == null)
            {
                _isoWorldPosition = Entity?.GetComponent<IsoWorldPositionComponent>();
            }

            if (_canvasPosition == null)
            {
                _canvasPosition = Entity?.GetComponent<CanvasPositionComponent>();
            }

            if (_motor == null)
            {
                _motor = Entity?.GetComponent<MotorComponent>();
            }
        }
    }
}
