using System;
using System.Collections.Generic;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private void RegisterCommands()
        {
            if (_api.MB_RegisterCommand == null)
            {
                return;
            }

            _api.MB_RegisterCommand("VisualBee: Next Layout", (s, e) => CycleLayout(1));
            _api.MB_RegisterCommand("VisualBee: Previous Layout", (s, e) => CycleLayout(-1));
            _api.MB_RegisterCommand("VisualBee: Next Preset", (s, e) => CyclePreset(1));
            _api.MB_RegisterCommand("VisualBee: Previous Preset", (s, e) => CyclePreset(-1));
            _api.MB_RegisterCommand("VisualBee: Toggle Full Screen", (s, e) => _host?.ToggleFullScreen(null));
            _api.MB_RegisterCommand("VisualBee: Settings", (s, e) => _host?.OpenSettings(null));
        }

        private void CycleLayout(int delta)
        {
            if (_host?.Store == null)
            {
                return;
            }
            LookStore store = _host.Store;
            SpectrumLook look = store.Current as SpectrumLook;
            if (look == null)
            {
                return;
            }
            int total = SpectrumLook.LayoutNames.Length;
            int next = (look.Layout + delta) % total;
            if (next < 0) next += total;
            Setting s = store.Schema.Find("layout");
            if (s != null)
            {
                store.Set(s, next);
            }
        }

        private void CyclePreset(int delta)
        {
            if (_host?.Store == null)
            {
                return;
            }
            LookStore store = _host.Store;
            List<Preset> presets = new List<Preset>(store.BuiltIn);
            presets.AddRange(store.UserPresets());
            if (presets.Count == 0)
            {
                return;
            }
            int curIdx = presets.FindIndex(p => p.Name == store.PresetName);
            int nextIdx = (curIdx + delta) % presets.Count;
            if (nextIdx < 0) nextIdx += presets.Count;
            store.Apply(presets[nextIdx]);
        }
    }
}
