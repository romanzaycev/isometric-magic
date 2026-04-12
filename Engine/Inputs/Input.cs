using static SDL2.SDL;

using IsometricMagic.Engine.Core.Platform.Sdl;

namespace IsometricMagic.Engine.Inputs
{
    public static class Input
    {
        private static readonly bool[] KeyState = new bool[Enum.GetValues<Key>().Length];
        private static readonly bool[] KeysPressedThisFrame = new bool[Enum.GetValues<Key>().Length];
        private static readonly bool[] KeysReleasedThisFrame = new bool[Enum.GetValues<Key>().Length];

        private static readonly bool[] MouseButtonState = new bool[Enum.GetValues<MouseButton>().Length];
        private static readonly bool[] MouseButtonsPressedThisFrame = new bool[Enum.GetValues<MouseButton>().Length];
        private static readonly bool[] MouseButtonsReleasedThisFrame = new bool[Enum.GetValues<MouseButton>().Length];

        private static readonly bool[] GamepadButtonState = new bool[Enum.GetValues<GamepadButton>().Length];
        private static readonly bool[] GamepadButtonsPressedThisFrame = new bool[Enum.GetValues<GamepadButton>().Length];
        private static readonly bool[] GamepadButtonsReleasedThisFrame = new bool[Enum.GetValues<GamepadButton>().Length];
        private static readonly float[] GamepadAxisState = new float[Enum.GetValues<GamepadAxis>().Length];

        private static IntPtr _gameController = IntPtr.Zero;
        private static int _gamepadInstanceId = -1;
        private static bool _gamepadInitialized;

        private const float StickDeadzone = 0.2f;
        private const float TriggerDeadzone = 0.05f;

        private static int _mouseX;
        public static int MouseX => _mouseX;

        private static int _mouseY;
        public static int MouseY => _mouseY;

        internal static void BeginFrame()
        {
            Array.Clear(KeysPressedThisFrame);
            Array.Clear(KeysReleasedThisFrame);
            Array.Clear(MouseButtonsPressedThisFrame);
            Array.Clear(MouseButtonsReleasedThisFrame);
            Array.Clear(GamepadButtonsPressedThisFrame);
            Array.Clear(GamepadButtonsReleasedThisFrame);

            EnsureGamepadInitialized();
        }

        internal static void HandleEvent(SDL_Event evt)
        {
            switch (evt.type)
            {
                case SDL_EventType.SDL_KEYDOWN:
                    HandleKeyDown(evt.key.keysym.sym);
                    break;
                case SDL_EventType.SDL_KEYUP:
                    HandleKeyUp(evt.key.keysym.sym);
                    break;
                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    HandleMouseButtonDown(evt.button.button);
                    break;
                case SDL_EventType.SDL_MOUSEBUTTONUP:
                    HandleMouseButtonUp(evt.button.button);
                    break;
                case SDL_EventType.SDL_MOUSEMOTION:
                    HandleMousePos(evt.motion.x, evt.motion.y);
                    break;
                case SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    HandleGamepadDeviceAdded(evt.cdevice.which);
                    break;
                case SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    HandleGamepadDeviceRemoved(evt.cdevice.which);
                    break;
                case SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                    HandleGamepadDeviceRemapped(evt.cdevice.which);
                    break;
                case SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    HandleGamepadButtonDown(evt.cbutton.which, evt.cbutton.button);
                    break;
                case SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    HandleGamepadButtonUp(evt.cbutton.which, evt.cbutton.button);
                    break;
                case SDL_EventType.SDL_CONTROLLERAXISMOTION:
                    HandleGamepadAxisMotion(evt.caxis.which, evt.caxis.axis, evt.caxis.axisValue);
                    break;
            }
        }

        private static void HandleKeyDown(SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                if (!TryGetIndex(key, out var keyIndex))
                {
                    return;
                }

                if (!KeyState[keyIndex])
                {
                    KeysPressedThisFrame[keyIndex] = true;
                }

                KeyState[keyIndex] = true;
            }
        }

