using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Materials;
using IsometricMagic.Engine.Graphics.OpenGL;
using IsometricMagic.Engine.Graphics.Utilities;
using SDL2;
using Silk.NET.OpenGL;
using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;

namespace IsometricMagic.Engine.Graphics.SDL
{
    public class SdlGlGraphics : IGraphics
    {
        private static readonly DebugOverlayService DebugOverlay = DebugOverlayService.GetInstance();
        private static readonly FrameStats FrameStats = FrameStats.GetInstance();
        private GraphicsParams _graphicsParams = null!;
        private IntPtr _sdlWindow;
        private IntPtr _glContext;
        private IntPtr _debugFont = IntPtr.Zero;
        private GL _gl = null!;
        private GlRenderContext _renderContext = null!;
        private GlFullscreenQuad _fullscreenQuad = null!;
        private GlShaderProgram _presentShader = null!;
        private UnlitSpriteMaterial _defaultMaterial = null!;
        private OutlineSpriteMaterial _outlineMaterial = null!;

        private uint _spriteVao;
        private uint _spriteVbo;

        private GlRenderTarget _sceneTarget;
        private GlRenderTarget _pingTarget;
        private GlRenderTarget _pongTarget;
        private bool _targetsReady;

        private int _viewportWidth;
        private int _viewportHeight;

        private readonly Dictionary<string, GlNativeTexture> _generatedNormalMaps = new();
        private GlNativeTexture _neutralNormal = null!;

        public void Initialize(GraphicsParams graphicsParams)
        {
            _graphicsParams = graphicsParams;
            InitWindow();
            InitGl();
            InitResources();
            InitDebugFont();
        }

        public void Stop()
        {
            DestroyRenderTargets();

            foreach (var normal in _generatedNormalMaps.Values)
            {
                DeleteTexture(normal);
            }
            _generatedNormalMaps.Clear();

            if (_neutralNormal != null)
            {
                DeleteTexture(_neutralNormal);
            }

            if (_spriteVbo != 0)
            {
                _gl.DeleteBuffer(_spriteVbo);
            }

            if (_spriteVao != 0)
            {
                _gl.DeleteVertexArray(_spriteVao);
            }

            if (_glContext != IntPtr.Zero)
            {
                SDL_GL_DeleteContext(_glContext);
            }

            if (_sdlWindow != IntPtr.Zero)
            {
                SDL_DestroyWindow(_sdlWindow);
            }

            if (_debugFont != IntPtr.Zero)
            {
                TTF_CloseFont(_debugFont);
                _debugFont = IntPtr.Zero;
            }
        }

        public void RepaintWindow(out int width, out int height)
        {
            SDL_GL_GetDrawableSize(_sdlWindow, out var w, out var h);
            _viewportWidth = w;
            _viewportHeight = h;
            _gl.Viewport(0, 0, (uint) w, (uint) h);
            ResizeRenderTargets(w, h);
            width = w;
            height = h;
        }

        public void Draw(Scene scene, Camera camera)
        {
            if (_viewportWidth == 0 || _viewportHeight == 0)
            {
                SDL_GL_GetDrawableSize(_sdlWindow, out _viewportWidth, out _viewportHeight);
            }

            EnsureRenderTargets();

            _renderContext.Scene = scene;
            _renderContext.Camera = camera;
            _renderContext.ViewportWidth = _viewportWidth;
            _renderContext.ViewportHeight = _viewportHeight;
            _renderContext.Time += Application.DeltaTime;

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _sceneTarget.FramebufferId);
            _gl.Viewport(0, 0, (uint) _sceneTarget.Width, (uint) _sceneTarget.Height);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            DrawLayer(scene.MainLayer, camera, true);

            var finalTarget = ApplyPostProcess(scene.PostProcess);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.Viewport(0, 0, (uint) _viewportWidth, (uint) _viewportHeight);
            DrawPresent(finalTarget.TextureId);

