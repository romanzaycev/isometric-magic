using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SDL2;

namespace IsometricMagic.Engine.Graphics
{
    public class SdlGraphics : IGraphics
    {
        private static int SDL_TEXTUREACCESS_STATIC = 0;
        // private static int SDL_TEXTUREACCESS_STREAMING = 1;
        private static int SDL_TEXTUREACCESS_TARGET = 2;
        
        private GraphicsParams _graphicsParams;
        private IntPtr _sdlWindow;
        private IntPtr _sdlRenderer;
        private readonly Dictionary<NativeTexture, IntPtr> _sdlSurfaces = new();

        public void Initialize(GraphicsParams graphicsParams)
        {
            _graphicsParams = graphicsParams;
            InitSdl();
            InitWindow();
            InitRenderer();
        }

        public void Stop()
        {
            if (_sdlRenderer != IntPtr.Zero)
            {
                SDL.SDL_DestroyRenderer(_sdlRenderer);
            }

            if (_sdlWindow != IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(_sdlWindow);
            }
        }

        public void RepaintWindow(out int width, out int height)
        {
            var windowSurface = SDL.SDL_GetWindowSurface(_sdlWindow);
            SDL.SDL_GetWindowSize(_sdlWindow, out var w, out var h);

            SDL.SDL_Rect windowRect;
            windowRect.x = 0;
            windowRect.y = 0;
            windowRect.w = w;
            windowRect.h = h;

            SDL.SDL_FillRect(windowSurface, ref windowRect, 0xff000000);
            SDL.SDL_UpdateWindowSurface(_sdlWindow);

            width = w;
            height = h;
        }

        public void Draw(Scene scene, Camera camera)
        {
            SDL.SDL_SetRenderDrawColor(_sdlRenderer, 0x00, 0x00, 0x00, 0x00);
            SDL.SDL_RenderClear(_sdlRenderer);

            DrawSprites(scene, camera);

            SDL.SDL_RenderPresent(_sdlRenderer);
        }

        public NativeTexture CreateTexture(PixelFormat format, TextureAccess access, int width, int height)
        {
            if (format != PixelFormat.Rgba8888)
            {
                throw new NotImplementedException();
            }

            if (access != TextureAccess.Static || access != TextureAccess.Target)
            {
                throw new NotImplementedException();
            }

            return new NativeTexture(
                SDL.SDL_CreateTexture(
                    _sdlRenderer,
                    SDL.SDL_PIXELFORMAT_RGBA8888,
                    (access == TextureAccess.Target) ? SDL_TEXTUREACCESS_TARGET : SDL_TEXTUREACCESS_STATIC,
                    width,
                    height
                )
            );
        }

        public void DestroyTexture(NativeTexture nativeTexture)
        {
            SDL.SDL_DestroyTexture(nativeTexture.Holder);
            
            if (_sdlSurfaces.ContainsKey(nativeTexture))
            {
                SDL.SDL_FreeSurface(_sdlSurfaces[nativeTexture]);
                _sdlSurfaces.Remove(nativeTexture);
            }
        }

        public void LoadImageToTexture(out NativeTexture nativeTexture, string imagePath)
        {
            var sdlSurface = SDL_image.IMG_Load(imagePath);

            if (sdlSurface == IntPtr.Zero)
            {
                throw new Exception($"IMG_Load error: {SDL_image.IMG_GetError()}");
            }

            var sdlTexture = SDL.SDL_CreateTextureFromSurface(
                _sdlRenderer,
                sdlSurface
            );

            nativeTexture = new NativeTexture(sdlTexture);
            _sdlSurfaces.Add(nativeTexture, sdlSurface);
        }
        
        private void InitSdl()
        {
            var sdlInitResult = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            if (sdlInitResult < 0)
            {
                throw new InvalidOperationException($"SDL_Init error: {SDL.SDL_GetError()}");
            }

            var sdlImageInitResult = SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_JPG |
                                                        SDL_image.IMG_InitFlags.IMG_INIT_PNG |
                                                        SDL_image.IMG_InitFlags.IMG_INIT_WEBP |
                                                        SDL_image.IMG_InitFlags.IMG_INIT_TIF);

            if (sdlImageInitResult < 0)
            {
                throw new InvalidOperationException($"IMG_Init error: {SDL_image.IMG_GetError()}");
            }

            SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "best");
        }

        private void InitWindow()
        {
            _sdlWindow = SDL.SDL_CreateWindow(
                "Isometric Magic",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                _graphicsParams.InitialWindowWidth,
                _graphicsParams.InitialWindowHeight,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            if (_sdlWindow == IntPtr.Zero)
            {
                Stop();
                throw new Exception($"SDL_CreateWindow error: {SDL.SDL_GetError()}");
            }

            if (_graphicsParams.IsFullscreen)
            {
                SDL.SDL_SetWindowFullscreen(_sdlWindow, (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
            }
        }

        private void InitRenderer()
        {
            var flags = SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED;

            if (_graphicsParams.VSync) flags |= SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC;

            _sdlRenderer = SDL.SDL_CreateRenderer(
                _sdlWindow,
                -1,
                flags
            );

            if (_sdlRenderer == IntPtr.Zero)
            {
                Stop();
                throw new InvalidOperationException($"SDL_CreateRenderer error: {SDL.SDL_GetError()}");
            }
        }

        private void DrawSprites(Scene scene, Camera camera)
        {
            DrawLayer(scene.MainLayer, camera, true);
            DrawLayer(scene.UiLayer, camera, false);
        }

        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        private void DrawLayer(SceneLayer layer, Camera camera, bool isCameraLayer)
        {
            var cameraRect = camera.Rect;
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
                        spritePosX = (int) sprite.Position.X;
                        spritePosY = (int) sprite.Position.Y;
                        break;

                    case OriginPoint.LeftCenter:
                        spritePosX = (int) sprite.Position.X;
                        spritePosY = (int) (sprite.Position.Y - sprite.Height / 2);
                        break;

                    case OriginPoint.LeftBottom:
                        spritePosX = (int) sprite.Position.X;
                        spritePosY = (int) sprite.Position.Y - sprite.Height;
                        break;

                    case OriginPoint.Centered:
                        spritePosX = (int) (sprite.Position.X - sprite.Width / 2);
                        spritePosY = (int) (sprite.Position.Y - sprite.Height / 2);
                        break;

                    case OriginPoint.RightTop:
                        spritePosX = (int) sprite.Position.X - sprite.Width;
                        spritePosY = (int) sprite.Position.Y;
                        break;

                    case OriginPoint.RightCenter:
                        spritePosX = (int) sprite.Position.X - sprite.Width;
                        spritePosY = (int) (sprite.Position.Y - sprite.Height / 2);
                        break;

                    case OriginPoint.RightBottom:
                        spritePosX = (int) (sprite.Position.X - sprite.Width);
                        spritePosY = (int) (sprite.Position.Y - sprite.Height);
                        break;

                    case OriginPoint.TopCenter:
                        spritePosX = (int) (sprite.Position.X - sprite.Width / 2);
                        spritePosY = (int) sprite.Position.Y;
                        break;

                    case OriginPoint.BottomCenter:
                        spritePosX = (int) (sprite.Position.X - sprite.Width / 2);
                        spritePosY = (int) (sprite.Position.Y - sprite.Height);
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
                        TextureHolder.GetInstance().GetNativeTexture(tex).Holder,
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
                        TextureHolder.GetInstance().GetNativeTexture(tex).Holder,
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
            return y > cameraRect.Bottom || x > cameraRect.Right || y + height < cameraRect.Top ||
                   x + width < cameraRect.Left;
        }
    }
}