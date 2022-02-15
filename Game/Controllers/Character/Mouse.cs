using IsometricMagic.Engine;
using IsometricMagic.Game.Model;
using SDL2;

namespace IsometricMagic.Game.Controllers.Character
{
    public class Mouse : CharacterMovementController
    {
        private static readonly Engine.Camera Camera = Application.GetInstance().GetRenderer().GetCamera();
        private bool _isDrag;
        private int _startMouseX;
        private int _startMouseY;
        
        public override void HandleMovement(Game.Character.Character character, IsoWorldPositionConverter positionConverter)
        {
            if (Input.IsMousePressed(SDL.SDL_BUTTON_LEFT) && !_isDrag)
            {
                _startMouseX = Input.MouseX;
                _startMouseY = Input.MouseY;
                _isDrag = true;
            }

            if (Input.IsMouseReleased(SDL.SDL_BUTTON_LEFT) && _isDrag)
            {
                _isDrag = false;
            }

            if (_isDrag)
            {
                var canvasMousePos = Camera.GetCanvasPosition(_startMouseX, _startMouseY);
                var mouseWorldPos = positionConverter.GetWorldPosition((int)canvasMousePos.X, (int)canvasMousePos.Y);

                TryMove(
                    (int) (mouseWorldPos.X - character.WorldPosX),
                    (int) (mouseWorldPos.Y - character.WorldPosY),
                    character,
                    positionConverter
                );
            }
            else
            {
                StopMove(character);
            }
            
            _startMouseX = Input.MouseX;
            _startMouseY = Input.MouseY;
        }
    }
}