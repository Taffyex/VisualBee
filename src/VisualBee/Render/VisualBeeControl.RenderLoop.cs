using System;
using System.Diagnostics;
using System.Threading;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeControl
    {
        private void StartRendering()
        {
            if (_running || _disposeRequested || IsDisposed) return;
            if (!IsHandleCreated) CreateControl();
            if (!CheckAndSyncVisibility()) return;

            _renderStopped.Reset();
            _running = true;

            IntPtr hwnd = Handle;
            int width = Math.Max(4, ClientSize.Width);
            int height = Math.Max(4, ClientSize.Height);
            int frameCap = _settings.FrameCap;

            var thread = new Thread(() => RenderLoopWorker(hwnd, width, height, frameCap))
            {
                IsBackground = true,
                Name = "VisualBee D3D11 render",
                Priority = ThreadPriority.AboveNormal
            };
            _renderThread = thread;
            try
            {
                thread.Start();
            }
            catch
            {
                _running = false;
                _renderStopped.Set();
            }
        }

        private bool StopRendering()
        {
            _running = false;
            _suspended = true;
            Thread t = _renderThread;
            _renderThread = null;
            bool exited = true;
            try
            {
                exited = _renderStopped.WaitOne(300);
            }
            catch
            {
                exited = true;
            }

            if (!exited && t != null && t.IsAlive)
            {
                try { t.Abort(); } catch { }
            }
            return exited;
        }

        private void RenderLoopWorker(IntPtr hwnd, int initialWidth, int initialHeight, int frameCap)
        {
            D3DRenderer renderer = null;
            try
            {
                string cacheFolder = null;
                try
                {
                    if (_api.Setting_GetPersistentStoragePath != null)
                        cacheFolder = System.IO.Path.Combine(_api.Setting_GetPersistentStoragePath(), "shadercache");
                }
                catch { }

                renderer = new D3DRenderer(hwnd, initialWidth, initialHeight, frameCap, cacheFolder);
                renderer.SetPalette(_palette);
                _renderer = renderer;
            }
            catch
            {
                _running = false;
                _renderStopped.Set();
                return;
            }

            var sw = Stopwatch.StartNew();
            var dynamics = new BarDynamics();
            var frame = new BandFrame();
            var heightPeak = new float[320 * 2];
            double lastTime = sw.Elapsed.TotalSeconds;

            try
            {
                while (_running && renderer != null && !renderer.IsDisposed)
                {
                    if (_suspended || !_hostVisible || _disposeRequested)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    if (Interlocked.Exchange(ref _swapchainRebuildPending, 0) != 0 || renderer.NeedsRebuildRetry)
                    {
                        if (renderer.RebuildAttempts > 5)
                        {
                            try
                            {
                                renderer.Dispose();
                                string cacheFolder = null;
                                try
                                {
                                    if (_api.Setting_GetPersistentStoragePath != null)
                                        cacheFolder = System.IO.Path.Combine(_api.Setting_GetPersistentStoragePath(), "shadercache");
                                }
                                catch { }
                                renderer = new D3DRenderer(hwnd, _lastWidth, _lastHeight, _settings.FrameCap, cacheFolder);
                                renderer.SetPalette(_palette);
                                _renderer = renderer;
                            }
                            catch { }
                        }
                        else
                        {
                            try { renderer.RebuildSwapChain(_settings.FrameCap); } catch { }
                        }
                    }

                    if (Interlocked.Exchange(ref _resizePending, 0) != 0)
                    {
                        int w = Interlocked.Exchange(ref _pendingWidth, -1);
                        int h = Interlocked.Exchange(ref _pendingHeight, -1);
                        if (w > 3 && h > 3)
                        {
                            try { renderer.Resize(w, h); } catch { }
                            _lastWidth = w;
                            _lastHeight = h;
                        }
                    }

                    if (_lastWidth < 4 || _lastHeight < 4 || !renderer.IsReady)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    try
                    {
                        double now = sw.Elapsed.TotalSeconds;
                        float dt = (float)Math.Max(0.001, Math.Min(0.1, now - lastTime));
                        lastTime = now;

                        _audio.GetFrame(frame);
                        bool isSilent = (frame.Metrics.Source == AudioSourceKind.Silent);

                        renderer.Pacer.WaitForFrame(_settings, isSilent);

                        dynamics.Push(frame);
                        dynamics.Step(now, dt, _settings, heightPeak);

                        renderer.Render(frame.Metrics, heightPeak, now, _dpiScale, _settings);
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(50);
                    }
                }
            }
            finally
            {
                _renderer = null;
                try { renderer?.Dispose(); } catch { }
                try { _renderStopped.Set(); } catch { }
            }
        }
    }
}
