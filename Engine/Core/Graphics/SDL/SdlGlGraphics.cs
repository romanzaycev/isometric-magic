using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using IsometricMagic.Engine.App;
using IsometricMagic.Engine.Assets;
using IsometricMagic.Engine.Diagnostics;
using IsometricMagic.Engine.Core.Graphics;
using IsometricMagic.Engine.Graphics.Effects;
using IsometricMagic.Engine.Graphics.Materials;
using IsometricMagic.Engine.Graphics.OpenGL;
using IsometricMagic.Engine.Graphics.Utilities;
using IsometricMagic.Engine.Inputs;
using IsometricMagic.Engine.Rendering;
using IsometricMagic.Engine.Scenes;
using IsometricMagic.Engine.Core.Assets;
using EngineTexture = IsometricMagic.Engine.Assets.Texture;
using SDL2;
using Silk.NET.OpenGL;
using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;

namespace IsometricMagic.Engine.Core.Graphics.SDL
{
    internal sealed class SdlGlGraphics : IGraphics
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
        private GlShaderProgram _blendCompositeShader = null!;
        private bool _blendCompositeSamplersInitialized;
        private StandardSpriteMaterial _defaultMaterial = null!;
        private OutlineSpriteMaterial _outlineMaterial = null!;
        private long _frameId;

        private const int SpriteVertexCount = 6;
        private const int SpriteVertexStrideFloats = 10;
        private const int SpriteFloatCount = SpriteVertexCount * SpriteVertexStrideFloats;

        private uint _spriteVao;
        private uint _spriteVbo;
        private int _spriteBufferCapacityFloats;

        private float[] _singleSpriteVertices = new float[SpriteFloatCount];
        private float[] _batchedVertices = new float[SpriteFloatCount * 256];

        private enum BlendStateMode
        {
            Unknown,
            Disabled,
            AlphaBlend,
        }

        private BlendStateMode _blendState = BlendStateMode.Unknown;
        private uint _boundFramebufferId = uint.MaxValue;
        private int _boundViewportWidth = -1;
        private int _boundViewportHeight = -1;

        private uint _backgroundRectTextureId;
        private int _backgroundRectTextureWidth;
        private int _backgroundRectTextureHeight;

        private readonly struct ScreenRect
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Width;
            public readonly int Height;

