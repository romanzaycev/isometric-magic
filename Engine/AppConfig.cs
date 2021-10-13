using System;
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
        
        public AppConfig(string iniFile)
        {
            var parser = new FileIniDataParser();
            _data = parser.ReadFile(iniFile);
        }

        private static int GetInt(string value, int defaultValue)
        {
            return value == string.Empty ? defaultValue : int.Parse(value);
        }
    }
}