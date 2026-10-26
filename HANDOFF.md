# VisualBee v2.0.0 — Architecture & Handoff Reference

**Target:** MusicBee 3.4 – 3.7+ (WinForms / x86 / .NET Framework 4.8)  
**Public Repository:** [https://github.com/Taffyex/VisualBee](https://github.com/Taffyex/VisualBee)  
**Release Tag:** `v2.0.0`  
**Local Codebases:** 
- Workspace project: `projects/VisualBee` (within `dev_tools/reverseEngineerAnything`)
- Standalone repository: `C:\Users\teodo\Documents\LLM_PROJECTS\VisualBee`
**Deployed Host Path:** `%APPDATA%\MusicBee\Plugins\mb_VisualBee.dll`

---

## 1. Release Summary (v2.0.0 — "Ban Hammer" Overhaul)

- **Version:** `v2.0.0` (Complete Visual & QoL Studio Overhaul)
- **Standalone Binary:** `release/mb_VisualBee.dll` (~1.28 MB, x86, net48, zero external dependencies).
- **Release Archive:** `release/VisualBee_v2.0.zip`
- **Embedded Assemblies:** SharpDX, SharpDX.Direct3D11, SharpDX.DXGI, SharpDX.D3DCompiler, SharpDX.Mathematics.
- **Presentation:** DXGI Flip-Model (`DXGI_SWAP_EFFECT_FLIP_DISCARD`) with waitable swapchain latency objects (`DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT`).
- **Pacing:** Sub-millisecond multimedia pacing at 60 Hz, 120 Hz, 144 Hz, 240 Hz, or uncapped.
- **Audio Engine:** 64-bit BASS stream tap with Gaussian FFT windowing and dynamic decibel scaling.

---

## 2. Key Features & Visual Pipeline (v2.0.0)

### A. Transparent Glass Tube Enclosures & Ghosted Phosphor Ladders
- **Full-Height Glass Cylinders:** Each frequency bar is encapsulated in a translucent glass tube featuring rounded dome caps, Fresnel edge glow, and specular highlights.
- **Ghosted Phosphor Ladders:** Soft, translucent unlit ladder rungs persist above active audio bars up to the peak needle, mirroring vintage VFD filaments.
- **Glass Fill Modes:**
  - `Mixed` (Alternates solid plasma core tubes and segmented VFD rungs).
  - `SegmentedRungs` (100% segmented ladder rungs across all bars).
  - `SolidGlass` (Continuous incandescent plasma columns).
- **Rung Controls:** Fine-tuned sliders for rung thickness, rung spacing/gaps, unlit ghost phosphor opacity, and ghosted ladder trail height.

### B. Center-Axis Symmetry & Horizontal Laser Beam Datum
- **True Symmetrical Incandescence:** Full equal up/down thermal distribution from the center axis (`y = 0.5`), resolving previous asymmetrical heat ramps.
- **Horizontal Center Laser Line:** High-intensity electric cyan/white datum line bisecting the visualizer, anchoring vertical mirror modes.

### C. GPU Atmosphere Effects & 3D Shaders
- **3D Parallax Bokeh Embers:** Multi-layer floating dust/ember particles with depth parallax, drifting behind visualizer meters.
- **3D Perspective Cyber-Grid:** Ground grid horizon that rolls dynamically with time and reacts to low-frequency bass pulses.
- **Scanline Shimmer:** Configurable CRT/VFD horizontal scanlines with subtle analog phosphor shimmer.
- **Two-Pass Gaussian Bloom:** ACES tone-mapped HDR glow preserving crisp incandescent cores while casting wide chromatic coronas.

### D. 4-Smart-Tab Settings Studio & 1-Click Vibe Bar
- **Tab 1: Visualizer Studio:** Mode selection, bar geometry, glass tube & ladder controls, symmetry, peak ballistics, and waveform controls.
- **Tab 2: Theme & Atmosphere:** Color presets, interactive custom palette swatches (A, B, C, D, Bg), bokeh embers, cyber-grid horizon, and scanlines.
- **Tab 3: Audio & Dynamics:** Attack/decay physics, peak drop gravity, input gain, dynamic dB range, and treble tilt equalization.
- **Tab 4: System & Telemetry:** Frame rate limiters, audio FFT rate, low-power background throttling, popout window, and real-time GPU/audio stats.
- **Top 1-Click Vibe Bar:** Instant presets for:
  - `Glass Pillars (Ref 1)`
  - `Center-Axis LED (Ref 2)`
  - `Cyber Horizon`
  - `Liquid Waveform`

### E. Quality-of-Life & Bug Fixes ("Ban Hammer")
- **Slider Scroll Hijacking Eliminated:** `VbSlider` forwards mouse wheel scroll events directly to parent panels; wheel adjustments now require `Ctrl + Wheel`.
- **Wired 7 Previously Dead Switches:**
  1. `ShowDiagnostics` HUD overlay pill displaying real-time FPS, frame ms, and audio stats.
  2. `BgSource == SkinPanel` resolving live MusicBee theme background color via API.
  3. `HighlightBarTips` active across all bar styles.
  4. Liquid Neon `Waveform` connected to atmosphere shaders, mirroring, and decibel lines.
  5. Desynchronized context menu items fully synchronized.
  6. Center laser line enabled and adjustable.
  7. Mode-locked orphaned controls pruned from the UI.

---

## 3. Directory Map

```
VisualBee/
├── HANDOFF.md                    # This architecture and handoff document
├── VisualBee.sln                 # Standalone solution
├── release/                      # Precompiled binary & zip
│   ├── mb_VisualBee.dll
│   └── VisualBee_v2.0.zip
├── screenshots/                  # High-resolution raster captures
│   ├── vfd_ref1_mixed_pillars.png
│   ├── vfd_ref2_center_led.png
│   ├── cyber_grid_horizon.png
│   ├── settings_studio.png
│   └── settings_theme.png
└── src/VisualBee/
    ├── VisualBee.csproj          # SDK-style x86 .csproj with embedded SharpDX target
    ├── MusicBeeInterface.cs      # Pinned MusicBee API mirror
    ├── EmbeddedResolver.cs       # Resolves embedded SharpDX assemblies
    ├── Plugin.Lifecycle.cs       # Initialise, Close, Uninstall, PluginInfo metadata
    ├── Plugin.Commands.cs        # MusicBee menu commands & hotkey hooks
    ├── Plugin.Config.cs          # Configure dialog hook
    ├── Plugin.UiPanel.cs         # Dockable panel provider for MusicBee
    ├── Audio/
    │   ├── AudioTap.cs           # BASS stream interceptor
    │   ├── AudioEngine.cs        # Main audio dispatcher
    │   ├── AudioEngine.Analysis.cs # FFT processing & frequency binning
    │   ├── FoobarAnalyzer.cs     # Gaussian FFT windowing & dynamic dB scaling
    │   └── PreciseTimer.cs       # High-precision multimedia audio loop timer
    ├── Render/
    │   ├── VisualBeeControl.cs   # WinForms host control with HUD overlay
    │   ├── VisualBeeControl.Menu.cs # Right-click context menu & sync
    │   ├── VisualBeeControl.RenderLoop.cs # Threaded render loop & pacing
    │   ├── BarDynamics.cs        # Independent attack/decay physics & gravity peak drop
    │   └── D3D/
    │       ├── D3DRenderer.cs    # D3D11 device, swapchain, context management
    │       ├── D3DRenderer.Frame.cs # Constant buffer dispatch & drawing
    │       ├── D3DRenderer.Pipeline.cs # Target textures, shader compilation, samplers
    │       ├── D3DShaders.cs     # Atmosphere, Bokeh particles & Bloom shaders
    │       ├── D3DShaders.Bars.cs# Glass pillars, VFD ladders & center laser HLSL
    │       ├── D3DShaders.Ribbon.cs # Liquid Neon oscilloscope waveform HLSL
    │       ├── FramePacer.cs     # Waitable object pacer with Win32 CloseHandle
    │       └── DisplayInfo.cs    # Display refresh rate detection
    └── Settings/
        ├── VisualBeeSettings.cs  # In-memory settings state model
        ├── VisualBeeSettings.Io.cs # Debounced file save & migration
        ├── VisualBeeSettingsForm.cs # 4-smart-tab dialog & 1-click vibes
        ├── VisualBeeSettingsForm.Pages.*.cs # Modular settings pages (Studio, Theme, Audio, System)
        └── Ui/                   # VbSlider, VbToggle, VbSegmented, VbSwatch
```

---

## 4. How to Build, Test, and Deploy

### 1. Build Standalone Solution
```powershell
cd C:\Users\teodo\Documents\LLM_PROJECTS\VisualBee
dotnet build VisualBee.sln -c Release
```
Output: `release/mb_VisualBee.dll` (~1.28 MB)

### 2. Build Workspace Project
```powershell
dotnet build projects/VisualBee/VisualBee.csproj -warnaserror
```

### 3. Deploy to MusicBee
```powershell
python tools/mb_plugin.py deploy VisualBee --to-user --confirm-user-host --force
```

### 4. Run Verification Suite & Gates
```powershell
# Plugin offline test (GDI/USER leak assertions and 20 lifecycle mounts)
python tools/mb_plugin.py test VisualBee

# Full repository verification suite
python tools/verify.py --gates
```
