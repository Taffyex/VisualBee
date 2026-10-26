using System;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        internal static Plugin Instance;
        internal static MusicBeeApiInterface Api;
        internal static VisualBeeSettings Settings;
        internal static AudioEngine Audio;
        private VisualBeeControl _control;

        static Plugin()
        {
            EmbeddedResolver.EnsureRegistered();
        }

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            EmbeddedResolver.EnsureRegistered();
            Api = default(MusicBeeApiInterface);
            Api.Initialise(apiInterfacePtr);
            Instance = this;

            Settings = VisualBeeSettings.Load(Api);
            PresetManager.Initialize(Api);
            Audio = new AudioEngine(Api, Settings);
            try { Audio.Probe(); } catch { }

            RegisterCommands();

            return new PluginInfo
            {
                PluginInfoVersion = MusicBeeApiInfo.PluginInfoVersion,
                Type = PluginType.General,
                Name = "VisualBee Visualizer",
                Description = "Hardware-accelerated Direct3D 11 audio visualizer with high-refresh flip presentation, Gaussian FFT, and customizable bar dynamics.",
                Author = "VisualBee Team",
                TargetApplication = "VisualBee",
                VersionMajor = 1,
                VersionMinor = 0,
                Revision = 0,
                MinInterfaceVersion = MusicBeeApiInfo.MinInterfaceVersion,
                MinApiRevision = MusicBeeApiInfo.MinApiRevision,
                ReceiveNotifications = ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents,
                ConfigurationPanelHeight = 0
            };
        }

        public void Close(PluginCloseReason reason)
        {
            Teardown();
            Instance = null;
        }

        public void Uninstall()
        {
            Teardown();
            Instance = null;
        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            try
            {
                if ((int)type == 14) // SkinChanged
                {
                    _control?.RefreshPalette();
                }
                else if (type == NotificationType.TrackChanged)
                {
                    Audio?.NotifyTrackChanged();
                    _control?.NotifyTrackChanged();
                }
                else if (type == NotificationType.PlayStateChanged)
                {
                    Audio?.NotifyPlayStateChanged();
                    _control?.NotifyPlayStateChanged();
                }
            }
            catch { }
        }

        private void Teardown()
        {
            VisualBeeSettingsForm.CloseIfOpen();
            DisposePanel();
            try { Audio?.Dispose(); } catch { }
            Audio = null;
            try { Settings?.Save(); } catch { }
        }
    }
}
