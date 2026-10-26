using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace MusicBeePlugin
{
    internal sealed partial class D3DRenderer
    {
        private readonly float[] _upsampledWaveform = new float[1024];

        public void Render(
            AudioSnapshot snap,
            float[] heightPeakBands,
            double time,
            float dpiScale,
            VisualBeeSettings settings)
        {
            if (_disposed || _swapChain == null || _backBufferRtv == null) return;

            long now = _clock.ElapsedTicks;
            if (_lastFrameTicks != 0)
            {
                double ms = (now - _lastFrameTicks) * 1000.0 / Stopwatch.Frequency;
                _frameMs = _frameMs * 0.9 + ms * 0.1;
                if (ms > _maxFrameMs) _maxFrameMs = ms;

                double targetIntervalMs = (_frameCap > 0) ? (1000.0 / _frameCap) : 8.33;
                if (ms > targetIntervalMs * 1.5) _slowFrames++;
                if (ms > 0.001) _fps = _fps * 0.9 + (1000.0 / ms) * 0.1;
            }
            _lastFrameTicks = now;

            UpdateWaveTexture(snap);
            UpdateBandTexture(heightPeakBands);

            // Update constant buffer via Map(WriteDiscard)
            _constants.Viewport = new Vector4(_width, _height, 1f / _width, 1f / _height);
            _constants.AudioMetricsA = new Vector4(snap.LowPulse, snap.Bass, snap.Mid, snap.High);
            _constants.AudioMetricsB = new Vector4(snap.Beat, snap.Rms, snap.Centroid, snap.Transient);
            float bloomIntensity = settings.BloomIntensity > 0.05f ? settings.BloomIntensity : 0.0f;
            _constants.VisualParams = new Vector4((float)time, (float)settings.BarStyle, bloomIntensity, (float)settings.Mode);
            _constants.BarParams = new Vector4(
                settings.BarCount,
                settings.BarGap,
                (float)(int)settings.Mirror,
                settings.PeakHoldEnabled ? 1f : 0f);
            _constants.RibbonParams = new Vector4(
                settings.RibbonThickness,
                settings.RibbonFill ? 1f : 0f,
                settings.RibbonFloorReflection ? 1f : 0f,
                dpiScale);
            _constants.StyleExtra = new Vector4(
                (float)(int)settings.HueMotion,
                (float)(int)settings.StereoMode,
                settings.HighlightBarTips ? 1.0f : 0.0f,
                (float)(int)settings.HueSource);

            // Background resolution & brightness
            Vector4 baseBg;
            switch (settings.BgSource)
            {
                case BackgroundSource.PureBlack:
                    baseBg = new Vector4(0f, 0f, 0f, 1f);
                    break;
                case BackgroundSource.SkinPanel:
                case BackgroundSource.Palette:
                default:
                    baseBg = ToLinearVec4(_currentPalette.Background);
                    break;
            }
            float bgb = Math.Max(0.0f, settings.BgBrightness);
            _constants.PaletteBg = new Vector4(baseBg.X * bgb, baseBg.Y * bgb, baseBg.Z * bgb, 1.0f);

            _constants.BarShape = new Vector4(
                settings.BarRoundness,
                settings.LedSegmentHeight * dpiScale,
                settings.LedSegmentGap * dpiScale,
                settings.TrebleTaper);

            _constants.ColorParams = new Vector4(
                (float)(int)settings.ColorMode,
                settings.HueSpeed,
                settings.BgBrightness,
                settings.Vignette);

            _constants.PeakParams = new Vector4(
                settings.PeakCapThickness * dpiScale,
                (float)(int)settings.PeakCapColorMode,
                settings.FloorReflection,
                settings.ContactGlow ? 1f : 0f);

            _constants.PeakColor = ToLinearVec4(settings.PeakCustomColor);

            DataBox cbBox = _context.MapSubresource(_constantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            try
            {
                Marshal.StructureToPtr(_constants, cbBox.DataPointer, false);
            }
            finally
            {
                _context.UnmapSubresource(_constantBuffer, 0);
            }

            _context.Rasterizer.State = _rasterState;
            _context.OutputMerger.SetDepthStencilState(_depthDisabled, 0);
            _context.VertexShader.Set(_vsFullscreen);
            _context.VertexShader.SetConstantBuffer(0, _constantBuffer);
            _context.PixelShader.SetConstantBuffer(0, _constantBuffer);
            _context.PixelShader.SetSampler(0, _linearSampler);
            _context.PixelShader.SetSampler(1, _pointSampler);

            // -------------------------------------------------------------
            // Pass 1: Main Scene Render
            // -------------------------------------------------------------
            _context.Rasterizer.SetViewport(0, 0, _width, _height, 0f, 1f);
            _context.OutputMerger.SetRenderTargets(_sceneRtv);

            _context.PixelShader.SetShaderResource(0, _waveSrv);
            _context.PixelShader.SetShaderResource(1, _bandSrv);

            switch (settings.Mode)
            {
                case VisualizerMode.Waveform:
                    _context.PixelShader.Set(_psRibbon);
                    break;
                case VisualizerMode.SpectrumBars:
                default:
                    _context.PixelShader.Set(_psBars);
                    break;
            }

            _context.Draw(3, 0);

            // -------------------------------------------------------------
            // Bloom Passes (4-Tap Box Downsample + Directional Blur)
            // -------------------------------------------------------------
            if (settings.BloomIntensity > 0.05f)
            {
                _context.OutputMerger.SetRenderTargets((RenderTargetView)null);

                // Pass 2: Bright Extraction (4-tap box filter)
                _context.Rasterizer.SetViewport(0, 0, _bloomWidth, _bloomHeight, 0f, 1f);
                _context.OutputMerger.SetRenderTargets(_bloomRtvA);
                _context.PixelShader.Set(_psBloomBright);
                _context.PixelShader.SetShaderResource(2, _sceneSrv);
                _context.Draw(3, 0);

                // Pass 3: Horizontal Blur with immutable CB at b1
                _context.OutputMerger.SetRenderTargets((RenderTargetView)null);
                _context.OutputMerger.SetRenderTargets(_bloomRtvB);
                _context.PixelShader.SetConstantBuffer(1, _cbBlurHorizontal);
                _context.PixelShader.Set(_psBloomBlur);
                _context.PixelShader.SetShaderResource(2, _bloomSrvA);
                _context.Draw(3, 0);

                // Pass 4: Vertical Blur with immutable CB at b1
                _context.OutputMerger.SetRenderTargets((RenderTargetView)null);
                _context.OutputMerger.SetRenderTargets(_bloomRtvA);
                _context.PixelShader.SetConstantBuffer(1, _cbBlurVertical);
                _context.PixelShader.Set(_psBloomBlur);
                _context.PixelShader.SetShaderResource(2, _bloomSrvB);
                _context.Draw(3, 0);
            }

            // -------------------------------------------------------------
            // Pass 5: Final Composite to BackBuffer
            // -------------------------------------------------------------
            _context.OutputMerger.SetRenderTargets((RenderTargetView)null);
            _context.Rasterizer.SetViewport(0, 0, _width, _height, 0f, 1f);
            _context.OutputMerger.SetRenderTargets(_backBufferRtv);
            _context.PixelShader.Set(_psComposite);
            _context.PixelShader.SetShaderResource(2, _sceneSrv);
            _context.PixelShader.SetShaderResource(3, _bloomSrvA);
            _context.Draw(3, 0);

            // Unbind textures to prevent hazards on subsequent frame
            _context.PixelShader.SetShaderResource(2, null);
            _context.PixelShader.SetShaderResource(3, null);

            // Present via FramePacer
            _pacer.Present(_swapChain);
        }

        private void UpdateWaveTexture(AudioSnapshot snap)
        {
            if (_waveTexture == null) return;
            float[] srcL = snap.WaveformL ?? snap.Waveform;
            float[] srcR = snap.WaveformR ?? snap.Waveform;

            if (srcL == null || srcL.Length < 256)
            {
                Array.Clear(_upsampledWaveform, 0, 1024);
            }
            else
            {
                for (int i = 0; i < 512; i++)
                {
                    float u = i * (255f / 511f);
                    int k = (int)u;
                    float t = u - k;

                    float l0 = srcL[Math.Max(0, k - 1)];
                    float l1 = srcL[k];
                    float l2 = srcL[Math.Min(255, k + 1)];
                    float l3 = srcL[Math.Min(255, k + 2)];
                    float valL = 0.5f * ((2f * l1) + (-l0 + l2) * t + (2f * l0 - 5f * l1 + 4f * l2 - l3) * (t * t) + (-l0 + 3f * l1 - 3f * l2 + l3) * (t * t * t));

                    float r0 = (srcR != null && srcR.Length >= 256) ? srcR[Math.Max(0, k - 1)] : l0;
                    float r1 = (srcR != null && srcR.Length >= 256) ? srcR[k] : l1;
                    float r2 = (srcR != null && srcR.Length >= 256) ? srcR[Math.Min(255, k + 1)] : l2;
                    float r3 = (srcR != null && srcR.Length >= 256) ? srcR[Math.Min(255, k + 2)] : l3;
                    float valR = 0.5f * ((2f * r1) + (-r0 + r2) * t + (2f * r0 - 5f * r1 + 4f * r2 - r3) * (t * t) + (-r0 + 3f * r1 - 3f * r2 + r3) * (t * t * t));

                    _upsampledWaveform[2 * i] = valL;
                    _upsampledWaveform[2 * i + 1] = valR;
                }
            }

            DataBox data = _context.MapSubresource(_waveTexture, 0, MapMode.WriteDiscard, MapFlags.None);
            try
            {
                Marshal.Copy(_upsampledWaveform, 0, data.DataPointer, 1024);
            }
            finally
            {
                _context.UnmapSubresource(_waveTexture, 0);
            }
        }

        private void UpdateBandTexture(float[] heightPeakBands)
        {
            if (_bandTexture == null) return;
            DataBox data = _context.MapSubresource(_bandTexture, 0, MapMode.WriteDiscard, MapFlags.None);
            try
            {
                if (heightPeakBands == null)
                {
                    for (int i = 0; i < 320 * 2; i++)
                        Marshal.WriteInt32(data.DataPointer, i * 4, 0);
                }
                else
                {
                    int count = Math.Min(640, heightPeakBands.Length);
                    Marshal.Copy(heightPeakBands, 0, data.DataPointer, count);
                }
            }
            finally
            {
                _context.UnmapSubresource(_bandTexture, 0);
            }
        }

        public Bitmap CaptureToBitmap()
        {
            if (_device == null || _sceneTexture == null) return null;

            var desc = new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            };

            // Intermediate texture to resolve composite / backbuffer
            var resolveDesc = new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None
            };

            using (var resolveTex = new Texture2D(_device, resolveDesc))
            using (var rtv = new RenderTargetView(_device, resolveTex))
            using (var staging = new Texture2D(_device, desc))
            {
                // Composite to resolve texture
                _context.OutputMerger.SetRenderTargets((RenderTargetView)null);
                _context.Rasterizer.SetViewport(0, 0, _width, _height, 0f, 1f);
                _context.OutputMerger.SetRenderTargets(rtv);
                _context.PixelShader.Set(_psComposite);
                _context.PixelShader.SetShaderResource(2, _sceneSrv);
                _context.PixelShader.SetShaderResource(3, _bloomSrvA);
                _context.Draw(3, 0);

                _context.PixelShader.SetShaderResource(2, null);
                _context.PixelShader.SetShaderResource(3, null);
                _context.OutputMerger.SetRenderTargets((RenderTargetView)null);

                _context.CopyResource(resolveTex, staging);

                DataBox box = _context.MapSubresource(staging, 0, MapMode.Read, MapFlags.None);
                try
                {
                    var bmp = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
                    var bData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, _width, _height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        for (int y = 0; y < _height; y++)
                        {
                            IntPtr srcPtr = box.DataPointer + y * box.RowPitch;
                            IntPtr dstPtr = bData.Scan0 + y * bData.Stride;
                            Utilities.CopyMemory(dstPtr, srcPtr, _width * 4);
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(bData);
                    }
                    return bmp;
                }
                finally
                {
                    _context.UnmapSubresource(staging, 0);
                }
            }
        }
    }
}
