using IniParser;
using IniParser.Model;

namespace IsometricMagic.Engine
{
    class AppConfig
    {
        private readonly IniData _data;

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

        public AppConfig(string iniFile)
        {
            var parser = new FileIniDataParser();
            _data = parser.ReadFile(iniFile);
        }

        private static int GetInt(string value, int defaultValue)
        {
            return value == string.Empty ? defaultValue : int.Parse(value);
        }
        
        private static bool GetBool(string value, bool defaultValue)
        {
            if (value is "true" or "1" or "On" or "on")
            {
                return true;
            }
            
            if (value is "false" or "0" or "Off" or "off")
            {
                return false;
            }

            return defaultValue;
        }
    }
}