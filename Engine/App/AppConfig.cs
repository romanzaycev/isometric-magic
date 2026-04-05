using System;
using IniParser;
using IniParser.Model;
using IsometricMagic.Engine.Graphics;
using IsometricMagic.Engine.Inputs;

namespace IsometricMagic.Engine.App
{
    public class AppConfig
    {
        private readonly IniData _data;

        private const string DefaultLogLayout =
            "${longdate}|${uppercase:${level}}|${logger}|${message}${onexception:inner= ${exception:format=tostring}}";

        private int _windowWidth = 0;
        
        public int WindowWidth {
            get {
                if (_windowWidth == 0)
                {
                    _windowWidth = GetInt(_data["Window"]["Width"], 800);
                }

                return _windowWidth;
            }
        }

        private int _windowHeight = 0;

        public int WindowHeight
        {
            get
            {
                if (_windowHeight == 0)
                {
                    _windowHeight = GetInt(_data["Window"]["Height"], 600);
                }

                return _windowHeight;
            }
        }

        private int _targetFps = -1;

        public int TargetFps
        {
            get
            {
                if (_targetFps == -1)
                {
                    _targetFps = GetInt(_data["Engine"]["TargetFPS"], 60);
                }

                return _targetFps;
            }
        }

        private bool _vSyncFetched = false;
        private bool _vSync = false;

        public bool VSync
        {
            get
            {
                if (!_vSyncFetched)
                {
                    _vSync = GetBool(_data["Engine"]["VSync"], false);
                    _vSyncFetched = true;
                }

                return _vSync;
            }
        }

        private bool _isFullscreenFetched = false;
        private bool _isFullscreen = false;

        public bool IsFullscreen
        {
            get
            {
                if (!_isFullscreenFetched)
                {
                    _isFullscreen = GetBool(_data["Window"]["Fullscreen"], false);
                    _isFullscreenFetched = true;
                }

                return _isFullscreen;
            }
        }

        private bool _graphicsBackendFetched = false;
        private GraphicsBackend _graphicsBackend = GraphicsBackend.OpenGL;

        public GraphicsBackend GraphicsBackend
        {
            get
            {
                if (!_graphicsBackendFetched)
                {
                    _graphicsBackend = GetGraphicsBackend(_data["Graphics"]["Backend"], GraphicsBackend.OpenGL);
                    _graphicsBackendFetched = true;
                }

                return _graphicsBackend;
            }
        }

        private bool _loggingEnabledFetched = false;
        private bool _loggingEnabled;

        public bool LoggingEnabled
        {
            get
            {
                if (!_loggingEnabledFetched)
                {
                    _loggingEnabled = GetBool(GetValue("Logging", "Enabled"), true);
                    _loggingEnabledFetched = true;
                }

                return _loggingEnabled;
            }
        }

        private bool _loggingAllEnabledFetched = false;
        private bool _loggingAllEnabled;

        public bool LoggingAllEnabled
        {
            get
            {
                if (!_loggingAllEnabledFetched)
                {
                    _loggingAllEnabled = GetBool(GetValue("Logging", "AllEnabled"), true);
                    _loggingAllEnabledFetched = true;
                }

                return _loggingAllEnabled;
            }
        }

        private bool _loggingWarnEnabledFetched = false;
        private bool _loggingWarnEnabled;

        public bool LoggingWarnEnabled
        {
            get
            {
                if (!_loggingWarnEnabledFetched)
                {
                    _loggingWarnEnabled = GetBool(GetValue("Logging", "WarnEnabled"), true);
                    _loggingWarnEnabledFetched = true;
                }

                return _loggingWarnEnabled;
            }
        }

        private bool _loggingErrorEnabledFetched = false;
        private bool _loggingErrorEnabled;

        public bool LoggingErrorEnabled
        {
            get
            {
                if (!_loggingErrorEnabledFetched)
                {
                    _loggingErrorEnabled = GetBool(GetValue("Logging", "ErrorEnabled"), true);
                    _loggingErrorEnabledFetched = true;
                }

                return _loggingErrorEnabled;
            }
        }

        private bool _logDateFormatFetched = false;
        private string _logDateFormat = string.Empty;

        public string LoggingDateFormat
        {
            get
            {
                if (!_logDateFormatFetched)
                {
                    _logDateFormat = GetString(GetValue("Logging", "DateFormat"), "yyyyMMdd_HHmm");
                    _logDateFormatFetched = true;
                }

                return _logDateFormat;
            }
        }

        private bool _logLayoutFetched = false;
        private string _logLayout = string.Empty;

        public string LoggingLayout
        {
            get
            {
                if (!_logLayoutFetched)
                {
                    _logLayout = GetString(GetValue("Logging", "Layout"), DefaultLogLayout);
                    _logLayoutFetched = true;
                }

                return _logLayout;
            }
        }