            DrawLayer(scene.UiLayer, camera, false);
            DebugOverlay.Update(Application.DeltaTime);
            DrawDebugOverlay();

            SDL_GL_SwapWindow(_sdlWindow);
        }

        public NativeTexture CreateTexture(PixelFormat format, TextureAccess access, int width, int height)
        {
            if (format != PixelFormat.Rgba8888)
            {
                throw new NotImplementedException();
            }

            if (access != TextureAccess.Static && access != TextureAccess.Target)
            {
                throw new NotImplementedException();
            }

            if (access == TextureAccess.Target)
            {
                var target = CreateRenderTarget(width, height);
                return new GlNativeTexture(target.TextureId, target.FramebufferId, true, width, height);
            }

            var texture = CreateTextureFromData(width, height, null);
            return new GlNativeTexture(texture, 0, false, width, height);
        }

        public void DestroyTexture(NativeTexture nativeTexture)
        {
            if (nativeTexture is not GlNativeTexture glNativeTexture)
            {
                return;
            }

            DeleteTexture(glNativeTexture);
        }

        public void LoadImageToTexture(out NativeTexture nativeTexture, string imagePath)
        {
            var surface = IMG_Load(imagePath);
            if (surface == IntPtr.Zero)
            {
                throw new InvalidOperationException($"IMG_Load error: {IMG_GetError()}");
            }

            var targetFormat = BitConverter.IsLittleEndian ? SDL_PIXELFORMAT_ABGR8888 : SDL_PIXELFORMAT_RGBA8888;
            var converted = SDL_ConvertSurfaceFormat(surface, targetFormat, 0);
            SDL_FreeSurface(surface);
            if (converted == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_ConvertSurfaceFormat error: {SDL_GetError()}");
            }

            var surfaceInfo = Marshal.PtrToStructure<SDL_Surface>(converted);
            var width = surfaceInfo.w;
            var height = surfaceInfo.h;
            var pitch = surfaceInfo.pitch;

            var data = new byte[width * height * 4];
            SDL_LockSurface(converted);
            unsafe
            {
                var src = (byte*) surfaceInfo.pixels;
                for (var y = 0; y < height; y++)
                {
                    var row = src + (y * pitch);
                    Marshal.Copy((IntPtr) row, data, y * width * 4, width * 4);
                }
            }
            SDL_UnlockSurface(converted);
            SDL_FreeSurface(converted);

            var textureId = CreateTextureFromData(width, height, data);
            nativeTexture = new GlNativeTexture(textureId, 0, false, width, height);
        }

