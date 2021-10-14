using System;
using SDL2;

namespace IsometricMagic.Engine
{
    class Renderer
    {
        private static readonly Application Application = Application.GetInstance();
        private static readonly SpriteHolder SpriteHolder = SpriteHolder.GetInstance();
        private static readonly TextureHolder TextureHolder = TextureHolder.GetInstance();
        
        private readonly IntPtr _sdlRenderer;
        private readonly Camera _camera;

        public Renderer(IntPtr sdlRenderer)
        {
            _sdlRenderer = sdlRenderer;
            _camera = new Camera(
                Application.GetConfig().WindowWidth,
                Application.GetConfig().WindowHeight
            );
        }

        public void DrawAll()
        {
            SDL.SDL_SetRenderDrawColor(_sdlRenderer,0x00, 0x00, 0x00, 0x00);
            SDL.SDL_RenderClear(_sdlRenderer);
            
            DrawSprites();

            SDL.SDL_RenderPresent(_sdlRenderer);
        }

        public IntPtr GetSdl()
        {
            return _sdlRenderer;
        }

        public Camera GetCamera()
        {
            return _camera;
        }
        
        private void DrawSprites()
        {
            var sprites = SpriteHolder.GetSprites();
            
            Array.Sort(sprites, (spriteA, spriteB) => spriteA.Sorting.CompareTo(spriteB.Sorting));
            
            foreach (var sprite in sprites)
            {
                if (sprite.Texture == null) continue;

                var tex = sprite.Texture;
                    
                SDL.SDL_Rect sourceRect;
                sourceRect.w = tex.Width;
                sourceRect.h = tex.Height;
                sourceRect.x = 0;
                sourceRect.y = 0;

                int offsetX = _camera.X;
                int offsetY = _camera.Y;
                
                SDL.SDL_Rect targetRect; // @TODO Apply scale transformation
                targetRect.w = tex.Width;
                targetRect.h = tex.Height;
                targetRect.x = (int)sprite.Position.X + offsetX;
                targetRect.y = (int)sprite.Position.Y + offsetY;
                
                SDL.SDL_RenderCopy(
                    _sdlRenderer,
                    TextureHolder.GetSdlTexture(tex),
                    ref sourceRect, 
                    ref targetRect
                );
            }
        }
    }
}