        private bool _logAllPathFetched = false;
        private string _logAllPath = string.Empty;

        public string LoggingAllPath
        {
            get
            {
                if (!_logAllPathFetched)
                {
                    _logAllPath = GetString(GetValue("Logging", "AllPath"), "logs/{date}_all.log");
                    _logAllPathFetched = true;
                }

                return _logAllPath;
            }
        }

        private bool _logWarnPathFetched = false;
        private string _logWarnPath = string.Empty;

        public string LoggingWarnPath
        {
            get
            {
                if (!_logWarnPathFetched)
                {
                    _logWarnPath = GetString(GetValue("Logging", "WarnPath"), "logs/{date}_{level}.log");
                    _logWarnPathFetched = true;
                }

                return _logWarnPath;
            }
        }

        private bool _logErrorPathFetched = false;
        private string _logErrorPath = string.Empty;

        public string LoggingErrorPath
        {
            get
            {
                if (!_logErrorPathFetched)
                {
                    _logErrorPath = GetString(GetValue("Logging", "ErrorPath"), "logs/{date}_{level}.log");
                    _logErrorPathFetched = true;
                }

                return _logErrorPath;
            }
        }

        private bool _debugOverlayEnabledFetched = false;
        private bool _debugOverlayEnabled;

        public bool DebugOverlayEnabled
        {
            get
            {
                if (!_debugOverlayEnabledFetched)
                {
                    _debugOverlayEnabled = GetBool(GetValue("DebugOverlay", "Enabled"), true);
                    _debugOverlayEnabledFetched = true;
                }

                return _debugOverlayEnabled;
            }
        }

        private bool _debugOverlayEnabledByDefaultFetched = false;
        private bool _debugOverlayEnabledByDefault;

        public bool DebugOverlayEnabledByDefault
        {
            get
            {
                if (!_debugOverlayEnabledByDefaultFetched)
                {
                    _debugOverlayEnabledByDefault = GetBool(GetValue("DebugOverlay", "EnabledByDefault"), false);
                    _debugOverlayEnabledByDefaultFetched = true;
                }

                return _debugOverlayEnabledByDefault;
            }
        }

        private bool _debugOverlayToggleKeyFetched = false;
        private Key _debugOverlayToggleKey;

        public Key DebugOverlayToggleKey
        {
            get
            {
                if (!_debugOverlayToggleKeyFetched)
                {
                    _debugOverlayToggleKey = GetKey(GetValue("DebugOverlay", "ToggleKey"), Key.F3);
                    _debugOverlayToggleKeyFetched = true;
                }

                return _debugOverlayToggleKey;
            }
        }

        private bool _debugOverlayFontPathFetched = false;
        private string _debugOverlayFontPath = string.Empty;

        public string DebugOverlayFontPath
        {
            get
            {
                if (!_debugOverlayFontPathFetched)
                {
                    _debugOverlayFontPath = GetString(
                        GetValue("DebugOverlay", "FontPath"),
                        "./resources/engine/vt323-regular.ttf"
                    );
                    _debugOverlayFontPathFetched = true;
                }

                return _debugOverlayFontPath;
            }
        }

        private bool _debugOverlayFontSizeFetched = false;
        private int _debugOverlayFontSize;

        public int DebugOverlayFontSize
        {
            get
            {
                if (!_debugOverlayFontSizeFetched)
                {
                    _debugOverlayFontSize = GetInt(GetValue("DebugOverlay", "FontSize"), 20);
                    _debugOverlayFontSizeFetched = true;
                }

                return _debugOverlayFontSize;
            }
        }

        private bool _debugOverlayRefreshHzFetched = false;
        private int _debugOverlayRefreshHz;

        public int DebugOverlayRefreshHz
        {
            get
            {
                if (!_debugOverlayRefreshHzFetched)
                {
                    _debugOverlayRefreshHz = Math.Max(1, GetInt(GetValue("DebugOverlay", "RefreshHz"), 4));
                    _debugOverlayRefreshHzFetched = true;
                }

                return _debugOverlayRefreshHz;
            }
        }

        private bool _debugOverlayPosXFetched = false;
        private int _debugOverlayPosX;

        public int DebugOverlayPosX
        {
            get
            {
                if (!_debugOverlayPosXFetched)
                {
                    _debugOverlayPosX = GetInt(GetValue("DebugOverlay", "PosX"), 12);
                    _debugOverlayPosXFetched = true;
                }

                return _debugOverlayPosX;
            }
        }

        private bool _debugOverlayPosYFetched = false;
        private int _debugOverlayPosY;

