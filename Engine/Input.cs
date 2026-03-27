using System;
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

        private static readonly Dictionary<GamepadButton, bool> GamepadButtonState = new();
        private static readonly HashSet<GamepadButton> GamepadButtonsPressedThisFrame = new();
        private static readonly HashSet<GamepadButton> GamepadButtonsReleasedThisFrame = new();
        private static readonly Dictionary<GamepadAxis, float> GamepadAxisState = new();

        private static IntPtr _gameController = IntPtr.Zero;
        private static int _gamepadInstanceId = -1;
        private static bool _gamepadInitialized;

        private const float StickDeadzone = 0.2f;
        private const float TriggerDeadzone = 0.05f;

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
            GamepadButtonsPressedThisFrame.Clear();
            GamepadButtonsReleasedThisFrame.Clear();

            EnsureGamepadInitialized();
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
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    HandleGamepadDeviceAdded(evt.cdevice.which);
                    break;
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    HandleGamepadDeviceRemoved(evt.cdevice.which);
                    break;
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                    HandleGamepadDeviceRemapped(evt.cdevice.which);
                    break;
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    HandleGamepadButtonDown(evt.cbutton.which, evt.cbutton.button);
                    break;
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    HandleGamepadButtonUp(evt.cbutton.which, evt.cbutton.button);
                    break;
                case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                    HandleGamepadAxisMotion(evt.caxis.which, evt.caxis.axis, evt.caxis.axisValue);
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

        public static bool IsGamepadConnected => _gameController != IntPtr.Zero;

        public static bool IsDown(GamepadButton button)
        {
            return GamepadButtonState.TryGetValue(button, out var isDown) && isDown;
        }

        public static bool IsUp(GamepadButton button)
        {
            return !GamepadButtonState.TryGetValue(button, out var isDown) || !isDown;
        }

        public static bool WasPressed(GamepadButton button)
        {
            return GamepadButtonsPressedThisFrame.Contains(button);
        }

        public static bool WasReleased(GamepadButton button)
        {
            return GamepadButtonsReleasedThisFrame.Contains(button);
        }

        public static float GetAxis(GamepadAxis axis)
        {
            if (!IsGamepadConnected)
            {
                return 0f;
            }

            if (!GamepadAxisState.TryGetValue(axis, out var value))
            {
                return 0f;
            }

            if (axis == GamepadAxis.LeftTrigger || axis == GamepadAxis.RightTrigger)
            {
                if (value < TriggerDeadzone)
                {
                    return 0f;
                }

                var adjusted = (value - TriggerDeadzone) / (1f - TriggerDeadzone);
                return Clamp01(adjusted);
            }

            if (Math.Abs(value) < StickDeadzone)
            {
                return 0f;
            }

            var sign = Math.Sign(value);
            var adjustedStick = (Math.Abs(value) - StickDeadzone) / (1f - StickDeadzone);
            return ClampNegPos(adjustedStick * sign);
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

        private static void EnsureGamepadInitialized()
        {
            if (_gamepadInitialized)
            {
                return;
            }

            _gamepadInitialized = true;
            SDL.SDL_GameControllerEventState(SDL.SDL_ENABLE);

            var count = SDL.SDL_NumJoysticks();
            for (var i = 0; i < count; i++)
            {
                if (SDL.SDL_IsGameController(i) != SDL.SDL_bool.SDL_TRUE)
                {
                    continue;
                }

                if (OpenGamepad(i))
                {
                    break;
                }
            }
        }

        private static void HandleGamepadDeviceAdded(int deviceIndex)
        {
            EnsureGamepadInitialized();
            OpenGamepad(deviceIndex);
        }

        private static void HandleGamepadDeviceRemoved(int instanceId)
        {
            if (_gameController == IntPtr.Zero)
            {
                return;
            }

            if (_gamepadInstanceId != instanceId)
            {
                return;
            }

            SDL.SDL_GameControllerClose(_gameController);
            _gameController = IntPtr.Zero;
            _gamepadInstanceId = -1;
            GamepadButtonState.Clear();
            GamepadButtonsPressedThisFrame.Clear();
            GamepadButtonsReleasedThisFrame.Clear();
            GamepadAxisState.Clear();
        }

        private static void HandleGamepadDeviceRemapped(int instanceId)
        {
            if (_gameController == IntPtr.Zero)
            {
                return;
            }

            if (_gamepadInstanceId != instanceId)
            {
                return;
            }

            var joystick = SDL.SDL_GameControllerGetJoystick(_gameController);
            _gamepadInstanceId = SDL.SDL_JoystickInstanceID(joystick);
        }

        private static void HandleGamepadButtonDown(int instanceId, byte sdlButton)
        {
            if (!IsGamepadActiveInstance(instanceId))
            {
                return;
            }

            if (!TryMapGamepadButton(sdlButton, out var button))
            {
                return;
            }

            if (!GamepadButtonState.TryGetValue(button, out var isDown) || !isDown)
            {
                GamepadButtonsPressedThisFrame.Add(button);
            }

            GamepadButtonState[button] = true;
        }

        private static void HandleGamepadButtonUp(int instanceId, byte sdlButton)
        {
            if (!IsGamepadActiveInstance(instanceId))
            {
                return;
            }

            if (!TryMapGamepadButton(sdlButton, out var button))
            {
                return;
            }

            if (GamepadButtonState.TryGetValue(button, out var isDown) && isDown)
            {
                GamepadButtonsReleasedThisFrame.Add(button);
            }

            GamepadButtonState[button] = false;
        }

        private static void HandleGamepadAxisMotion(int instanceId, byte sdlAxis, short value)
        {
            if (!IsGamepadActiveInstance(instanceId))
            {
                return;
            }

            if (!TryMapGamepadAxis(sdlAxis, out var axis))
            {
                return;
            }

            var normalized = NormalizeAxisValue(axis, value);
            GamepadAxisState[axis] = normalized;
        }

        private static bool IsGamepadActiveInstance(int instanceId)
        {
            return _gameController != IntPtr.Zero && _gamepadInstanceId == instanceId;
        }

        private static bool OpenGamepad(int deviceIndex)
        {
            if (_gameController != IntPtr.Zero)
            {
                return false;
            }

            if (SDL.SDL_IsGameController(deviceIndex) != SDL.SDL_bool.SDL_TRUE)
            {
                return false;
            }

            var controller = SDL.SDL_GameControllerOpen(deviceIndex);
            if (controller == IntPtr.Zero)
            {
                return false;
            }

            _gameController = controller;
            var joystick = SDL.SDL_GameControllerGetJoystick(_gameController);
            _gamepadInstanceId = SDL.SDL_JoystickInstanceID(joystick);

            GamepadButtonState.Clear();
            GamepadAxisState.Clear();

            return true;
        }

        private static bool TryMapGamepadButton(byte sdlButton, out GamepadButton button)
        {
            button = default;
            switch ((SDL.SDL_GameControllerButton) sdlButton)
            {
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    button = GamepadButton.A;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                    button = GamepadButton.B;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                    button = GamepadButton.X;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                    button = GamepadButton.Y;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    button = GamepadButton.DpadUp;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    button = GamepadButton.DpadDown;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                    button = GamepadButton.DpadLeft;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                    button = GamepadButton.DpadRight;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START:
                    button = GamepadButton.Start;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                    button = GamepadButton.Back;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                    button = GamepadButton.LeftShoulder;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                    button = GamepadButton.RightShoulder;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                    button = GamepadButton.LeftStick;
                    return true;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                    button = GamepadButton.RightStick;
                    return true;
            }

            return false;
        }

        private static bool TryMapGamepadAxis(byte sdlAxis, out GamepadAxis axis)
        {
            axis = default;
            switch ((SDL.SDL_GameControllerAxis) sdlAxis)
            {
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX:
                    axis = GamepadAxis.LeftX;
                    return true;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY:
                    axis = GamepadAxis.LeftY;
                    return true;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX:
                    axis = GamepadAxis.RightX;
                    return true;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY:
                    axis = GamepadAxis.RightY;
                    return true;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT:
                    axis = GamepadAxis.LeftTrigger;
                    return true;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT:
                    axis = GamepadAxis.RightTrigger;
                    return true;
            }

            return false;
        }

        private static float NormalizeAxisValue(GamepadAxis axis, short value)
        {
            if (axis == GamepadAxis.LeftTrigger || axis == GamepadAxis.RightTrigger)
            {
                var normalized = value / 32767f;
                return Clamp01(normalized);
            }

            if (value < 0)
            {
                return value / 32768f;
            }

            return value / 32767f;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static float ClampNegPos(float value)
        {
            if (value < -1f) return -1f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
