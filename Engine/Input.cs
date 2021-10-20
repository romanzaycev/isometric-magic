using System.Collections.Generic;
using SDL2;

namespace IsometricMagic.Engine
{
    static class Input
    {
        private static readonly Dictionary<SDL.SDL_Keycode, byte> KeyState = new();
        private static readonly Dictionary<uint, byte> MouseButtonState = new();
        
        private static int _mouseX;
        public static int MouseX => _mouseX;
        
        private static int _mouseY;
        public static int MouseY => _mouseY;

        public static void HandleKeyboardEvent(SDL.SDL_Event evt)
        {
            KeyState[evt.key.keysym.sym] = evt.key.state;
        }
        
        public static void HandleMouseBtnEvent(SDL.SDL_Event evt)
        {
            MouseButtonState[evt.button.button] = evt.button.state;
        }

        public static void HandleMousePos(int x, int y)
        {
            _mouseX = x;
            _mouseY = y;
        }

        public static bool IsPressed(SDL.SDL_Keycode keyCode)
        {
            return KeyState.ContainsKey(keyCode) && KeyState[keyCode] == 1;
        }

        public static bool IsReleased(SDL.SDL_Keycode keyCode)
        {
            return !KeyState.ContainsKey(keyCode) || KeyState[keyCode] == 0;
        }

        public static bool IsMousePressed(uint mouseButton)
        {
            return MouseButtonState.ContainsKey(mouseButton) && MouseButtonState[mouseButton] == 1;
        }
        
        public static bool IsMouseReleased(uint mouseButton)
        {
            return !MouseButtonState.ContainsKey(mouseButton) || MouseButtonState[mouseButton] == 0;
        }
    }
}