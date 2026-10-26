using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace MusicBeePlugin
{
    internal sealed partial class D3DRenderer
    {
        private void CreateSwapChain(IntPtr hwnd, int frameCap)
        {
            using (var dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device>())
            using (var adapter = dxgiDevice.Adapter)
            using (var factory = adapter.GetParent<Factory2>())
            {
                FramePacer.QueryCapabilities(factory, out _tearingSupported);
                FramePacer.DetermineSwapchainParams(frameCap, _tearingSupported, out var flags, out var isVsync);

                var desc = new SwapChainDescription
                {
                    BufferCount = 2,
                    ModeDescription = new ModeDescription(_width, _height, new Rational(0, 1), Format.B8G8R8A8_UNorm),
                    IsWindowed = true,
                    OutputHandle = hwnd,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.FlipDiscard,
                    Usage = Usage.RenderTargetOutput,
                    Flags = flags
                };

                try
                {
                    _swapChain = new SwapChain(factory, _device, desc);
                    _swapChainFlags = desc.Flags;
                }
                catch
                {
                    // Fallback without special flags
                    desc.Flags = SwapChainFlags.None;
                    _swapChainFlags = SwapChainFlags.None;
                    try
                    {
                        _swapChain = new SwapChain(factory, _device, desc);
                    }
                    catch
                    {
                        // Fallback to Discard for legacy / virtualized environments
                        desc.BufferCount = 1;
                        desc.SwapEffect = SwapEffect.Discard;
                        _swapChain = new SwapChain(factory, _device, desc);
                    }
                }

                try { factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter); } catch { }
            }

            _pacer.AttachSwapChain(_swapChain, frameCap, _tearingSupported);

            using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
                _backBufferRtv = new RenderTargetView(_device, backBuffer);
        }

        private void CreatePipeline(string shaderCacheFolder)
        {
            string fullSource = D3DShaders.Common + D3DShaders.Bloom + D3DShaders.Bars + D3DShaders.Ribbon;

            _vsFullscreen = CompileOrLoadVS(fullSource, "VSFullscreen", shaderCacheFolder);
            _psBars = CompileOrLoadPS(fullSource, "PSBars", shaderCacheFolder);
            _psRibbon = CompileOrLoadPS(fullSource, "PSRibbon", shaderCacheFolder);

            _psBloomBright = CompileOrLoadPS(fullSource, "PSBloomBright", shaderCacheFolder);
            _psBloomBlur = CompileOrLoadPS(fullSource, "PSBloomBlur", shaderCacheFolder);
            _psComposite = CompileOrLoadPS(fullSource, "PSComposite", shaderCacheFolder);

            _constantBuffer = new Buffer(_device, new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<VisualBeeConstants>(),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            });

            // Immutable Direction Constant Buffers for Bloom Blur passes
            var hBlur = new BlurConstants { BlurDir = new Vector4(1f, 0f, 0f, 0f) };
            _cbBlurHorizontal = Buffer.Create(_device, BindFlags.ConstantBuffer, ref hBlur);

            var vBlur = new BlurConstants { BlurDir = new Vector4(0f, 1f, 0f, 0f) };
            _cbBlurVertical = Buffer.Create(_device, BindFlags.ConstantBuffer, ref vBlur);

            // 512-texel waveform texture (upsampled via Catmull-Rom, R = Left, G = Right)
            _waveTexture = new Texture1D(_device, new Texture1DDescription
            {
                Width = 512,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32G32_Float,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            });
            _waveSrv = new ShaderResourceView(_device, _waveTexture);

            // 320-texel R32G32_Float band texture (Left in 0..159, Right in 160..319, height in R, peak in G)
            _bandTexture = new Texture1D(_device, new Texture1DDescription
            {
                Width = 320,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32G32_Float,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            });
            _bandSrv = new ShaderResourceView(_device, _bandTexture);

            _linearSampler = new SamplerState(_device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            _pointSampler = new SamplerState(_device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            _rasterState = new RasterizerState(_device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsDepthClipEnabled = false,
                IsScissorEnabled = false
            });

            _depthDisabled = new DepthStencilState(_device, new DepthStencilStateDescription
            {
                IsDepthEnabled = false,
                IsStencilEnabled = false
            });
        }

        private void CreateTargets()
        {
            DisposeTargets();

            _sceneTexture = new Texture2D(_device, new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R16G16B16A16_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            _sceneRtv = new RenderTargetView(_device, _sceneTexture);
            _sceneSrv = new ShaderResourceView(_device, _sceneTexture);

            _bloomWidth = Math.Max(2, _width / 2);
            _bloomHeight = Math.Max(2, _height / 2);

            var bloomDesc = new Texture2DDescription
            {
                Width = _bloomWidth,
                Height = _bloomHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R16G16B16A16_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _bloomTextureA = new Texture2D(_device, bloomDesc);
            _bloomRtvA = new RenderTargetView(_device, _bloomTextureA);
            _bloomSrvA = new ShaderResourceView(_device, _bloomTextureA);

            _bloomTextureB = new Texture2D(_device, bloomDesc);
            _bloomRtvB = new RenderTargetView(_device, _bloomTextureB);
            _bloomSrvB = new ShaderResourceView(_device, _bloomTextureB);
        }

        private void DisposeTargets()
        {
            _sceneRtv?.Dispose(); _sceneRtv = null;
            _sceneSrv?.Dispose(); _sceneSrv = null;
            _sceneTexture?.Dispose(); _sceneTexture = null;

            _bloomRtvA?.Dispose(); _bloomRtvA = null;
            _bloomSrvA?.Dispose(); _bloomSrvA = null;
            _bloomTextureA?.Dispose(); _bloomTextureA = null;

            _bloomRtvB?.Dispose(); _bloomRtvB = null;
            _bloomSrvB?.Dispose(); _bloomSrvB = null;
            _bloomTextureB?.Dispose(); _bloomTextureB = null;
        }

        private VertexShader CompileOrLoadVS(string source, string entry, string cacheFolder)
        {
            byte[] bytes = LoadOrCompileBytecode(source, entry, "vs_5_0", cacheFolder);
            return new VertexShader(_device, bytes);
        }

        private PixelShader CompileOrLoadPS(string source, string entry, string cacheFolder)
        {
            byte[] bytes = LoadOrCompileBytecode(source, entry, "ps_5_0", cacheFolder);
            return new PixelShader(_device, bytes);
        }

        private static byte[] LoadOrCompileBytecode(string source, string entry, string profile, string cacheFolder)
        {
            string cachePath = null;
            try
            {
                if (string.IsNullOrEmpty(cacheFolder))
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    cacheFolder = Path.Combine(appData, "MusicBee", "VisualBee", "shadercache");
                }
                Directory.CreateDirectory(cacheFolder);

                using (var sha = SHA1.Create())
                {
                    byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
                    string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant().Substring(0, 12);
                    cachePath = Path.Combine(cacheFolder, $"{entry}_{profile}_{hash}.cso");
                }

                if (File.Exists(cachePath))
                {
                    byte[] cached = File.ReadAllBytes(cachePath);
                    if (cached != null && cached.Length > 16) return cached;
                }
            }
            catch { }

            using (CompilationResult result = ShaderBytecode.Compile(source, entry, profile, ShaderFlags.OptimizationLevel3, EffectFlags.None))
            {
                byte[] bytes = result.Bytecode.Data;
                if (!string.IsNullOrEmpty(cachePath))
                {
                    try { File.WriteAllBytes(cachePath, bytes); } catch { }
                }
                return bytes;
            }
        }
    }
}
