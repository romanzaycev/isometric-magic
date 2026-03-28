using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using static SDL2.SDL;
using static SDL2.SDL_image;

namespace IsometricMagic.Engine.Graphics.SDL
{
    public class SdlGraphics : IGraphics
    {
        private static int SDL_TEXTUREACCESS_STATIC = 0;
        // private static int SDL_TEXTUREACCESS_STREAMING = 1;
        private static int SDL_TEXTUREACCESS_TARGET = 2;
        
        private GraphicsParams _graphicsParams = null!;
        private IntPtr _sdlWindow;
        private IntPtr _sdlRenderer;
        private readonly Dictionary<NativeTexture, IntPtr> _sdlSurfaces = new();

        public void Initialize(GraphicsParams graphicsParams)
        {
            _graphicsParams = graphicsParams;
            InitWindow();
            InitRenderer();
        }

        public void Stop()
        {
            if (_sdlRenderer != IntPtr.Zero)
            {
                SDL_DestroyRenderer(_sdlRenderer);
            }

            if (_sdlWindow != IntPtr.Zero)
            {
                SDL_DestroyWindow(_sdlWindow);
            }
        }

        public void RepaintWindow(out int width, out int height)
        {
            var windowSurface = SDL_GetWindowSurface(_sdlWindow);
            SDL_GetWindowSize(_sdlWindow, out var w, out var h);

            SDL_Rect windowRect;
            windowRect.x = 0;
            windowRect.y = 0;
            windowRect.w = w;
            windowRect.h = h;

            SDL_FillRect(windowSurface, ref windowRect, 0xff000000);
            SDL_UpdateWindowSurface(_sdlWindow);

            width = w;
            height = h;
        }

        public void Draw(Scene scene, Camera camera)
        {
            SDL_SetRenderDrawColor(_sdlRenderer, 0x00, 0x00, 0x00, 0x00);
            SDL_RenderClear(_sdlRenderer);

            DrawSprites(scene, camera);

            SDL_RenderPresent(_sdlRenderer);
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
                SDL_CreateTexture(
                    _sdlRenderer,
                    SDL_PIXELFORMAT_RGBA8888,
                    (access == TextureAccess.Target) ? SDL_TEXTUREACCESS_TARGET : SDL_TEXTUREACCESS_STATIC,
                    width,
                    height
                )
            );
        }

        public void DestroyTexture(NativeTexture nativeTexture)
        {
            SDL_DestroyTexture(nativeTexture.Holder);
            
            if (_sdlSurfaces.ContainsKey(nativeTexture))
            {
                SDL_FreeSurface(_sdlSurfaces[nativeTexture]);
                _sdlSurfaces.Remove(nativeTexture);
            }
        }

        public void LoadImageToTexture(out NativeTexture nativeTexture, string imagePath)
        {
            var sdlSurface = IMG_Load(imagePath);

            if (sdlSurface == IntPtr.Zero)
            {
                throw new Exception($"IMG_Load error: {IMG_GetError()}");
            }

            var sdlTexture = SDL_CreateTextureFromSurface(
                _sdlRenderer,
                sdlSurface
            );

            nativeTexture = new NativeTexture(sdlTexture);
            _sdlSurfaces.Add(nativeTexture, sdlSurface);
        }
        
        private void InitWindow()
        {
            _sdlWindow = SDL_CreateWindow(
                "Isometric Magic",
                SDL_WINDOWPOS_CENTERED,
                SDL_WINDOWPOS_CENTERED,
                _graphicsParams.InitialWindowWidth,
                _graphicsParams.InitialWindowHeight,
                SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            if (_sdlWindow == IntPtr.Zero)
            {
                Stop();
                throw new Exception($"SDL_CreateWindow error: {SDL_GetError()}");
            }

            if (_graphicsParams.IsFullscreen)
            {
                SDL_SetWindowFullscreen(_sdlWindow, (uint) SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
            }
        }

        private void InitRenderer()
        {
            var flags = SDL_RendererFlags.SDL_RENDERER_ACCELERATED;

            if (_graphicsParams.VSync) flags |= SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC;

            _sdlRenderer = SDL_CreateRenderer(
                _sdlWindow,
                0,
                flags
            );

            if (_sdlRenderer == IntPtr.Zero)
            {
                Stop();
                throw new InvalidOperationException($"SDL_CreateRenderer error: {SDL_GetError()}");
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

                SDL_Rect sourceRect;
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

                SDL_Rect targetRect; // @TODO Apply scale transformation
                targetRect.w = tex.Width;
                targetRect.h = tex.Height;
                targetRect.x = spritePosX;
                targetRect.y = spritePosY;

                if (sprite.Transformation.Rotation.Angle == 0f)
                {
                    SDL_RenderCopy(
                        _sdlRenderer,
                        TextureHolder.GetInstance().GetNativeTexture(tex).Holder,
                        ref sourceRect,
                        ref targetRect
                    );
                }
                else
                {
                    var rotation = spriteTransformation.Rotation;

                    SDL_Point pivotPoint;

                    switch (sprite.PivotMode)
                    {
                        case PivotMode.Centered:
                            pivotPoint.x = targetRect.w / 2;
                            pivotPoint.y = targetRect.h / 2;
                            break;

                        default:
                            throw new ArgumentException($"Pivot mode {sprite.PivotMode.ToString()} not supported");
                    }

                    SDL_RenderCopyEx(
                        _sdlRenderer,
                        TextureHolder.GetInstance().GetNativeTexture(tex).Holder,
                        ref sourceRect,
                        ref targetRect,
                        MathHelper.NorRotationToDegree((rotation.Clockwise) ? rotation.Angle : -rotation.Angle),
                        ref pivotPoint,
                        SDL_RendererFlip.SDL_FLIP_NONE
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