        private static void HandleKeyUp(SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                if (!TryGetIndex(key, out var keyIndex))
                {
                    return;
                }

                if (KeyState[keyIndex])
                {
                    KeysReleasedThisFrame[keyIndex] = true;
                }

                KeyState[keyIndex] = false;
            }
        }

        private static void HandleMouseButtonDown(uint mouseButton)
        {
            if (TryMapMouseButton(mouseButton, out var button))
            {
                if (!TryGetIndex(button, out var buttonIndex))
                {
                    return;
                }

                if (!MouseButtonState[buttonIndex])
                {
                    MouseButtonsPressedThisFrame[buttonIndex] = true;
                }

                MouseButtonState[buttonIndex] = true;
            }
        }

        private static void HandleMouseButtonUp(uint mouseButton)
        {
            if (TryMapMouseButton(mouseButton, out var button))
            {
                if (!TryGetIndex(button, out var buttonIndex))
                {
                    return;
                }

                if (MouseButtonState[buttonIndex])
                {
                    MouseButtonsReleasedThisFrame[buttonIndex] = true;
                }

                MouseButtonState[buttonIndex] = false;
            }
        }

        private static void HandleMousePos(int x, int y)
        {
            _mouseX = x;
            _mouseY = y;
        }

        public static bool IsDown(Key key)
        {
            return TryGetIndex(key, out var keyIndex) && KeyState[keyIndex];
        }

        public static bool IsUp(Key key)
        {
            return !TryGetIndex(key, out var keyIndex) || !KeyState[keyIndex];
        }

        public static bool WasPressed(Key key)
        {
            return TryGetIndex(key, out var keyIndex) && KeysPressedThisFrame[keyIndex];
        }

        public static bool WasReleased(Key key)
        {
            return TryGetIndex(key, out var keyIndex) && KeysReleasedThisFrame[keyIndex];
        }

        public static bool IsDown(MouseButton button)
        {
            return TryGetIndex(button, out var buttonIndex) && MouseButtonState[buttonIndex];
        }

        public static bool IsUp(MouseButton button)
        {
            return !TryGetIndex(button, out var buttonIndex) || !MouseButtonState[buttonIndex];
        }

        public static bool WasPressed(MouseButton button)
        {
            return TryGetIndex(button, out var buttonIndex) && MouseButtonsPressedThisFrame[buttonIndex];
        }

        public static bool WasReleased(MouseButton button)
        {
            return TryGetIndex(button, out var buttonIndex) && MouseButtonsReleasedThisFrame[buttonIndex];
        }

        public static bool IsGamepadConnected => _gameController != IntPtr.Zero;

        public static bool IsDown(GamepadButton button)
        {
            return TryGetIndex(button, out var buttonIndex) && GamepadButtonState[buttonIndex];
        }

        public static bool IsUp(GamepadButton button)
        {
            return !TryGetIndex(button, out var buttonIndex) || !GamepadButtonState[buttonIndex];
        }

        public static bool WasPressed(GamepadButton button)
        {
            return TryGetIndex(button, out var buttonIndex) && GamepadButtonsPressedThisFrame[buttonIndex];
        }

        public static bool WasReleased(GamepadButton button)
        {
            return TryGetIndex(button, out var buttonIndex) && GamepadButtonsReleasedThisFrame[buttonIndex];
        }

        public static float GetAxis(GamepadAxis axis)
        {
            if (!IsGamepadConnected)
            {
                return 0f;
            }

            if (!TryGetIndex(axis, out var axisIndex))
            {
                return 0f;
            }

            var value = GamepadAxisState[axisIndex];

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

        internal static bool IsPressed(SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                return IsDown(key);
            }
            return false;
        }

        internal static bool IsReleased(SDL_Keycode keyCode)
        {
            if (SdlKeycodeMapper.TryMap(keyCode, out var key))
            {
                return IsUp(key);
            }
            return true;
        }

        internal static bool IsMousePressed(uint mouseButton)
        {
            if (TryMapMouseButton(mouseButton, out var button))
            {
                return IsDown(button);
            }
            return false;
        }
        
        internal static bool IsMouseReleased(uint mouseButton)
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
                case SDL_BUTTON_LEFT:
                    button = MouseButton.Left;
                    return true;
                case SDL_BUTTON_RIGHT:
                    button = MouseButton.Right;
                    return true;
                case SDL_BUTTON_MIDDLE:
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
            SDL_GameControllerEventState(SDL_ENABLE);

            var count = SDL_NumJoysticks();
            for (var i = 0; i < count; i++)
            {
                if (SDL_IsGameController(i) != SDL_bool.SDL_TRUE)
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

            SDL_GameControllerClose(_gameController);
            _gameController = IntPtr.Zero;
            _gamepadInstanceId = -1;
            Array.Clear(GamepadButtonState);
            Array.Clear(GamepadButtonsPressedThisFrame);
            Array.Clear(GamepadButtonsReleasedThisFrame);
            Array.Clear(GamepadAxisState);
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

            var joystick = SDL_GameControllerGetJoystick(_gameController);
            _gamepadInstanceId = SDL_JoystickInstanceID(joystick);
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

            if (!TryGetIndex(button, out var buttonIndex))
            {
                return;
            }

            if (!GamepadButtonState[buttonIndex])
            {
                GamepadButtonsPressedThisFrame[buttonIndex] = true;
            }

            GamepadButtonState[buttonIndex] = true;
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

            if (!TryGetIndex(button, out var buttonIndex))
            {
                return;
            }

            if (GamepadButtonState[buttonIndex])
            {
                GamepadButtonsReleasedThisFrame[buttonIndex] = true;
            }

            GamepadButtonState[buttonIndex] = false;
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

            if (!TryGetIndex(axis, out var axisIndex))
            {
                return;
            }

            var normalized = NormalizeAxisValue(axis, value);
            GamepadAxisState[axisIndex] = normalized;
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

            if (SDL_IsGameController(deviceIndex) != SDL_bool.SDL_TRUE)
            {
                return false;
            }

            var controller = SDL_GameControllerOpen(deviceIndex);
            if (controller == IntPtr.Zero)
            {
                return false;
            }

            _gameController = controller;
            var joystick = SDL_GameControllerGetJoystick(_gameController);
            _gamepadInstanceId = SDL_JoystickInstanceID(joystick);

            Array.Clear(GamepadButtonState);
            Array.Clear(GamepadButtonsPressedThisFrame);
            Array.Clear(GamepadButtonsReleasedThisFrame);
            Array.Clear(GamepadAxisState);

            return true;
        }

        private static bool TryGetIndex(Key key, out int index)
        {
            index = (int) key;
            return (uint) index < (uint) KeyState.Length;
        }

        private static bool TryGetIndex(MouseButton button, out int index)
        {
            index = (int) button;
            return (uint) index < (uint) MouseButtonState.Length;
        }

        private static bool TryGetIndex(GamepadButton button, out int index)
        {
            index = (int) button;
            return (uint) index < (uint) GamepadButtonState.Length;
        }

        private static bool TryGetIndex(GamepadAxis axis, out int index)
        {
            index = (int) axis;
            return (uint) index < (uint) GamepadAxisState.Length;
        }

        private static bool TryMapGamepadButton(byte sdlButton, out GamepadButton button)
        {
            button = default;
            switch ((SDL_GameControllerButton) sdlButton)
            {
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    button = GamepadButton.A;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                    button = GamepadButton.B;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                    button = GamepadButton.X;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                    button = GamepadButton.Y;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    button = GamepadButton.DpadUp;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    button = GamepadButton.DpadDown;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                    button = GamepadButton.DpadLeft;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                    button = GamepadButton.DpadRight;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START:
                    button = GamepadButton.Start;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                    button = GamepadButton.Back;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                    button = GamepadButton.LeftShoulder;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                    button = GamepadButton.RightShoulder;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                    button = GamepadButton.LeftStick;
                    return true;
                case SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                    button = GamepadButton.RightStick;
                    return true;
            }

            return false;
        }

        private static bool TryMapGamepadAxis(byte sdlAxis, out GamepadAxis axis)
        {
            axis = default;
            switch ((SDL_GameControllerAxis) sdlAxis)
            {
                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX:
                    axis = GamepadAxis.LeftX;
                    return true;
                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY:
                    axis = GamepadAxis.LeftY;
                    return true;
                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX:
                    axis = GamepadAxis.RightX;
                    return true;
                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY:
                    axis = GamepadAxis.RightY;
                    return true;
                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT:
                    axis = GamepadAxis.LeftTrigger;
                    return true;
                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT:
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
