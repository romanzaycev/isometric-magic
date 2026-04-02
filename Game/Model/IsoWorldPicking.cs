namespace IsometricMagic.Game.Model
{
    public static class IsoWorldPicking
    {
        public static IsoWorldPosition ToIsoWorld(IsoWorldPositionConverter converter, CanvasPosition canvasPosition)
        {
            return converter.ToIsoWorld(canvasPosition);
        }
    }
}
