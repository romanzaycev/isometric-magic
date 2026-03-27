using System.Collections.Generic;
using SDL2;

namespace IsometricMagic.Engine
{
    static class Input
    {
        private static readonly Dictionary<Key, bool> KeyState = new();
        private static readonly HashSet<Key> KeysPressedThisFrame = new();
        private static readonly HashSet<Key> KeysReleasedThisFrame = new();

        private static readonly Dictionary<MouseButton, bool> MouseButtonState = new();
        private static readonly HashSet<MouseButton> MouseButtonsPressedThisFrame = new();
        private static readonly HashSet<MouseButton> MouseButtonsReleasedThisFrame = new();

        private static int _mouseX;
        public static int MouseX => _mouseX;

        private static int _mouseY;
        public static int MouseY => _mouseY;

        public static void BeginFrame()
        {
            KeysPressedThisFrame.Clear();
            KeysReleasedThisFrame.Clear();
            MouseButtonsPressedThisFrame.Clear();
            MouseButtonsReleasedThisFrame.Clear();
        }

        public static void HandleEvent(SDL.SDL_Event evt)
        {
            switch (evt.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    HandleKeyDown(evt.key.keysym.sym);
                    break;
                case SDL.SDL_EventType.SDL_KEYUP:
                    HandleKeyUp(evt.key.keysym.sym);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    HandleMouseButtonDown(evt.button.button);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    HandleMouseButtonUp(evt.button.button);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    HandleMousePos(evt.motion.x, evt.motion.y);
                    break;
            }
        }

        private static void HandleKeyDown(SDL.SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                if (!KeyState.TryGetValue(key, out var isDown) || !isDown)
                {
                    KeysPressedThisFrame.Add(key);
                }
                KeyState[key] = true;
            }
        }

        private static void HandleKeyUp(SDL.SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                if (KeyState.TryGetValue(key, out var isDown) && isDown)
                {
                    KeysReleasedThisFrame.Add(key);
                }
                KeyState[key] = false;
            }
        }

        private static void HandleMouseButtonDown(uint mouseButton)
        {
            if (TryMapMouseButton(mouseButton, out var button))
            {
                if (!MouseButtonState.TryGetValue(button, out var isDown) || !isDown)
                {
                    MouseButtonsPressedThisFrame.Add(button);
                }
                MouseButtonState[button] = true;
            }
        }

        private static void HandleMouseButtonUp(uint mouseButton)
        {
            if (TryMapMouseButton(mouseButton, out var button))
            {
                if (MouseButtonState.TryGetValue(button, out var isDown) && isDown)
                {
                    MouseButtonsReleasedThisFrame.Add(button);
                }
                MouseButtonState[button] = false;
            }
        }

        private static void HandleMousePos(int x, int y)
        {
            _mouseX = x;
            _mouseY = y;
        }

        public static bool IsDown(Key key)
        {
            return KeyState.TryGetValue(key, out var isDown) && isDown;
        }

        public static bool IsUp(Key key)
        {
            return !KeyState.TryGetValue(key, out var isDown) || !isDown;
        }

        public static bool WasPressed(Key key)
        {
            return KeysPressedThisFrame.Contains(key);
        }

        public static bool WasReleased(Key key)
        {
            return KeysReleasedThisFrame.Contains(key);
        }

        public static bool IsDown(MouseButton button)
        {
            return MouseButtonState.TryGetValue(button, out var isDown) && isDown;
        }

        public static bool IsUp(MouseButton button)
        {
            return !MouseButtonState.TryGetValue(button, out var isDown) || !isDown;
        }

        public static bool WasPressed(MouseButton button)
        {
            return MouseButtonsPressedThisFrame.Contains(button);
        }

        public static bool WasReleased(MouseButton button)
        {
            return MouseButtonsReleasedThisFrame.Contains(button);
        }

        [System.Obsolete("Use IsDown(Key) instead")]
        public static bool IsPressed(SDL.SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                return IsDown(key);
            }
            return false;
        }

        [System.Obsolete("Use IsUp(Key) instead")]
        public static bool IsReleased(SDL.SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                return IsUp(key);
            }
            return true;
        }

        [System.Obsolete("Use IsDown(MouseButton) instead")]
        public static bool IsMousePressed(uint mouseButton)
        {
            if (TryMapMouseButton(mouseButton, out var button))
            {
                return IsDown(button);
            }
            return false;
        }

        [System.Obsolete("Use IsUp(MouseButton) instead")]
        public static bool IsMouseReleased(uint mouseButton)
        {
            if (TryMapMouseButton(mouseButton, out var button))
            {
                return IsUp(button);
            }
            return true;
        }

        

        private static bool TryMapMouseButton(uint sdlButton, out MouseButton button)
        {
            button = default;
            switch (sdlButton)
            {
                case SDL.SDL_BUTTON_LEFT:
                    button = MouseButton.Left;
                    return true;
                case SDL.SDL_BUTTON_RIGHT:
                    button = MouseButton.Right;
                    return true;
                case SDL.SDL_BUTTON_MIDDLE:
                    button = MouseButton.Middle;
                    return true;
            }
            return false;
        }
    }
}