            public ScreenRect(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        private readonly struct SpriteBatchKey : IEquatable<SpriteBatchKey>
        {
            public readonly uint AlbedoTextureId;
            public readonly uint NormalTextureId;
            public readonly uint EmissionTextureId;
            public readonly int ShadingModel;
            public readonly int NormalMapMode;
            public readonly int EmissionColorX;
            public readonly int EmissionColorY;
            public readonly int EmissionColorZ;
            public readonly int EmissionIntensity;
            public readonly bool ForceUnlitShading;

            public SpriteBatchKey(
                uint albedoTextureId,
                uint normalTextureId,
                uint emissionTextureId,
                int shadingModel,
                int normalMapMode,
                int emissionColorX,
                int emissionColorY,
                int emissionColorZ,
                int emissionIntensity,
                bool forceUnlitShading)
            {
                AlbedoTextureId = albedoTextureId;
                NormalTextureId = normalTextureId;
                EmissionTextureId = emissionTextureId;
                ShadingModel = shadingModel;
                NormalMapMode = normalMapMode;
                EmissionColorX = emissionColorX;
                EmissionColorY = emissionColorY;
                EmissionColorZ = emissionColorZ;
                EmissionIntensity = emissionIntensity;
                ForceUnlitShading = forceUnlitShading;
            }

            public bool Equals(SpriteBatchKey other)
            {
                return AlbedoTextureId == other.AlbedoTextureId
                       && NormalTextureId == other.NormalTextureId
                       && EmissionTextureId == other.EmissionTextureId
                       && ShadingModel == other.ShadingModel
                       && NormalMapMode == other.NormalMapMode
                       && EmissionColorX == other.EmissionColorX
                       && EmissionColorY == other.EmissionColorY
                       && EmissionColorZ == other.EmissionColorZ
                       && EmissionIntensity == other.EmissionIntensity
                       && ForceUnlitShading == other.ForceUnlitShading;
            }

            public override bool Equals(object? obj)
            {
                return obj is SpriteBatchKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(AlbedoTextureId);
                hash.Add(NormalTextureId);
                hash.Add(EmissionTextureId);
                hash.Add(ShadingModel);
                hash.Add(NormalMapMode);
                hash.Add(EmissionColorX);
                hash.Add(EmissionColorY);
                hash.Add(EmissionColorZ);
                hash.Add(EmissionIntensity);
                hash.Add(ForceUnlitShading);
                return hash.ToHashCode();
            }
        }

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

            if (_backgroundRectTextureId != 0)
            {
                _gl.DeleteTexture(_backgroundRectTextureId);
                _backgroundRectTextureId = 0;
                _backgroundRectTextureWidth = 0;
                _backgroundRectTextureHeight = 0;
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
            _boundViewportWidth = w;
            _boundViewportHeight = h;
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
            _renderContext.Time += Time.DeltaTime;
            _renderContext.FrameId = ++_frameId;
            _renderContext.ForceUnlitShading = false;

            BindTarget(_sceneTarget);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            var mainTarget = DrawLayer(scene.MainLayer, camera, true, _sceneTarget);

            var postProcessed = ApplyPostProcess(scene.PostProcess, mainTarget);
            var finalTarget = DrawLayer(scene.UiLayer, camera, false, postProcessed);

            BindDefaultTarget();
            DrawPresent(finalTarget.TextureId);

            DebugOverlay.Update(Time.DeltaTime);
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
            _gl.BlendFuncSeparate(
                BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha,
                BlendingFactor.One,
                BlendingFactor.OneMinusSrcAlpha
            );

            SDL_GL_GetDrawableSize(_sdlWindow, out _viewportWidth, out _viewportHeight);
            _gl.Viewport(0, 0, (uint) _viewportWidth, (uint) _viewportHeight);
            _boundFramebufferId = 0;
            _boundViewportWidth = _viewportWidth;
            _boundViewportHeight = _viewportHeight;
        }

        private void InitResources()
        {
            _fullscreenQuad = new GlFullscreenQuad(_gl);
            _renderContext = new GlRenderContext(_gl, _fullscreenQuad, new Scene("bootstrap"),
                new Camera(_graphicsParams.InitialWindowWidth, _graphicsParams.InitialWindowHeight));

            _presentShader = new GlShaderProgram(_gl, PresentVertexSource, PresentFragmentSource);
            _blendCompositeShader = new GlShaderProgram(_gl, BlendCompositeVertexSource, BlendCompositeFragmentSource);
            _blendCompositeSamplersInitialized = false;
            _defaultMaterial = SpriteMaterialFactory.Unlit();
            _outlineMaterial = new OutlineSpriteMaterial();

            _spriteVao = _gl.GenVertexArray();
            _spriteVbo = _gl.GenBuffer();

            _gl.BindVertexArray(_spriteVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _spriteVbo);
            _spriteBufferCapacityFloats = SpriteFloatCount * 256;
            unsafe
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (_spriteBufferCapacityFloats * sizeof(float)), null,
                    BufferUsageARB.DynamicDraw);
            }

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, SpriteVertexStrideFloats * sizeof(float),
                0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, SpriteVertexStrideFloats * sizeof(float),
                2 * sizeof(float));
            _gl.EnableVertexAttribArray(2);
            _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, SpriteVertexStrideFloats * sizeof(float),
                4 * sizeof(float));
            _gl.EnableVertexAttribArray(3);
            _gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, SpriteVertexStrideFloats * sizeof(float),
                6 * sizeof(float));

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);

            _neutralNormal = CreateNeutralNormal();
        }

        private void DrawPresent(uint textureId)
        {
            SetBlendDisabledState();
            _presentShader.Use();
            _presentShader.SetInt("u_texture", 0);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, textureId);
            _fullscreenQuad.Draw();
            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void BindTarget(GlRenderTarget target)
        {
            if (_boundFramebufferId != target.FramebufferId)
            {
                _gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.FramebufferId);
                _boundFramebufferId = target.FramebufferId;
            }

            if (_boundViewportWidth != target.Width || _boundViewportHeight != target.Height)
            {
                _gl.Viewport(0, 0, (uint) target.Width, (uint) target.Height);
                _boundViewportWidth = target.Width;
                _boundViewportHeight = target.Height;
            }
        }

        private void BindDefaultTarget()
        {
            if (_boundFramebufferId != 0)
            {
                _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                _boundFramebufferId = 0;
            }

            if (_boundViewportWidth != _viewportWidth || _boundViewportHeight != _viewportHeight)
            {
                _gl.Viewport(0, 0, (uint) _viewportWidth, (uint) _viewportHeight);
                _boundViewportWidth = _viewportWidth;
                _boundViewportHeight = _viewportHeight;
            }
        }

        private void SetAlphaBlendState()
        {
            if (_blendState == BlendStateMode.AlphaBlend)
            {
                return;
            }

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFuncSeparate(
                BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha,
                BlendingFactor.One,
                BlendingFactor.OneMinusSrcAlpha
            );
            _blendState = BlendStateMode.AlphaBlend;
        }

        private void SetBlendDisabledState()
        {
            if (_blendState == BlendStateMode.Disabled)
            {
                return;
            }

            _gl.Disable(EnableCap.Blend);
            _blendState = BlendStateMode.Disabled;
        }

        private GlRenderTarget SelectPostProcessOutputTarget(GlRenderTarget input)
        {
            if (TargetsEqual(input, _pingTarget))
            {
                return _pongTarget;
            }

            return _pingTarget;
        }

        private GlRenderTarget SelectForegroundTarget(GlRenderTarget current)
        {
            if (!TargetsEqual(current, _sceneTarget))
            {
                return _sceneTarget;
            }

            return _pingTarget;
        }

        private void CompositeTargetsToCurrentTarget(ScreenRect rect, GlRenderTarget foreground, GlRenderTarget target,
            SpriteBlendMode blendMode)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            EnsureBackgroundRectTexture(rect.Width, rect.Height);
            CaptureBackgroundRect(target, rect);

            BuildCompositeRectVertices(_singleSpriteVertices, rect.X, rect.Y, rect.Width, rect.Height,
                2f / target.Width,
                -1f,
                -2f / target.Height,
                1f);

            BindTarget(target);
            SetBlendDisabledState();
            EnableScissor(rect, target.Height);

            _blendCompositeShader.Use();
            if (!_blendCompositeSamplersInitialized)
            {
                _blendCompositeShader.SetInt("u_background", 0);
                _blendCompositeShader.SetInt("u_foreground", 1);
                _blendCompositeSamplersInitialized = true;
            }

            _blendCompositeShader.SetInt("u_mode", (int) blendMode);
            _blendCompositeShader.SetVector2("u_backgroundUvScale",
                rect.Width / (float) _backgroundRectTextureWidth,
                rect.Height / (float) _backgroundRectTextureHeight);
            _blendCompositeShader.SetVector2("u_foregroundUvMin",
                rect.X / (float) target.Width,
                1f - (rect.Y + rect.Height) / (float) target.Height);
            _blendCompositeShader.SetVector2("u_foregroundUvScale",
                rect.Width / (float) target.Width,
                rect.Height / (float) target.Height);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _backgroundRectTextureId);
            _gl.ActiveTexture(TextureUnit.Texture1);
            _gl.BindTexture(TextureTarget.Texture2D, foreground.TextureId);

            DrawRawSpriteVertices(_singleSpriteVertices, SpriteVertexCount);
            FrameStats.AddDrawCall();
            DisableScissor();
        }

        private static bool TargetsEqual(GlRenderTarget a, GlRenderTarget b)
        {
            return a.FramebufferId == b.FramebufferId;
        }

        private GlRenderTarget ApplyPostProcess(PostProcessStack stack, GlRenderTarget inputTarget)
        {
            if (!stack.Enabled || stack.Effects.Count == 0)
            {
                return inputTarget;
            }

            var input = inputTarget;
            var applied = false;

            foreach (var effect in stack.Effects)
            {
                if (effect is not IGlPostProcessEffect glEffect || !effect.Enabled)
                {
                    continue;
                }

                var output = SelectPostProcessOutputTarget(input);
                glEffect.Apply(_renderContext, input, output);
                input = output;
                applied = true;
            }

            return applied ? input : inputTarget;
        }

        private void DrawSprites(Scene scene, Camera camera)
        {
            var mainTarget = DrawLayer(scene.MainLayer, camera, true, _sceneTarget);
            DrawLayer(scene.UiLayer, camera, false, mainTarget);
        }

        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        private GlRenderTarget DrawLayer(SceneLayer layer, Camera camera, bool isCameraLayer, GlRenderTarget initialTarget)
        {
            var cameraRect = camera.Rect;
            var cameraOffsetX = cameraRect.X + camera.SubpixelOffset.X;
            var cameraOffsetY = cameraRect.Y + camera.SubpixelOffset.Y;
            var target = initialTarget;
            _renderContext.ForceUnlitShading = !isCameraLayer;

            var ndcScaleX = 2f / _viewportWidth;
            var ndcBiasX = -1f;
            var ndcScaleY = -2f / _viewportHeight;
            var ndcBiasY = 1f;

            var hasPendingBatch = false;
            var pendingBatchKey = default(SpriteBatchKey);
            var pendingBatchMaterial = default(IGlMaterial)!;
            var pendingBatchSprite = default(Sprite)!;
            var pendingBatchAlbedo = default(GlNativeTexture)!;
            GlNativeTexture? pendingBatchNormal = null;
            GlNativeTexture? pendingBatchEmission = null;
            var pendingBatchSpriteCount = 0;
            var pendingBatchFloatCount = 0;

            void FlushPendingBatch()
            {
                if (!hasPendingBatch)
                {
                    return;
                }

                BindTarget(target);
                SetAlphaBlendState();
                DrawSprite(
                    _batchedVertices.AsSpan(0, pendingBatchFloatCount),
                    pendingBatchFloatCount / SpriteVertexStrideFloats,
                    pendingBatchSpriteCount,
                    pendingBatchMaterial,
                    pendingBatchSprite,
                    pendingBatchAlbedo,
                    pendingBatchNormal,
                    pendingBatchEmission);

                hasPendingBatch = false;
                pendingBatchSpriteCount = 0;
                pendingBatchFloatCount = 0;
            }

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

                var canvasSpritePosX = spritePosX;
                var canvasSpritePosY = spritePosY;
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

                BuildQuadVertices(
                    _singleSpriteVertices,
                    canvasSpritePosX, canvasSpritePosY,
                    screenSpritePosX, screenSpritePosY,
                    sprite.Width, sprite.Height,
                    sprite.Transformation.Rotation.Angle,
                    sprite.Transformation.Rotation.Clockwise,
                    sprite.Color.X,
                    sprite.Color.Y,
                    sprite.Color.Z,
                    sprite.Color.W,
                    ndcScaleX,
                    ndcBiasX,
                    ndcScaleY,
                    ndcBiasY
                );
                var outline = sprite.Outline;
                var outlineEnabled = outline.Enabled && outline.ThicknessTexels > 0f && outline.Color.W > 0f;
                var outlineBlendMode = outline.ForceAlphaBlend ? SpriteBlendMode.Normal : sprite.BlendMode;

                if (outlineEnabled && outline.Layering == OutlineLayering.Under)
                {
                    FlushPendingBatch();
                    DrawOutline(sprite, albedo, canvasSpritePosX, canvasSpritePosY, screenSpritePosX, screenSpritePosY,
                        outlineBlendMode, ref target, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
                }

                var material = ResolveMaterial(sprite);
                if (material == null)
                {
                    continue;
                }

                var capabilities = ResolveMaterialCapabilities(material);
                var normalMap = ResolveNormalMap(sprite, capabilities);
                var emissionMap = ResolveEmissionMap(capabilities);

                if (sprite.BlendMode == SpriteBlendMode.Normal
                    && TryBuildBatchKey(material, albedo, normalMap, emissionMap, out var batchKey))
                {
                    if (hasPendingBatch && !pendingBatchKey.Equals(batchKey))
                    {
                        FlushPendingBatch();
                    }

                    if (!hasPendingBatch)
                    {
                        pendingBatchKey = batchKey;
                        pendingBatchMaterial = material;
                        pendingBatchSprite = sprite;
                        pendingBatchAlbedo = albedo;
                        pendingBatchNormal = normalMap;
                        pendingBatchEmission = emissionMap;
                        hasPendingBatch = true;
                    }

                    EnsureBatchVertexCapacity(pendingBatchFloatCount + SpriteFloatCount);
                    Array.Copy(_singleSpriteVertices, 0, _batchedVertices, pendingBatchFloatCount, SpriteFloatCount);
                    pendingBatchFloatCount += SpriteFloatCount;
                    pendingBatchSpriteCount++;
                }
                else
                {
                    FlushPendingBatch();
                    DrawSpriteWithBlendMode(
                        _singleSpriteVertices,
                        SpriteVertexCount,
                        1,
                        material,
                        sprite,
                        albedo,
                        normalMap,
                        emissionMap,
                        sprite.BlendMode,
                        ref target);
                }

                if (outlineEnabled && outline.Layering == OutlineLayering.Over)
                {
                    FlushPendingBatch();
                    DrawOutline(sprite, albedo, canvasSpritePosX, canvasSpritePosY, screenSpritePosX, screenSpritePosY,
                        outlineBlendMode, ref target, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
                }
            }

            FlushPendingBatch();

            return target;
        }

        private bool TryBuildBatchKey(IGlMaterial material, GlNativeTexture albedo, GlNativeTexture? normalMap,
            GlNativeTexture? emissionMap, out SpriteBatchKey key)
        {
            if (material is not StandardSpriteMaterial standardMaterial)
            {
                key = default;
                return false;
            }

            key = new SpriteBatchKey(
                albedo.TextureId,
                normalMap?.TextureId ?? 0u,
                emissionMap?.TextureId ?? 0u,
                (int) standardMaterial.ShadingModel,
                (int) standardMaterial.NormalMapMode,
                BitConverter.SingleToInt32Bits(standardMaterial.EmissionColor.X),
                BitConverter.SingleToInt32Bits(standardMaterial.EmissionColor.Y),
                BitConverter.SingleToInt32Bits(standardMaterial.EmissionColor.Z),
                BitConverter.SingleToInt32Bits(standardMaterial.EmissionIntensity),
                _renderContext.ForceUnlitShading);
            return true;
        }

        private void EnsureBatchVertexCapacity(int requiredFloats)
        {
            if (_batchedVertices.Length >= requiredFloats)
            {
                return;
            }

            var newCapacity = _batchedVertices.Length;
            while (newCapacity < requiredFloats)
            {
                newCapacity *= 2;
            }

            Array.Resize(ref _batchedVertices, newCapacity);
        }

        private void DrawSpriteWithBlendMode(ReadOnlySpan<float> vertices, int vertexCount, int spriteCount,
            IGlMaterial material, Sprite sprite, GlNativeTexture albedo,
            GlNativeTexture? normalMap, GlNativeTexture? emissionMap, SpriteBlendMode blendMode, ref GlRenderTarget target)
        {
            if (blendMode == SpriteBlendMode.Normal)
            {
                BindTarget(target);
                SetAlphaBlendState();
                DrawSprite(vertices, vertexCount, spriteCount, material, sprite, albedo, normalMap, emissionMap);
                return;
            }

            if (!TryGetScreenRect(vertices, vertexCount, target.Width, target.Height, out var rect))
            {
                return;
            }

            var foregroundTarget = SelectForegroundTarget(target);
            BindTarget(foregroundTarget);
            EnableScissor(rect, foregroundTarget.Height);
            _gl.ClearColor(0f, 0f, 0f, 0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            SetBlendDisabledState();
            DrawSprite(vertices, vertexCount, spriteCount, material, sprite, albedo, normalMap, emissionMap);
            DisableScissor();
            _gl.ClearColor(0f, 0f, 0f, 1f);

            CompositeTargetsToCurrentTarget(rect, foregroundTarget, target, blendMode);
        }

        private void DrawSprite(ReadOnlySpan<float> vertices, int vertexCount, int spriteCount,
            IGlMaterial material, Sprite sprite, GlNativeTexture albedo,
            GlNativeTexture? normalMap, GlNativeTexture? emissionMap)
        {
            material.Bind(_renderContext, sprite, albedo, normalMap, emissionMap);

            DrawRawSpriteVertices(vertices, vertexCount);
            FrameStats.AddDrawCall();
            for (var i = 0; i < spriteCount; i++)
            {
                FrameStats.AddSpriteDrawn();
            }
        }

        private void DrawRawSpriteVertices(ReadOnlySpan<float> vertices, int vertexCount)
        {
            UpdateSpriteBuffer(vertices);
            _gl.BindVertexArray(_spriteVao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) vertexCount);
        }

        private static bool TryGetScreenRect(ReadOnlySpan<float> vertices, int vertexCount, int targetWidth,
            int targetHeight, out ScreenRect rect)
        {
            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;

            for (var i = 0; i < vertexCount; i++)
            {
                var offset = i * SpriteVertexStrideFloats;
                var ndcX = vertices[offset];
                var ndcY = vertices[offset + 1];

                var screenX = (ndcX * 0.5f + 0.5f) * targetWidth;
                var screenY = (0.5f - ndcY * 0.5f) * targetHeight;

                if (screenX < minX)
                {
                    minX = screenX;
                }

                if (screenY < minY)
                {
                    minY = screenY;
                }

                if (screenX > maxX)
                {
                    maxX = screenX;
                }

                if (screenY > maxY)
                {
                    maxY = screenY;
                }
            }

            var x0 = Math.Clamp((int) MathF.Floor(minX), 0, targetWidth);
            var y0 = Math.Clamp((int) MathF.Floor(minY), 0, targetHeight);
            var x1 = Math.Clamp((int) MathF.Ceiling(maxX), 0, targetWidth);
            var y1 = Math.Clamp((int) MathF.Ceiling(maxY), 0, targetHeight);

            var width = x1 - x0;
            var height = y1 - y0;
            if (width <= 0 || height <= 0)
            {
                rect = default;
                return false;
            }

            rect = new ScreenRect(x0, y0, width, height);
            return true;
        }

        private void EnsureBackgroundRectTexture(int width, int height)
        {
            if (_backgroundRectTextureId != 0 && _backgroundRectTextureWidth >= width && _backgroundRectTextureHeight >= height)
            {
                return;
            }

            if (_backgroundRectTextureId != 0)
            {
                _gl.DeleteTexture(_backgroundRectTextureId);
            }

            _backgroundRectTextureId = CreateHdrTexture(width, height);
            _backgroundRectTextureWidth = width;
            _backgroundRectTextureHeight = height;
        }

        private void CaptureBackgroundRect(GlRenderTarget target, ScreenRect rect)
        {
            BindTarget(target);
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            _gl.BindTexture(TextureTarget.Texture2D, _backgroundRectTextureId);

            var glY = target.Height - rect.Y - rect.Height;
            _gl.CopyTexSubImage2D(GLEnum.Texture2D, 0, 0, 0, rect.X, glY, (uint) rect.Width,
                (uint) rect.Height);
        }

        private void EnableScissor(ScreenRect rect, int targetHeight)
        {
            _gl.Enable(EnableCap.ScissorTest);
            var glY = targetHeight - rect.Y - rect.Height;
            _gl.Scissor(rect.X, glY, (uint) rect.Width, (uint) rect.Height);
        }

        private void DisableScissor()
        {
            _gl.Disable(EnableCap.ScissorTest);
        }

        private void DrawOutline(Sprite sprite, GlNativeTexture albedo,
            float worldSpritePosX, float worldSpritePosY,
            float screenSpritePosX, float screenSpritePosY,
            SpriteBlendMode blendMode,
            ref GlRenderTarget target,
            float ndcScaleX,
            float ndcBiasX,
            float ndcScaleY,
            float ndcBiasY)
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

            BuildQuadVertices(
                _singleSpriteVertices,
                outlineWorldX, outlineWorldY,
                outlineScreenX, outlineScreenY,
                outlineWidth, outlineHeight,
                sprite.Transformation.Rotation.Angle,
                sprite.Transformation.Rotation.Clockwise,
                -uvPadX, -uvPadY,
                1f + uvPadX, 1f + uvPadY,
                sprite.Color.X,
                sprite.Color.Y,
                sprite.Color.Z,
                sprite.Color.W,
                ndcScaleX,
                ndcBiasX,
                ndcScaleY,
                ndcBiasY
            );

            DrawSpriteWithBlendMode(_singleSpriteVertices, SpriteVertexCount, 1, _outlineMaterial, sprite, albedo,
                null, null, blendMode,
                ref target);
        }

        private IGlMaterial? ResolveMaterial(Sprite sprite)
        {
            if (sprite.Material is IGlMaterial glMaterial && glMaterial.Enabled)
            {
                return glMaterial;
            }

            return _defaultMaterial;
        }

        private static ISpriteMaterialCapabilities ResolveMaterialCapabilities(IGlMaterial material)
        {
            if (material is ISpriteMaterialCapabilities capabilities)
            {
                return capabilities;
            }

            return DefaultMaterialCapabilities.Instance;
        }

        private GlNativeTexture? ResolveNormalMap(Sprite sprite, ISpriteMaterialCapabilities capabilities)
        {
            if (capabilities.ShadingModel != SpriteShadingModel.Lit || _renderContext.ForceUnlitShading)
            {
                return null;
            }

            switch (capabilities.NormalMapMode)
            {
                case SpriteNormalMapMode.None:
                    return null;

                case SpriteNormalMapMode.UseMaterial:
                {
                    if (capabilities.NormalMapTexture != null)
                    {
                        return TextureHolder.GetInstance().GetNativeTexture(capabilities.NormalMapTexture) as GlNativeTexture;
                    }

                    return _neutralNormal;
                }

                case SpriteNormalMapMode.Neutral:
                    return _neutralNormal;
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

        private static GlNativeTexture? ResolveEmissionMap(ISpriteMaterialCapabilities capabilities)
        {
            if (capabilities.EmissionIntensity <= 0f || capabilities.EmissionMapTexture == null)
            {
                return null;
            }

            return TextureHolder.GetInstance().GetNativeTexture(capabilities.EmissionMapTexture) as GlNativeTexture;
        }

        private sealed class DefaultMaterialCapabilities : ISpriteMaterialCapabilities
        {
            public static readonly DefaultMaterialCapabilities Instance = new();

            public bool Enabled
            {
                get => true;
                set
                {
                }
            }

            public SpriteShadingModel ShadingModel => SpriteShadingModel.Unlit;
            public SpriteNormalMapMode NormalMapMode => SpriteNormalMapMode.None;
            public EngineTexture? NormalMapTexture => null;
            public EngineTexture? EmissionMapTexture => null;
            public System.Numerics.Vector3 EmissionColor => System.Numerics.Vector3.One;
            public float EmissionIntensity => 0f;
        }

        private static void BuildQuadVertices(
            Span<float> vertices,
            float worldX, float worldY,
            float screenX, float screenY,
            int width, int height,
            double angle,
            bool clockwise,
            float tintR,
            float tintG,
            float tintB,
            float tintA,
            float ndcScaleX,
            float ndcBiasX,
            float ndcScaleY,
            float ndcBiasY)
        {
            BuildQuadVertices(vertices, worldX, worldY, screenX, screenY, width, height, angle, clockwise, 0f, 0f,
                1f, 1f, tintR, tintG, tintB, tintA, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
        }

        private static void BuildQuadVertices(
            Span<float> vertices,
            float worldX,
            float worldY,
            float screenX,
            float screenY,
            int width,
            int height,
            double angle,
            bool clockwise,
            float uvMinX,
            float uvMinY,
            float uvMaxX,
            float uvMaxY,
            float tintR,
            float tintG,
            float tintB,
            float tintA,
            float ndcScaleX,
            float ndcBiasX,
            float ndcScaleY,
            float ndcBiasY)
        {
            float worldTlX;
            float worldTlY;
            float worldTrX;
            float worldTrY;
            float worldBrX;
            float worldBrY;
            float worldBlX;
            float worldBlY;

            float screenTlX;
            float screenTlY;
            float screenTrX;
            float screenTrY;
            float screenBrX;
            float screenBrY;
            float screenBlX;
            float screenBlY;

            if (Math.Abs(angle) < double.Epsilon)
            {
                worldTlX = worldX;
                worldTlY = worldY;
                worldTrX = worldX + width;
                worldTrY = worldY;
                worldBrX = worldX + width;
                worldBrY = worldY + height;
                worldBlX = worldX;
                worldBlY = worldY + height;

                screenTlX = screenX;
                screenTlY = screenY;
                screenTrX = screenX + width;
                screenTrY = screenY;
                screenBrX = screenX + width;
                screenBrY = screenY + height;
                screenBlX = screenX;
                screenBlY = screenY + height;
            }
            else
            {
                var rotationDeg = MathHelper.NorRotationToDegree(clockwise ? angle : -angle);
                var rotationRad = (float) (rotationDeg * Math.PI / 180f);
                var cos = MathF.Cos(rotationRad);
                var sin = MathF.Sin(rotationRad);

                var worldX0 = worldX;
                var worldY0 = worldY;
                var worldX1 = worldX + width;
                var worldY1 = worldY + height;
                var worldCenterX = worldX + width / 2f;
                var worldCenterY = worldY + height / 2f;

                RotatePoint(worldX0, worldY0, worldCenterX, worldCenterY, cos, sin, out worldTlX, out worldTlY);
                RotatePoint(worldX1, worldY0, worldCenterX, worldCenterY, cos, sin, out worldTrX, out worldTrY);
                RotatePoint(worldX1, worldY1, worldCenterX, worldCenterY, cos, sin, out worldBrX, out worldBrY);
                RotatePoint(worldX0, worldY1, worldCenterX, worldCenterY, cos, sin, out worldBlX, out worldBlY);

                var screenX0 = screenX;
                var screenY0 = screenY;
                var screenX1 = screenX + width;
                var screenY1 = screenY + height;
                var screenCenterX = screenX + width / 2f;
                var screenCenterY = screenY + height / 2f;

                RotatePoint(screenX0, screenY0, screenCenterX, screenCenterY, cos, sin, out screenTlX,
                    out screenTlY);
                RotatePoint(screenX1, screenY0, screenCenterX, screenCenterY, cos, sin, out screenTrX,
                    out screenTrY);
                RotatePoint(screenX1, screenY1, screenCenterX, screenCenterY, cos, sin, out screenBrX,
                    out screenBrY);
                RotatePoint(screenX0, screenY1, screenCenterX, screenCenterY, cos, sin, out screenBlX,
                    out screenBlY);
            }

            WriteSpriteVertex(vertices, 0, screenTlX, screenTlY, uvMinX, uvMinY, worldTlX, worldTlY, tintR, tintG,
                tintB, tintA, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
            WriteSpriteVertex(vertices, 1, screenTrX, screenTrY, uvMaxX, uvMinY, worldTrX, worldTrY, tintR, tintG,
                tintB, tintA, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
            WriteSpriteVertex(vertices, 2, screenBrX, screenBrY, uvMaxX, uvMaxY, worldBrX, worldBrY, tintR, tintG,
                tintB, tintA, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
            WriteSpriteVertex(vertices, 3, screenTlX, screenTlY, uvMinX, uvMinY, worldTlX, worldTlY, tintR, tintG,
                tintB, tintA, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
            WriteSpriteVertex(vertices, 4, screenBrX, screenBrY, uvMaxX, uvMaxY, worldBrX, worldBrY, tintR, tintG,
                tintB, tintA, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
            WriteSpriteVertex(vertices, 5, screenBlX, screenBlY, uvMinX, uvMaxY, worldBlX, worldBlY, tintR, tintG,
                tintB, tintA, ndcScaleX, ndcBiasX, ndcScaleY, ndcBiasY);
        }

        private static void RotatePoint(float x, float y, float cx, float cy, float cos, float sin, out float rx,
            out float ry)
        {
            var dx = x - cx;
            var dy = y - cy;
            rx = dx * cos - dy * sin + cx;
            ry = dx * sin + dy * cos + cy;
        }

        private static void BuildCompositeRectVertices(Span<float> vertices, int x, int y, int width, int height,
            float ndcScaleX, float ndcBiasX, float ndcScaleY, float ndcBiasY)
        {
            var tlX = x;
            var tlY = y;
            var trX = x + width;
            var trY = y;
            var brX = x + width;
            var brY = y + height;
            var blX = x;
            var blY = y + height;

            WriteSpriteVertex(vertices, 0, tlX, tlY, 0f, 1f, 0f, 0f, 1f, 1f, 1f, 1f, ndcScaleX, ndcBiasX, ndcScaleY,
                ndcBiasY);
            WriteSpriteVertex(vertices, 1, trX, trY, 1f, 1f, 0f, 0f, 1f, 1f, 1f, 1f, ndcScaleX, ndcBiasX, ndcScaleY,
                ndcBiasY);
            WriteSpriteVertex(vertices, 2, brX, brY, 1f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, ndcScaleX, ndcBiasX, ndcScaleY,
                ndcBiasY);
            WriteSpriteVertex(vertices, 3, tlX, tlY, 0f, 1f, 0f, 0f, 1f, 1f, 1f, 1f, ndcScaleX, ndcBiasX, ndcScaleY,
                ndcBiasY);
            WriteSpriteVertex(vertices, 4, brX, brY, 1f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, ndcScaleX, ndcBiasX, ndcScaleY,
                ndcBiasY);
            WriteSpriteVertex(vertices, 5, blX, blY, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, ndcScaleX, ndcBiasX, ndcScaleY,
                ndcBiasY);
        }

        private static void WriteSpriteVertex(Span<float> vertices, int index, float screenX, float screenY, float uvX,
            float uvY, float worldX, float worldY, float tintR, float tintG, float tintB, float tintA,
            float ndcScaleX, float ndcBiasX, float ndcScaleY, float ndcBiasY)
        {
            var offset = index * SpriteVertexStrideFloats;
            vertices[offset + 0] = screenX * ndcScaleX + ndcBiasX;
            vertices[offset + 1] = screenY * ndcScaleY + ndcBiasY;
            vertices[offset + 2] = uvX;
            vertices[offset + 3] = uvY;
            vertices[offset + 4] = worldX;
            vertices[offset + 5] = worldY;
            vertices[offset + 6] = tintR;
            vertices[offset + 7] = tintG;
            vertices[offset + 8] = tintB;
            vertices[offset + 9] = tintA;
        }

        private void UpdateSpriteBuffer(ReadOnlySpan<float> vertices)
        {
            EnsureSpriteBufferCapacity(vertices.Length);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _spriteVbo);
            unsafe
            {
                fixed (float* data = vertices)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint) (vertices.Length * sizeof(float)),
                        data);
                }
            }
        }

        private void EnsureSpriteBufferCapacity(int requiredFloats)
        {
            if (_spriteBufferCapacityFloats >= requiredFloats)
            {
                return;
            }

            var newCapacity = _spriteBufferCapacityFloats;
            while (newCapacity < requiredFloats)
            {
                newCapacity *= 2;
            }

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _spriteVbo);
            unsafe
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (newCapacity * sizeof(float)), null,
                    BufferUsageARB.DynamicDraw);
            }

            _spriteBufferCapacityFloats = newCapacity;
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
            _boundFramebufferId = framebufferId;
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, textureId, 0);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException($"Framebuffer incomplete: {status}");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _boundFramebufferId = 0;
            return new GlRenderTarget(framebufferId, textureId, width, height);
        }

        private GlRenderTarget CreateHdrRenderTarget(int width, int height)
        {
            var textureId = CreateHdrTexture(width, height);
            var framebufferId = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);
            _boundFramebufferId = framebufferId;
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, textureId, 0);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException($"Framebuffer incomplete: {status}");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _boundFramebufferId = 0;
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

            SetAlphaBlendState();

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

                var sprite = new Sprite(width, height)
                {
                    Color = Vector4.One,
                };

                BuildQuadVertices(
                    _singleSpriteVertices,
                    x,
                    y,
                    x,
                    y,
                    width,
                    height,
                    0d,
                    true,
                    sprite.Color.X,
                    sprite.Color.Y,
                    sprite.Color.Z,
                    sprite.Color.W,
                    2f / _viewportWidth,
                    -1f,
                    -2f / _viewportHeight,
                    1f);
                DrawSprite(_singleSpriteVertices, SpriteVertexCount, 1, _defaultMaterial, sprite, texture, null, null);
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

        private const string BlendCompositeVertexSource = @"#version 330 core
