using System.Collections.Generic;

namespace IsometricMagic.Engine.Diagnostics
{
    public sealed class DebugOverlayService
    {
        private static readonly DebugOverlayService Instance = new();
        private static readonly FrameStats Stats = FrameStats.GetInstance();

        private readonly List<string> _lines = new();
        private bool _initialized;
        private bool _enabled;
        private bool _visible;
        private Key _toggleKey = Key.F3;
        private float _refreshInterval = 0.25f;
        private float _refreshTimer;

        private string _fontPath = "./resources/engine/vt323-regular.ttf";
        private int _fontSize = 20;
        private int _posX = 12;
        private int _posY = 12;

        public static DebugOverlayService GetInstance()
        {
            return Instance;
        }

        public bool Visible => _enabled && _visible;
        public bool Enabled => _enabled;
        public IReadOnlyList<string> Lines => _lines;
        public string FontPath => _fontPath;
        public int FontSize => _fontSize;
        public int PosX => _posX;
        public int PosY => _posY;

        public void Initialize(AppConfig config)
        {
            _enabled = config.DebugOverlayEnabled;
            _visible = config.DebugOverlayEnabledByDefault;
            _toggleKey = config.DebugOverlayToggleKey;
            _fontPath = config.DebugOverlayFontPath;
            _fontSize = config.DebugOverlayFontSize;
            _posX = config.DebugOverlayPosX;
            _posY = config.DebugOverlayPosY;
            _refreshInterval = 1f / config.DebugOverlayRefreshHz;
            _refreshTimer = _refreshInterval;
            _initialized = true;

            BuildLines();
        }

        public void Update(float dt)
        {
            if (!_initialized || !_enabled)
            {
                return;
            }

            if (Input.WasPressed(_toggleKey))
            {
                _visible = !_visible;
                _refreshTimer = _refreshInterval;
            }

            if (!_visible)
            {
                return;
            }

            _refreshTimer += dt;
            if (_refreshTimer < _refreshInterval)
            {
                return;
            }

            _refreshTimer = 0f;
            BuildLines();
        }

        private void BuildLines()
        {
            _lines.Clear();
            _lines.Add("DEBUG PANEL");
            _lines.Add($"FPS: {Stats.FpsAvg:0.0}   Frame: {Stats.FrameMs:0.00} ms (avg {Stats.FrameMsAvg:0.00})");
            _lines.Add($"DrawCalls: {Stats.DrawCalls}   Drawn: {Stats.SpritesDrawn}   Culled: {Stats.SpritesCulled}");
            _lines.Add($"Viewport: {Stats.ViewportWidth}x{Stats.ViewportHeight}");
            _lines.Add($"Backend: {Stats.Backend}   VSync: {(Stats.VSync ? "On" : "Off")}");
            _lines.Add($"Scene: {Stats.SceneName}");
            _lines.Add($"Toggle: {_toggleKey}");
        }
    }
}
