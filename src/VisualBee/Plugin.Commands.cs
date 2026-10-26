using System;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private void RegisterCommands()
        {
            if (Api.MB_RegisterCommand == null) return;

            Api.MB_RegisterCommand("VisualBee: Next Visualizer Mode", delegate { CycleMode(); });
            Api.MB_RegisterCommand("VisualBee: Next Color Palette", delegate { CyclePalette(); });
            Api.MB_RegisterCommand("VisualBee: Cycle Bar Mirror", delegate { CycleMirror(); });
            Api.MB_RegisterCommand("VisualBee: Cycle Frame Cap", delegate { CycleFrameCap(); });
            Api.MB_RegisterCommand("VisualBee: Popout Window", delegate { TogglePopout(); });
            Api.MB_RegisterCommand("VisualBee: Settings", delegate { ShowSettings(); });
            Api.MB_RegisterCommand("VisualBee: Toggle Diagnostics", delegate { ToggleDiagnostics(); });
        }

        private void CycleMode()
        {
            if (Settings == null) return;
            int next = ((int)Settings.Mode + 1) % 2;
            Settings.Mode = (VisualizerMode)next;
            Settings.NotifyChanged();
            Settings.RequestSave();
        }

        private void CyclePalette()
        {
            if (Settings == null) return;
            var presets = PresetManager.GetAllPresets(Api);
            if (presets.Count == 0) return;
            int cur = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i].Key.Equals(Settings.Palette, StringComparison.OrdinalIgnoreCase))
                {
                    cur = i;
                    break;
                }
            }
            Settings.Palette = presets[(cur + 1) % presets.Count].Key;
            Settings.NotifyChanged();
            Settings.RequestSave();
        }

        private void CycleMirror()
        {
            if (Settings == null) return;
            int cur = (int)Settings.Mirror;
            int next = (cur + 1) % 4;
            Settings.Mirror = (BarMirror)next;
            Settings.NotifyChanged();
            Settings.RequestSave();
        }

        private void CycleFrameCap()
        {
            if (Settings == null) return;
            int[] caps = { 0, 60, 120, 144, 240, 360, -1 };
            int cur = 0;
            for (int i = 0; i < caps.Length; i++)
            {
                if (caps[i] == Settings.FrameCap)
                {
                    cur = i;
                    break;
                }
            }
            Settings.FrameCap = caps[(cur + 1) % caps.Length];
            Settings.NotifyChanged();
            Settings.RequestSave();
        }

        private void TogglePopout()
        {
            _control?.TogglePopout();
        }

        private void ShowSettings()
        {
            VisualBeeSettingsForm.ShowOrFocus(Api, Settings, _control);
        }

        private void ToggleDiagnostics()
        {
            if (Settings == null) return;
            Settings.ShowDiagnostics = !Settings.ShowDiagnostics;
            Settings.NotifyChanged();
            Settings.RequestSave();
        }
    }
}