layout(location = 0) in vec2 a_pos;
layout(location = 1) in vec2 a_uv;

out vec2 v_uv;

void main()
{
    v_uv = a_uv;
    gl_Position = vec4(a_pos.xy, 0.0, 1.0);
}
";

        private const string BlendCompositeFragmentSource = @"#version 330 core
in vec2 v_uv;
out vec4 FragColor;

uniform sampler2D u_background;
uniform sampler2D u_foreground;
uniform int u_mode;
uniform vec2 u_backgroundUvScale;
uniform vec2 u_foregroundUvMin;
uniform vec2 u_foregroundUvScale;

float BlendSoftLightChannel(float d, float s)
{
    if (s <= 0.5)
    {
        return d - (1.0 - 2.0 * s) * d * (1.0 - d);
    }

    float g;
    if (d <= 0.25)
    {
        g = ((16.0 * d - 12.0) * d + 4.0) * d;
    }
    else
    {
        g = sqrt(max(d, 0.0));
    }

    return d + (2.0 * s - 1.0) * (g - d);
}

vec3 BlendRgb(vec3 dst, vec3 src)
{
    if (u_mode == 1)
    {
        return dst * src;
    }

    if (u_mode == 2)
    {
        return 1.0 - (1.0 - src) * (1.0 - dst);
    }

    if (u_mode == 3)
    {
        return vec3(
            BlendSoftLightChannel(dst.r, src.r),
            BlendSoftLightChannel(dst.g, src.g),
            BlendSoftLightChannel(dst.b, src.b)
        );
    }

    if (u_mode == 4)
    {
        return vec3(
            dst.r <= 0.5 ? (2.0 * dst.r * src.r) : (1.0 - 2.0 * (1.0 - dst.r) * (1.0 - src.r)),
            dst.g <= 0.5 ? (2.0 * dst.g * src.g) : (1.0 - 2.0 * (1.0 - dst.g) * (1.0 - src.g)),
            dst.b <= 0.5 ? (2.0 * dst.b * src.b) : (1.0 - 2.0 * (1.0 - dst.b) * (1.0 - src.b))
        );
    }

    return src;
}

void main()
{
    vec2 bgUv = v_uv * u_backgroundUvScale;
    vec2 fgUv = u_foregroundUvMin + v_uv * u_foregroundUvScale;

    vec4 background = texture(u_background, bgUv);
    vec4 foreground = texture(u_foreground, fgUv);

    float dstA = clamp(background.a, 0.0, 1.0);
    float srcA = clamp(foreground.a, 0.0, 1.0);
    vec3 blended = BlendRgb(background.rgb, foreground.rgb);
    vec3 outRgb = (1.0 - srcA) * background.rgb + srcA * ((1.0 - dstA) * foreground.rgb + dstA * blended);
    float outA = srcA + dstA - srcA * dstA;

    FragColor = vec4(outRgb, outA);
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
