using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: InternalsVisibleTo("CaptureVisualBee")]

[assembly: InternalsVisibleTo("VisualBeePreview")]

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        internal MusicBeeApiInterface _api;
        private readonly PluginInfo _about = new PluginInfo();
        internal VisualiserHost _host;
        private float[] _frames = new float[0];

        public const short PluginInfoVersion = 1;
        public const short MinInterfaceVersion = 41;
        public const short MinApiRevision = 53;

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            _api.Initialise(apiInterfacePtr);
            LookStore store = SpectrumSettings.Create();
            _host = new VisualiserHost(store, (VisualiserHost host, bool fullScreen) => new SpectrumView(this, host, fullScreen));
            _host.SetApi(_api);
            _host.LoadSettings("mb_VisualBee.ini", (string file) =>
            {
                SpectrumSettings.Migrate(store, file);
                return true;
            });

            _about.PluginInfoVersion = 1;
            _about.Name = "VisualBee";
            _about.Description = "Ultra high-performance hardware-accelerated Direct3D 11 spectrum analyzer and phase-locked oscilloscope with bloom, trails, and custom themes.";
            _about.Author = "VisualBee";
            _about.TargetApplication = "VisualBee";
            _about.Type = PluginType.PanelView;
            _about.VersionMajor = 2;
            _about.VersionMinor = 1;
            _about.Revision = 1;
            _about.MinInterfaceVersion = 41;
            _about.MinApiRevision = 53;
            _about.ReceiveNotifications = ReceiveNotificationFlags.PlayerEvents;
            _about.ConfigurationPanelHeight = PanelLayout.S(150);

            RegisterCommands();

            return _about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            return _host != null && _host.Configure(panelHandle);
        }

        public void SaveSettings()
        {
            _host?.Store?.Save();
        }

        public void Close(PluginCloseReason reason)
        {
            _host?.Dispose();
            _host?.Store?.Save();
        }

        public void Uninstall()
        {
            _host?.Dispose();
        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            if (type == NotificationType.TrackChanged)
            {
                _host?.TrackChanged();
            }
            else if (type == NotificationType.PlayStateChanged)
            {
                _host?.PlayStateChanged();
            }
        }

        public int OnDockablePanelCreated(Control panel)
        {
            if (panel == null)
            {
                return 0;
            }
            if (_host == null)
            {
                return 0;
            }
            if (panel.InvokeRequired)
            {
                panel.Invoke(new Action<Control>(_host.Attach), panel);
            }
            else
            {
                _host.Attach(panel);
            }
            return 0;
        }

        internal string StatusText()
        {
            return _host?.StatusText() ?? "";
        }
    }
}
