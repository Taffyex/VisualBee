using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace MusicBeePlugin
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VisualBeeConstants
    {
        public Vector4 Viewport;        // x: width, y: height, z: 1/width, w: 1/height
        public Vector4 AudioMetricsA;   // x: lowPulse, y: bass, z: mid, w: high
        public Vector4 AudioMetricsB;   // x: beat, y: rms, z: centroid, w: transient
        public Vector4 VisualParams;    // x: time (s), y: barStyle, z: bloomIntensity, w: visualizerMode
        public Vector4 PaletteColorA;   // Primary / Accent (linear)
        public Vector4 PaletteColorB;   // Secondary / Warm Glow (linear)
        public Vector4 PaletteColorC;   // Highlight / Peak (linear)
        public Vector4 PaletteColorD;   // Accent Tip / Highlight (linear)
        public Vector4 PaletteBg;       // True Dark Background (linear)
        public Vector4 BarParams;       // x: barCount, y: barGap, z: mirrorFlags, w: peakHoldEnabled
        public Vector4 RibbonParams;    // x: thicknessPx, y: fillToFloor, z: floorReflection, w: dpiScale
        public Vector4 StyleExtra;      // extra
        public Vector4 BarShape;        // x: roundness 0..1, y: segHeightPx, z: segGapPx, w: trebleTaper 0..0.8
        public Vector4 ColorParams;     // x: colorMode 0..3, y: hueSpeed, z: bgBrightness, w: vignette 0..1
        public Vector4 PeakParams;      // x: capThicknessPx, y: capColorMode 0..2, z: reflection 0..1, w: contactGlow 0/1
        public Vector4 PeakColor;       // linear custom cap colour (rgb)
        public Vector4 StudioParams;    // x: decibelGrid, y: incandescence, z: glassOpacity, w: horizonGlow
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BlurConstants
    {
        public Vector4 BlurDir;
    }

    internal sealed partial class D3DRenderer : IDisposable
    {
        private Device _device;
        private DeviceContext _context;
        private SwapChain _swapChain;
        private RenderTargetView _backBufferRtv;
        private readonly FramePacer _pacer = new FramePacer();
        private readonly IntPtr _hwnd;
        private int _width;
        private int _height;
        private int _frameCap;
        private SwapChainFlags _swapChainFlags = SwapChainFlags.None;
        private bool _tearingSupported;
        private bool _disposed;

        private Texture2D _sceneTexture;
        private RenderTargetView _sceneRtv;
        private ShaderResourceView _sceneSrv;

        private Texture2D _bloomTextureA;
        private RenderTargetView _bloomRtvA;
        private ShaderResourceView _bloomSrvA;
        private Texture2D _bloomTextureB;
        private RenderTargetView _bloomRtvB;
        private ShaderResourceView _bloomSrvB;
        private int _bloomWidth;
        private int _bloomHeight;

        private Buffer _constantBuffer;
        private VisualBeeConstants _constants;
        private Buffer _cbBlurHorizontal;
        private Buffer _cbBlurVertical;

        private Texture1D _waveTexture;
        private ShaderResourceView _waveSrv;
        private Texture1D _bandTexture;
        private ShaderResourceView _bandSrv;

        private VertexShader _vsFullscreen;
        private PixelShader _psBars;
        private PixelShader _psRibbon;

        private PixelShader _psBloomBright;
        private PixelShader _psBloomBlur;
        private PixelShader _psComposite;

        private SamplerState _linearSampler;
        private SamplerState _pointSampler;
        private RasterizerState _rasterState;
        private DepthStencilState _depthDisabled;

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double _fps = 60.0;
        private double _frameMs = 16.6;
        private long _lastFrameTicks;
        private double _maxFrameMs;
        private long _slowFrames;

        public int Width => _width;
        public int Height => _height;
        public double Fps => _fps;
        public double FrameMs => _frameMs;
        public double MaxFrameMs => _maxFrameMs;
        public long SlowFrames => _slowFrames;
        public string PresentMode => _pacer.PresentMode;
        public bool TearingSupported => _tearingSupported;
        public FramePacer Pacer => _pacer;
        public bool IsReady => _device != null && !_disposed && _swapChain != null;
        public bool IsDisposed => _disposed;
        public bool NeedsRebuildRetry => _swapChain == null && !_disposed && DateTime.UtcNow >= _rebuildRetryAt;
        public int RebuildAttempts => _rebuildAttempts;

        private DateTime _rebuildRetryAt = DateTime.MinValue;
        private int _rebuildAttempts = 0;

        public D3DRenderer(IntPtr hwnd, int width, int height, int frameCap, string shaderCacheFolder = null)
        {
            if (hwnd == IntPtr.Zero) throw new ArgumentException("hwnd");
            _hwnd = hwnd;
            _width = Math.Max(4, width);
            _height = Math.Max(4, height);
            _frameCap = frameCap;

            try
            {
                _device = new Device(SharpDX.Direct3D.DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport, SharpDX.Direct3D.FeatureLevel.Level_11_0);
            }
            catch
            {
                _device = new Device(SharpDX.Direct3D.DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport, SharpDX.Direct3D.FeatureLevel.Level_10_1, SharpDX.Direct3D.FeatureLevel.Level_10_0);
            }
            _context = _device.ImmediateContext;

            CreateSwapChain(hwnd, frameCap);
            CreatePipeline(shaderCacheFolder);
            CreateTargets();
        }

        private PaletteColors _currentPalette;
        public PaletteColors CurrentPalette => _currentPalette;

        public void SetPalette(PaletteColors p)
        {
            _currentPalette = p;
            _constants.PaletteColorA = ToLinearVec4(p.ColorA);
            _constants.PaletteColorB = ToLinearVec4(p.ColorB);
            _constants.PaletteColorC = ToLinearVec4(p.ColorC);
            _constants.PaletteColorD = ToLinearVec4(p.ColorD);
            _constants.PaletteBg = ToLinearVec4(p.Background);
        }

        public static float SrgbToLinear(float c)
        {
            return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4);
        }

        public static Vector4 ToLinearVec4(System.Drawing.Color c)
        {
            return new Vector4(SrgbToLinear(c.R / 255f), SrgbToLinear(c.G / 255f), SrgbToLinear(c.B / 255f), c.A / 255f);
        }

        public void RebuildSwapChain(int frameCap)
        {
            if (_disposed || _hwnd == IntPtr.Zero) return;
            _frameCap = frameCap;

            _context.OutputMerger.SetRenderTargets((DepthStencilView)null, (RenderTargetView)null);
            _context.ClearState();
            _context.Flush();
            _pacer.DetachSwapChain();
            _backBufferRtv?.Dispose();
            _backBufferRtv = null;
            _swapChain?.Dispose();
            _swapChain = null;

            try
            {
                CreateSwapChain(_hwnd, frameCap);
                _rebuildAttempts = 0;
                _rebuildRetryAt = DateTime.MinValue;
            }
            catch
            {
                _rebuildAttempts++;
                int delayMs = Math.Min(2000, 250 * _rebuildAttempts);
                _rebuildRetryAt = DateTime.UtcNow.AddMilliseconds(delayMs);
            }
        }

        public void Resize(int width, int height)
        {
            if (_disposed) return;
            width = Math.Max(4, width);
            height = Math.Max(4, height);
            if (width == _width && height == _height && _backBufferRtv != null) return;

            _width = width;
            _height = height;

            try
            {
                _context.OutputMerger.SetRenderTargets((DepthStencilView)null, (RenderTargetView)null);
                _context.ClearState();
                _context.Flush();

                _backBufferRtv?.Dispose();
                _backBufferRtv = null;

                if (_swapChain != null)
                {
                    _swapChain.ResizeBuffers(0, _width, _height, Format.Unknown, _swapChainFlags);
                    using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
                        _backBufferRtv = new RenderTargetView(_device, backBuffer);
                }

                CreateTargets();
            }
            catch
            {
                try { RebuildSwapChain(_frameCap); } catch { }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _context.ClearState(); _context.Flush(); } catch { }

            DisposeTargets();
            _pacer.Dispose();

            _backBufferRtv?.Dispose();
            _swapChain?.Dispose();
            _constantBuffer?.Dispose();
            _cbBlurHorizontal?.Dispose();
            _cbBlurVertical?.Dispose();

            _waveSrv?.Dispose();
            _waveTexture?.Dispose();
            _bandSrv?.Dispose();
            _bandTexture?.Dispose();

            _vsFullscreen?.Dispose();
            _psBars?.Dispose();
            _psRibbon?.Dispose();
            _psBloomBright?.Dispose();
            _psBloomBlur?.Dispose();
            _psComposite?.Dispose();

            _linearSampler?.Dispose();
            _pointSampler?.Dispose();
            _rasterState?.Dispose();
            _depthDisabled?.Dispose();

            _context?.Dispose();
            _device?.Dispose();
        }
    }
}
