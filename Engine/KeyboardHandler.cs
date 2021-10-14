using System.Collections.Generic;
using SDL2;

namespace IsometricMagic.Engine
{
    class KeyboardHandler
    {
        private Dictionary<SDL.SDL_Keycode, byte> _keyState = new Dictionary<SDL.SDL_Keycode, byte>();

        public void HandleKeyboardEvent(SDL.SDL_Event evt)
        {
            _keyState[evt.key.keysym.sym] = evt.key.state;
        }

        public bool IsPressed(SDL.SDL_Keycode keyCode) {
            return _keyState[keyCode] == 1;
        }

        public bool IsReleased(SDL.SDL_Keycode keyCode) {
            return _keyState[keyCode] == 0;
        }
    }
}