        private void InitWindow()
        {
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 3);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int) SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);

            _sdlWindow = SDL_CreateWindow(
                "Isometric Magic",
                SDL_WINDOWPOS_CENTERED,
                SDL_WINDOWPOS_CENTERED,
                _graphicsParams.InitialWindowWidth,
                _graphicsParams.InitialWindowHeight,
                SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI
            );

            if (_sdlWindow == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_CreateWindow error: {SDL_GetError()}");
            }

            if (_graphicsParams.IsFullscreen)
            {
                SDL_SetWindowFullscreen(_sdlWindow, (uint) SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
            }
        }

        private void InitGl()
        {
            _glContext = SDL_GL_CreateContext(_sdlWindow);
            if (_glContext == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_GL_CreateContext error: {SDL_GetError()}");
            }

            SDL_GL_MakeCurrent(_sdlWindow, _glContext);
            SDL_GL_SetSwapInterval(_graphicsParams.VSync ? 1 : 0);

            _gl = GL.GetApi(SDL_GL_GetProcAddress);
            _gl.ClearColor(0f, 0f, 0f, 1f);
            _gl.Disable(EnableCap.DepthTest);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            SDL_GL_GetDrawableSize(_sdlWindow, out _viewportWidth, out _viewportHeight);
            _gl.Viewport(0, 0, (uint) _viewportWidth, (uint) _viewportHeight);
        }

        private void InitResources()
        {
            _fullscreenQuad = new GlFullscreenQuad(_gl);
            _renderContext = new GlRenderContext(_gl, _fullscreenQuad, new Scene("bootstrap"),
                new Camera(_graphicsParams.InitialWindowWidth, _graphicsParams.InitialWindowHeight));

            _presentShader = new GlShaderProgram(_gl, PresentVertexSource, PresentFragmentSource);
            _defaultMaterial = new UnlitSpriteMaterial(_gl);
            _outlineMaterial = new OutlineSpriteMaterial();

            _spriteVao = _gl.GenVertexArray();
            _spriteVbo = _gl.GenBuffer();

            _gl.BindVertexArray(_spriteVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _spriteVbo);
            unsafe
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (6 * 6 * sizeof(float)), null,
                    BufferUsageARB.DynamicDraw);
            }

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float),
                2 * sizeof(float));
            _gl.EnableVertexAttribArray(2);
            _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float),
                4 * sizeof(float));

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);

            _neutralNormal = CreateNeutralNormal();
        }

        private void DrawPresent(uint textureId)
        {
            _presentShader.Use();
            _presentShader.SetInt("u_texture", 0);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, textureId);
            _fullscreenQuad.Draw();
            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private GlRenderTarget ApplyPostProcess(PostProcessStack stack)
        {
            if (!stack.Enabled || stack.Effects.Count == 0)
            {
                return _sceneTarget;
            }

            var input = _sceneTarget;
            var usePing = true;
            var applied = false;

            foreach (var effect in stack.Effects)
            {
                if (effect is not IGlPostProcessEffect glEffect || !effect.Enabled)
                {
                    continue;
                }

                var output = usePing ? _pingTarget : _pongTarget;
                glEffect.Apply(_renderContext, input, output);
                input = output;
                usePing = !usePing;
                applied = true;
            }

            return applied ? input : _sceneTarget;
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
            var cameraOffsetX = cameraRect.X + camera.SubpixelOffset.X;
            var cameraOffsetY = cameraRect.Y + camera.SubpixelOffset.Y;

            foreach (var sprite in layer.Sprites)
            {
                if (sprite.Texture == null || !sprite.Visible)
                {
                    continue;
                }

                var tex = sprite.Texture;
                var albedo = TextureHolder.GetInstance().GetNativeTexture(tex) as GlNativeTexture;
                if (albedo == null)
                {
                    continue;
                }

                var spriteTransformation = sprite.Transformation;
                var offsetX = spriteTransformation.Translate.X;
                var offsetY = spriteTransformation.Translate.Y;

                float spritePosX;
                float spritePosY;

                switch (sprite.OriginPoint)
                {
                    case OriginPoint.LeftTop:
                        spritePosX = sprite.Position.X;
                        spritePosY = sprite.Position.Y;
                        break;

                    case OriginPoint.LeftCenter:
                        spritePosX = sprite.Position.X;
                        spritePosY = sprite.Position.Y - sprite.Height / 2f;
                        break;

                    case OriginPoint.LeftBottom:
                        spritePosX = sprite.Position.X;
                        spritePosY = sprite.Position.Y - sprite.Height;
                        break;

                    case OriginPoint.Centered:
                        spritePosX = sprite.Position.X - sprite.Width / 2f;
                        spritePosY = sprite.Position.Y - sprite.Height / 2f;
                        break;

                    case OriginPoint.RightTop:
                        spritePosX = sprite.Position.X - sprite.Width;
                        spritePosY = sprite.Position.Y;
                        break;

                    case OriginPoint.RightCenter:
                        spritePosX = sprite.Position.X - sprite.Width;
                        spritePosY = sprite.Position.Y - sprite.Height / 2f;
                        break;

                    case OriginPoint.RightBottom:
                        spritePosX = sprite.Position.X - sprite.Width;
                        spritePosY = sprite.Position.Y - sprite.Height;
                        break;

                    case OriginPoint.TopCenter:
                        spritePosX = sprite.Position.X - sprite.Width / 2f;
                        spritePosY = sprite.Position.Y;
                        break;

                    case OriginPoint.BottomCenter:
                        spritePosX = sprite.Position.X - sprite.Width / 2f;
                        spritePosY = sprite.Position.Y - sprite.Height;
                        break;

                    default:
                        throw new ArgumentException($"Unknown OriginPoint: {sprite.OriginPoint.ToString()}");
                }

                spritePosX += offsetX;
                spritePosY += offsetY;

                var worldSpritePosX = spritePosX;
                var worldSpritePosY = spritePosY;
                var screenSpritePosX = spritePosX;
                var screenSpritePosY = spritePosY;

                if (isCameraLayer)
                {
                    if (IsCulled(sprite.Width, sprite.Height, screenSpritePosX, screenSpritePosY, cameraRect))
                    {
                        FrameStats.AddSpriteCulled();
                        continue;
                    }

                    screenSpritePosX -= cameraOffsetX;
                    screenSpritePosY -= cameraOffsetY;
                }

                var vertices = BuildQuadVertices(
                    worldSpritePosX, worldSpritePosY,
                    screenSpritePosX, screenSpritePosY,
                    sprite.Width, sprite.Height,
                    sprite.Transformation.Rotation.Angle,
                    sprite.Transformation.Rotation.Clockwise
                );
                var outline = sprite.Outline;
                var outlineEnabled = outline.Enabled && outline.ThicknessTexels > 0f && outline.Color.W > 0f;

                if (outlineEnabled && outline.Layering == OutlineLayering.Under)
                {
                    DrawOutline(sprite, albedo, worldSpritePosX, worldSpritePosY, screenSpritePosX, screenSpritePosY);
                }

                var material = ResolveMaterial(sprite);
                if (material == null)
                {
                    continue;
                }

                var normalMap = ResolveNormalMap(sprite);
                DrawSprite(vertices, material, sprite, albedo, normalMap);

                if (outlineEnabled && outline.Layering == OutlineLayering.Over)
                {
                    DrawOutline(sprite, albedo, worldSpritePosX, worldSpritePosY, screenSpritePosX, screenSpritePosY);
                }
            }
        }

        private void DrawSprite(float[] vertices, IGlMaterial material, Sprite sprite, GlNativeTexture albedo,
            GlNativeTexture? normalMap)
        {
            UpdateSpriteBuffer(vertices);
            material.Bind(_renderContext, sprite, albedo, normalMap);

            _gl.BindVertexArray(_spriteVao);
            FrameStats.AddDrawCall();
            FrameStats.AddSpriteDrawn();
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            _gl.BindVertexArray(0);

            material.Unbind(_renderContext);
        }

        private void DrawOutline(Sprite sprite, GlNativeTexture albedo,
            float worldSpritePosX, float worldSpritePosY,
            float screenSpritePosX, float screenSpritePosY)
        {
            var pad = (int) MathF.Ceiling(sprite.Outline.ThicknessTexels);
            if (pad <= 0)
            {
                return;
            }

            var outlineWidth = sprite.Width + pad * 2;
            var outlineHeight = sprite.Height + pad * 2;
            var outlineWorldX = worldSpritePosX - pad;
            var outlineWorldY = worldSpritePosY - pad;
            var outlineScreenX = screenSpritePosX - pad;
            var outlineScreenY = screenSpritePosY - pad;

            var uvPadX = pad / (float) sprite.Width;
            var uvPadY = pad / (float) sprite.Height;

            var outlineVertices = BuildQuadVertices(
                outlineWorldX, outlineWorldY,
                outlineScreenX, outlineScreenY,
                outlineWidth, outlineHeight,
                sprite.Transformation.Rotation.Angle,
                sprite.Transformation.Rotation.Clockwise,
                -uvPadX, -uvPadY,
                1f + uvPadX, 1f + uvPadY
            );

            DrawSprite(outlineVertices, _outlineMaterial, sprite, albedo, null);
        }

        private IGlMaterial? ResolveMaterial(Sprite sprite)
        {
            if (sprite.Material is IGlMaterial glMaterial && glMaterial.Enabled)
            {
                return glMaterial;
            }

            return _defaultMaterial;
        }

        private GlNativeTexture? ResolveNormalMap(Sprite sprite)
        {
            if (sprite.Material is not NormalMappedLitSpriteMaterial &&
                sprite.Material is not EmissiveNormalMappedLitSpriteMaterial)
            {
                return null;
            }

            if (sprite.NormalMap != null)
            {
                return TextureHolder.GetInstance().GetNativeTexture(sprite.NormalMap) as GlNativeTexture;
            }

            var imagePath = sprite.Texture?.ImagePath;
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return _neutralNormal;
            }

            if (_generatedNormalMaps.TryGetValue(imagePath, out var cached))
            {
                return cached;
            }

            var data = NormalMapGenerator.GenerateFromImage(imagePath, out var width, out var height, 1.2f);
            var textureId = CreateTextureFromData(width, height, data);
            var generated = new GlNativeTexture(textureId, 0, false, width, height);
            _generatedNormalMaps[imagePath] = generated;
            return generated;
        }

        private float[] BuildQuadVertices(
            float worldX, float worldY,
            float screenX, float screenY,
            int width, int height,
            double angle, bool clockwise)
        {
            return BuildQuadVertices(worldX, worldY, screenX, screenY, width, height, angle, clockwise, 0f, 0f, 1f, 1f);
        }

        private float[] BuildQuadVertices(
            float worldX, float worldY,
            float screenX, float screenY,
            int width, int height,
            double angle, bool clockwise,
            float uvMinX, float uvMinY,
            float uvMaxX, float uvMaxY)
        {
            var rotationDeg = MathHelper.NorRotationToDegree(clockwise ? angle : -angle);
            var rotationRad = (float) (rotationDeg * Math.PI / 180f);

            float worldX0 = worldX;
            float worldY0 = worldY;
            float worldX1 = worldX + width;
            float worldY1 = worldY + height;
            float worldCenterX = worldX + width / 2f;
            float worldCenterY = worldY + height / 2f;

            var worldTl = RotatePoint(worldX0, worldY0, worldCenterX, worldCenterY, rotationRad);
            var worldTr = RotatePoint(worldX1, worldY0, worldCenterX, worldCenterY, rotationRad);
            var worldBr = RotatePoint(worldX1, worldY1, worldCenterX, worldCenterY, rotationRad);
            var worldBl = RotatePoint(worldX0, worldY1, worldCenterX, worldCenterY, rotationRad);

            float screenX0 = screenX;
            float screenY0 = screenY;
            float screenX1 = screenX + width;
            float screenY1 = screenY + height;
            float screenCenterX = screenX + width / 2f;
            float screenCenterY = screenY + height / 2f;

            var screenTl = RotatePoint(screenX0, screenY0, screenCenterX, screenCenterY, rotationRad);
            var screenTr = RotatePoint(screenX1, screenY0, screenCenterX, screenCenterY, rotationRad);
            var screenBr = RotatePoint(screenX1, screenY1, screenCenterX, screenCenterY, rotationRad);
            var screenBl = RotatePoint(screenX0, screenY1, screenCenterX, screenCenterY, rotationRad);

            return new float[]
            {
                ToNdcX(screenTl.X), ToNdcY(screenTl.Y), uvMinX, uvMinY, worldTl.X, worldTl.Y,
                ToNdcX(screenTr.X), ToNdcY(screenTr.Y), uvMaxX, uvMinY, worldTr.X, worldTr.Y,
                ToNdcX(screenBr.X), ToNdcY(screenBr.Y), uvMaxX, uvMaxY, worldBr.X, worldBr.Y,
                ToNdcX(screenTl.X), ToNdcY(screenTl.Y), uvMinX, uvMinY, worldTl.X, worldTl.Y,
                ToNdcX(screenBr.X), ToNdcY(screenBr.Y), uvMaxX, uvMaxY, worldBr.X, worldBr.Y,
                ToNdcX(screenBl.X), ToNdcY(screenBl.Y), uvMinX, uvMaxY, worldBl.X, worldBl.Y
            };
        }

        private void UpdateSpriteBuffer(float[] vertices)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _spriteVbo);
            unsafe
            {
                fixed (float* data = vertices)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint) (vertices.Length * sizeof(float)),
                        data);
                }
            }
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        private float ToNdcX(float x)
        {
            return (x / _viewportWidth) * 2f - 1f;
        }

        private float ToNdcY(float y)
        {
            return 1f - (y / _viewportHeight) * 2f;
        }

        private static PointF RotatePoint(float x, float y, float cx, float cy, float angleRad)
        {
            var cos = MathF.Cos(angleRad);
            var sin = MathF.Sin(angleRad);
            var dx = x - cx;
            var dy = y - cy;
            var rx = dx * cos - dy * sin + cx;
            var ry = dx * sin + dy * cos + cy;
            return new PointF(rx, ry);
        }

        private void EnsureRenderTargets()
        {
            if (_targetsReady)
            {
                return;
            }

            _sceneTarget = CreateHdrRenderTarget(_viewportWidth, _viewportHeight);
            _pingTarget = CreateHdrRenderTarget(_viewportWidth, _viewportHeight);
            _pongTarget = CreateHdrRenderTarget(_viewportWidth, _viewportHeight);
            _targetsReady = true;
        }

        private void ResizeRenderTargets(int width, int height)
        {
            if (!_targetsReady)
            {
                return;
            }

            DestroyRenderTargets();
            _sceneTarget = CreateHdrRenderTarget(width, height);
            _pingTarget = CreateHdrRenderTarget(width, height);
            _pongTarget = CreateHdrRenderTarget(width, height);
            _targetsReady = true;
        }

        private void DestroyRenderTargets()
        {
            if (!_targetsReady)
            {
                return;
            }

            DeleteRenderTarget(_sceneTarget);
            DeleteRenderTarget(_pingTarget);
            DeleteRenderTarget(_pongTarget);
            _targetsReady = false;
        }

        private GlRenderTarget CreateRenderTarget(int width, int height)
        {
            var textureId = CreateTextureFromData(width, height, null);
            var framebufferId = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, textureId, 0);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException($"Framebuffer incomplete: {status}");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return new GlRenderTarget(framebufferId, textureId, width, height);
        }

        private GlRenderTarget CreateHdrRenderTarget(int width, int height)
        {
            var textureId = CreateHdrTexture(width, height);
            var framebufferId = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, textureId, 0);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException($"Framebuffer incomplete: {status}");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return new GlRenderTarget(framebufferId, textureId, width, height);
        }

        private void DeleteRenderTarget(GlRenderTarget target)
        {
            if (target.FramebufferId != 0)
            {
                _gl.DeleteFramebuffer(target.FramebufferId);
            }

            if (target.TextureId != 0)
            {
                _gl.DeleteTexture(target.TextureId);
            }
        }

        private uint CreateTextureFromData(int width, int height, byte[]? data)
        {
            var textureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, textureId);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

            unsafe
            {
                fixed (byte* ptr = data)
                {
                    var dataPtr = data == null ? null : ptr;
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) width, (uint) height, 0,
                        Silk.NET.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
                }
            }

            _gl.BindTexture(TextureTarget.Texture2D, 0);
            return textureId;
        }

        private uint CreateHdrTexture(int width, int height)
        {
            var textureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, textureId);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);

            unsafe
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint) width, (uint) height, 0,
                    Silk.NET.OpenGL.PixelFormat.Rgba, PixelType.HalfFloat, null);
            }

            _gl.BindTexture(TextureTarget.Texture2D, 0);
            return textureId;
        }

        private void DeleteTexture(GlNativeTexture texture)
        {
            if (texture.FramebufferId != 0)
            {
                _gl.DeleteFramebuffer(texture.FramebufferId);
            }

            if (texture.TextureId != 0)
            {
                _gl.DeleteTexture(texture.TextureId);
            }
        }

        private GlNativeTexture CreateNeutralNormal()
        {
            var data = new byte[] { 128, 128, 255, 255 };
            var textureId = CreateTextureFromData(1, 1, data);
            return new GlNativeTexture(textureId, 0, false, 1, 1);
        }

        private static bool IsCulled(int width, int height, float x, float y, Rectangle cameraRect)
        {
            return y > cameraRect.Bottom || x > cameraRect.Right || y + height < cameraRect.Top ||
                   x + width < cameraRect.Left;
        }

        private void InitDebugFont()
        {
            if (!DebugOverlay.Enabled)
            {
                return;
            }

            if (!File.Exists(DebugOverlay.FontPath))
            {
                return;
            }

            _debugFont = TTF_OpenFont(DebugOverlay.FontPath, DebugOverlay.FontSize);
        }

        private void DrawDebugOverlay()
        {
            if (!DebugOverlay.Visible || _debugFont == IntPtr.Zero)
            {
                return;
            }

            var x = DebugOverlay.PosX;
            var y = DebugOverlay.PosY;
            const int lineGap = 2;

            foreach (var line in DebugOverlay.Lines)
            {
                var text = string.IsNullOrEmpty(line) ? " " : line;
                if (!TryCreateTextTexture(text, out var texture, out var width, out var height))
                {
                    continue;
                }

                var sprite = new Sprite(width, height);
                var vertices = BuildQuadVertices(x, y, x, y, width, height, 0d, true);
                DrawSprite(vertices, _defaultMaterial, sprite, texture, null);
                y += height + lineGap;

                DeleteTexture(texture);
            }
        }

        private bool TryCreateTextTexture(string text, out GlNativeTexture texture, out int width, out int height)
        {
            texture = null!;
            width = 0;
            height = 0;

            SDL2.SDL.SDL_Color color;
            color.r = 230;
            color.g = 255;
            color.b = 230;
            color.a = 255;

            var surface = TTF_RenderUTF8_Blended(_debugFont, text, color);
            if (surface == IntPtr.Zero)
            {
                return false;
            }

            var targetFormat = BitConverter.IsLittleEndian ? SDL_PIXELFORMAT_ABGR8888 : SDL_PIXELFORMAT_RGBA8888;
            var converted = SDL_ConvertSurfaceFormat(surface, targetFormat, 0);
            SDL_FreeSurface(surface);

            if (converted == IntPtr.Zero)
            {
                return false;
            }

            var surfaceInfo = Marshal.PtrToStructure<SDL_Surface>(converted);
            width = surfaceInfo.w;
            height = surfaceInfo.h;
            var pitch = surfaceInfo.pitch;

            var data = new byte[width * height * 4];
            SDL_LockSurface(converted);
            unsafe
            {
                var src = (byte*) surfaceInfo.pixels;
                for (var rowIndex = 0; rowIndex < height; rowIndex++)
                {
                    var row = src + (rowIndex * pitch);
                    Marshal.Copy((IntPtr) row, data, rowIndex * width * 4, width * 4);
                }
            }
            SDL_UnlockSurface(converted);
            SDL_FreeSurface(converted);

            var textureId = CreateTextureFromData(width, height, data);
            texture = new GlNativeTexture(textureId, 0, false, width, height);
            return true;
        }

        private const string PresentVertexSource = @"#version 330 core
layout(location = 0) in vec2 a_pos;
layout(location = 1) in vec2 a_uv;

out vec2 v_uv;

void main()
{
    v_uv = a_uv;
    gl_Position = vec4(a_pos.xy, 0.0, 1.0);
}
";

        private const string PresentFragmentSource = @"#version 330 core
in vec2 v_uv;
out vec4 FragColor;

uniform sampler2D u_texture;

void main()
{
    FragColor = texture(u_texture, v_uv);
}
";
    }
}