        public int DebugOverlayPosY
        {
            get
            {
                if (!_debugOverlayPosYFetched)
                {
                    _debugOverlayPosY = GetInt(GetValue("DebugOverlay", "PosY"), 12);
                    _debugOverlayPosYFetched = true;
                }

                return _debugOverlayPosY;
            }
        }

        private bool _runtimeEditorEnabledFetched = false;
        private bool _runtimeEditorEnabled;

        public bool RuntimeEditorEnabled
        {
            get
            {
                if (!_runtimeEditorEnabledFetched)
                {
                    _runtimeEditorEnabled = GetBool(GetValue("RuntimeEditor", "Enabled"), false);
                    _runtimeEditorEnabledFetched = true;
                }

                return _runtimeEditorEnabled;
            }
        }

        private bool _runtimeEditorToggleKeyFetched = false;
        private Key _runtimeEditorToggleKey;

        public Key RuntimeEditorToggleKey
        {
            get
            {
                if (!_runtimeEditorToggleKeyFetched)
                {
                    _runtimeEditorToggleKey = GetKey(GetValue("RuntimeEditor", "ToggleKey"), Key.F4);
                    _runtimeEditorToggleKeyFetched = true;
                }

                return _runtimeEditorToggleKey;
            }
        }

        private bool _runtimeEditorPortFetched = false;
        private int _runtimeEditorPort;

        public int RuntimeEditorPort
        {
            get
            {
                if (!_runtimeEditorPortFetched)
                {
                    _runtimeEditorPort = Math.Max(1, Math.Min(65535, GetInt(GetValue("RuntimeEditor", "Port"), 5057)));
                    _runtimeEditorPortFetched = true;
                }

                return _runtimeEditorPort;
            }
        }

        private bool _runtimeEditorOpenBrowserFetched = false;
        private bool _runtimeEditorOpenBrowser;

        public bool RuntimeEditorOpenBrowser
        {
            get
            {
                if (!_runtimeEditorOpenBrowserFetched)
                {
                    _runtimeEditorOpenBrowser = GetBool(GetValue("RuntimeEditor", "OpenBrowser"), true);
                    _runtimeEditorOpenBrowserFetched = true;
                }

                return _runtimeEditorOpenBrowser;
            }
        }

        private bool _runtimeEditorAutostartFetched = false;
        private bool _runtimeEditorAutostart = false;
        
        public bool RuntimeEditorAutostart
        {
            get
            {
                if (!_runtimeEditorAutostartFetched)
                {
                    _runtimeEditorAutostart = GetBool(GetValue("RuntimeEditor", "Autostart"), false);
                    _runtimeEditorAutostartFetched = true;
                }

                return _runtimeEditorAutostart;
            }
        }

        private bool _runtimeEditorBrowserAppModeFetched = false;
        private bool _runtimeEditorBrowserAppMode;

        public bool RuntimeEditorBrowserAppMode
        {
            get
            {
                if (!_runtimeEditorBrowserAppModeFetched)
                {
                    _runtimeEditorBrowserAppMode = GetBool(GetValue("RuntimeEditor", "BrowserAppMode"), false);
                    _runtimeEditorBrowserAppModeFetched = true;
                }

                return _runtimeEditorBrowserAppMode;
            }
        }

        private bool _runtimeEditorBrowserExecutableFetched = false;
        private string _runtimeEditorBrowserExecutable = string.Empty;

        public string RuntimeEditorBrowserExecutable
        {
            get
            {
                if (!_runtimeEditorBrowserExecutableFetched)
                {
                    _runtimeEditorBrowserExecutable = GetString(GetValue("RuntimeEditor", "BrowserExecutable"), "chromium");
                    _runtimeEditorBrowserExecutableFetched = true;
                }

                return _runtimeEditorBrowserExecutable;
            }
        }

        public AppConfig(string iniFile)
        {
            var parser = new FileIniDataParser();
            _data = parser.ReadFile(iniFile);
        }

        private static int GetInt(string value, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }
        
        private static bool GetBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.ToLower() switch
            {
                "true" or "1" or "on" => true,
                "false" or "0" or "off" => false,
                _ => defaultValue
            };
        }

        private string GetValue(string section, string key)
        {
            if (!_data.Sections.ContainsSection(section))
            {
                return string.Empty;
            }

            return _data[section][key] ?? string.Empty;
        }

        private static string GetString(string value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static Key GetKey(string value, Key defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return Enum.TryParse<Key>(value, true, out var parsed)
                ? parsed
                : defaultValue;
        }

        private static GraphicsBackend GetGraphicsBackend(string value, GraphicsBackend defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.Trim().ToLower() switch
            {
                "opengl" or "gl" => GraphicsBackend.OpenGL,
                _ => throw new InvalidOperationException(
                    $"Unsupported graphics backend '{value}'. Supported values: OpenGL, GL.")
            };
        }
    }
}
