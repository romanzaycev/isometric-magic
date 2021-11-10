using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SDL2;

namespace IsometricMagic.Engine
{
    public class Renderer
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
            _camera.Rect = new Rectangle()
            {
                Width = w,
                Height = h,
                X = _camera.Rect.X,
                Y = _camera.Rect.Y
            };
        }

        private void DrawSprites()
        {
            var scene = SceneManager.GetInstance().GetCurrent();
            
            DrawLayer(scene.MainLayer, true);
            DrawLayer(scene.UiLayer, false);
        }

        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        private void DrawLayer(SceneLayer layer, bool isCameraLayer)
        {
            var cameraRect = _camera.Rect;
            var cameraOffsetX = cameraRect.X;
            var cameraOffsetY = cameraRect.Y;
            
            foreach (var sprite in layer.Sprites)
            {
                if (sprite.Texture == null || !sprite.Visible) continue;

                var tex = sprite.Texture;
                    
                SDL.SDL_Rect sourceRect;
                sourceRect.w = tex.Width;
                sourceRect.h = tex.Height;
                sourceRect.x = 0;
                sourceRect.y = 0;

                var spriteTransformation = sprite.Transformation;
                
                var offsetX = (int) spriteTransformation.Translate.X;
                var offsetY = (int) spriteTransformation.Translate.Y;

                int spritePosX;
                int spritePosY;

                switch (sprite.OriginPoint)
                {
                    case OriginPoint.LeftTop:
                        spritePosX = (int)sprite.Position.X;
                        spritePosY = (int)sprite.Position.Y;
                        break;
                    
                    case OriginPoint.LeftCenter:
                        spritePosX = (int)sprite.Position.X;
                        spritePosY = (int)(sprite.Position.Y - sprite.Height / 2);
                        break;
                    
                    case OriginPoint.LeftBottom:
                        spritePosX = (int)sprite.Position.X;
                        spritePosY = (int)sprite.Position.Y - sprite.Height;
                        break;
                    
                    case OriginPoint.Centered:
                        spritePosX = (int)(sprite.Position.X - sprite.Width / 2);
                        spritePosY = (int)(sprite.Position.Y - sprite.Height / 2);
                        break;
                    
                    case OriginPoint.RightTop:
                        spritePosX = (int)sprite.Position.X - sprite.Width;
                        spritePosY = (int)sprite.Position.Y;
                        break;
                    
                    case OriginPoint.RightCenter:
                        spritePosX = (int)sprite.Position.X - sprite.Width;
                        spritePosY = (int)(sprite.Position.Y - sprite.Height / 2);
                        break;
                    
                    case OriginPoint.RightBottom:
                        spritePosX = (int)(sprite.Position.X - sprite.Width);
                        spritePosY = (int)(sprite.Position.Y - sprite.Height);
                        break;

                    case OriginPoint.TopCenter:
                        spritePosX = (int)(sprite.Position.X - sprite.Width / 2);
                        spritePosY = (int)sprite.Position.Y;
                        break;
                    
                    case OriginPoint.BottomCenter:
                        spritePosX = (int)(sprite.Position.X - sprite.Width / 2);
                        spritePosY = (int)(sprite.Position.Y - sprite.Height);
                        break;
                    
                    default:
                        throw new ArgumentException($"Unknown OriginPoint: {sprite.OriginPoint.ToString()}");
                }

                spritePosX += offsetX;
                spritePosY += offsetY;
                
                if (isCameraLayer)
                {
                    if (IsCulled(sprite.Width, sprite.Height, spritePosX, spritePosY, cameraRect))
                    {
                        continue;
                    }

                    spritePosX -= cameraOffsetX;
                    spritePosY -= cameraOffsetY;
                }
                
                SDL.SDL_Rect targetRect; // @TODO Apply scale transformation
                targetRect.w = tex.Width;
                targetRect.h = tex.Height;
                targetRect.x = spritePosX;
                targetRect.y = spritePosY;

                if (sprite.Transformation.Rotation.Angle == 0f)
                {
                    SDL.SDL_RenderCopy(
                        _sdlRenderer,
                        TextureHolder.GetSdlTexture(tex),
                        ref sourceRect,
                        ref targetRect
                    );
                }
                else
                {
                    var rotation = spriteTransformation.Rotation;
                    
                    SDL.SDL_Point pivotPoint;

                    switch (sprite.PivotMode)
                    {
                        case PivotMode.Centered:
                            pivotPoint.x = targetRect.w / 2;
                            pivotPoint.y = targetRect.h / 2;
                            break;
                        
                        default:
                            throw new ArgumentException($"Pivot mode {sprite.PivotMode.ToString()} not supported");
                    }
                    
                    SDL.SDL_RenderCopyEx(
                        _sdlRenderer,
                        TextureHolder.GetSdlTexture(tex),
                        ref sourceRect,
                        ref targetRect,
                        MathHelper.NorRotationToDegree((rotation.Clockwise) ? rotation.Angle : -rotation.Angle),
                        ref pivotPoint,
                        SDL.SDL_RendererFlip.SDL_FLIP_NONE
                    );
                }
            }
        }

        private static bool IsCulled(int width, int height, int x, int y, Rectangle cameraRect)
        {
            return y > cameraRect.Bottom || x > cameraRect.Right || y + height < cameraRect.Top || x + width < cameraRect.Left;
        }
    }
}