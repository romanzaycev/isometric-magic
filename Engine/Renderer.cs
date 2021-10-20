using System;
using SDL2;

namespace IsometricMagic.Engine
{
    class Renderer
    {
        private static readonly Application Application = Application.GetInstance();
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

        public void HandleWindowResized(int w, int h)
        {
            _camera.W = w;
            _camera.H = h;
        }

        private void DrawSprites()
        {
            var scene = SceneManager.GetInstance().GetCurrent();
            
            DrawLayer(scene.MainLayer, true);
            DrawLayer(scene.UiLayer, false);
        }

        private void DrawLayer(SceneLayer layer, bool isCameraLayer)
        {
            var sprites = layer.Sprites;
            
            Array.Sort(
                sprites,
                (spriteA, spriteB) => spriteA.Sorting.CompareTo(spriteB.Sorting)
            );
            
            foreach (var sprite in sprites)
            {
                if (sprite.Texture == null || !sprite.Visible) continue;

                var tex = sprite.Texture;
                    
                SDL.SDL_Rect sourceRect;
                sourceRect.w = tex.Width;
                sourceRect.h = tex.Height;
                sourceRect.x = 0;
                sourceRect.y = 0;

                int offsetX = (isCameraLayer) ? _camera.X : 0;
                int offsetY = (isCameraLayer) ? _camera.Y : 0;
                
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