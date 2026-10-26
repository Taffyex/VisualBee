using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("VisualBeePreview")]

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeControl : Control
    {
        private readonly MusicBeeApiInterface _api;
        private readonly VisualBeeSettings _settings;
        private readonly AudioEngine _audio;

        private D3DRenderer _renderer;
        private Thread _renderThread;
        private Timer _startTimer;
        private volatile bool _running;
        private volatile bool _startRequested;
        private volatile bool _suspended;
        private volatile bool _hostVisible;
        private volatile bool _disposeRequested;
        private int _swapchainRebuildPending;
        private int _appliedFrameCap;

        private int _pendingWidth;
        private int _pendingHeight;
        private int _lastWidth;
        private int _lastHeight;
        private int _resizePending;
        private float _dpiScale = 1f;

        private PaletteColors _palette;
        private readonly ManualResetEvent _renderStopped = new ManualResetEvent(true);
        private readonly ContextMenuStrip _menu = new ContextMenuStrip();
        private Form _popoutForm;

        internal D3DRenderer Renderer => _renderer;
        public double Fps => _renderer?.Fps ?? 0;
        public double FrameMs => _renderer?.FrameMs ?? 0;
        public long SlowFrames => _renderer?.SlowFrames ?? 0;

        public Bitmap CaptureBitmap()
        {
            var r = _renderer;
            if (r == null || !r.IsReady) return null;
            return r.CaptureToBitmap();
        }

        public VisualBeeControl(MusicBeeApiInterface api, VisualBeeSettings settings, AudioEngine audio)
        {
            _api = api;
            _settings = settings;
            _audio = audio;

            SetStyle(ControlStyles.Opaque |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            UpdateStyles();

            TabStop = true;
            _appliedFrameCap = _settings.FrameCap;
            RefreshPalette();
            _settings.SettingsChanged += OnSettingsChanged;
            SetupMenu();
        }

        private void OnSettingsChanged()
        {
            RefreshPalette();
            if (_settings.FrameCap != _appliedFrameCap)
            {
                _appliedFrameCap = _settings.FrameCap;
                Interlocked.Exchange(ref _swapchainRebuildPending, 1);
            }
            var r = _renderer;
            if (r != null && r.IsReady)
                r.SetPalette(_palette);
        }

        public void NotifyTrackChanged() { }
        public void NotifyPlayStateChanged() { }

        public void SetCustomPalette(PaletteColors p)
        {
            _palette = p;
            BackColor = p.Background;
            var r = _renderer;
            if (r != null && r.IsReady)
                r.SetPalette(p);
        }

        public void ShowSettingsWindow()
        {
            VisualBeeSettingsForm.ShowOrFocus(_api, _settings, this);
        }

        public void ToggleSettingsOverlay() => ShowSettingsWindow();

        public void RefreshPalette()
        {
            _palette = PaletteManager.Resolve(_settings.Palette, _api);
            BackColor = _palette.Background;
            var r = _renderer;
            if (r != null && r.IsReady)
                r.SetPalette(_palette);
        }

        private float ComputeDpiScale()
        {
            try
            {
                using (var g = CreateGraphics())
                    return Math.Max(1.0f, g.DpiX / 96.0f);
            }
            catch { return 1.0f; }
        }

        private bool CheckAndSyncVisibility()
        {
            if (_disposeRequested || IsDisposed) return false;
            bool vis = IsHandleCreated && Visible && ClientSize.Width >= 4 && ClientSize.Height >= 4;
            _hostVisible = vis;
            _suspended = !vis;
            return vis;
        }

        public void Start()
        {
            _disposeRequested = false;
            try { _audio.Start(); } catch { }
            _startRequested = true;
            if (CheckAndSyncVisibility())
                StartRendering();
            else
                ScheduleStart();
        }

        public bool Stop()
        {
            _startRequested = false;
            _suspended = true;
            if (_startTimer != null)
            {
                _startTimer.Stop();
                _startTimer.Dispose();
                _startTimer = null;
            }
            return StopRendering();
        }

        private void ScheduleStart()
        {
            if (!_startRequested || _running || _disposeRequested || IsDisposed) return;
            if (!IsHandleCreated)
            {
                try { CreateControl(); } catch { }
            }
            if (CheckAndSyncVisibility())
            {
                _startTimer?.Stop();
                StartRendering();
                return;
            }

            if (_startTimer == null)
            {
                _startTimer = new Timer { Interval = 100 };
                _startTimer.Tick += (s, e) =>
                {
                    if (!_startRequested || _running || _disposeRequested || IsDisposed)
                    {
                        _startTimer?.Stop();
                        return;
                    }
                    if (CheckAndSyncVisibility())
                    {
                        _startTimer?.Stop();
                        StartRendering();
                    }
                };
            }
            _startTimer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dpiScale = ComputeDpiScale();
            _pendingWidth = ClientSize.Width;
            _pendingHeight = ClientSize.Height;
            _lastWidth = Math.Max(1, _pendingWidth);
            _lastHeight = Math.Max(1, _pendingHeight);
            Interlocked.Exchange(ref _resizePending, 1);
            if (CheckAndSyncVisibility()) StartRendering();
            else ScheduleStart();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _hostVisible = false;
            _suspended = true;
            StopRendering();
            base.OnHandleDestroyed(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            Interlocked.Exchange(ref _pendingWidth, ClientSize.Width);
            Interlocked.Exchange(ref _pendingHeight, ClientSize.Height);
            Interlocked.Exchange(ref _resizePending, 1);
            CheckAndSyncVisibility();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (CheckAndSyncVisibility())
            {
                if (!_running && _startRequested) StartRendering();
                else _suspended = false;
            }
            else
            {
                _suspended = true;
            }
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent == null || Parent.IsDisposed)
            {
                _startRequested = false;
                _hostVisible = false;
                _suspended = true;
                StopRendering();
            }
            else
            {
                if (CheckAndSyncVisibility()) StartRendering();
                else if (Visible) ScheduleStart();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
                _menu.Show(this, e.Location);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            ShowSettingsWindow();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposeRequested = true;
                Stop();
                _settings.SettingsChanged -= OnSettingsChanged;
                _menu.Dispose();
                _popoutForm?.Dispose();
                VisualBeeSettingsForm.CloseIfOpen();
            }
            base.Dispose(disposing);
            if (disposing)
            {
                try { _renderStopped.Dispose(); } catch { }
            }
        }
    }
}
