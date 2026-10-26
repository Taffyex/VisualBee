# VisualBee v1.1.0 — Architecture & Handoff Reference

**Target:** MusicBee 3.4 - 3.7+ (WinForms / x86 / .NET Framework 4.8)  
**Public Repository:** [https://github.com/Taffyex/VisualBee](https://github.com/Taffyex/VisualBee)  
**Local Codebases:** 
- Workspace project: `projects/VisualBee` (within `dev_tools/reverseEngineerAnything`)
- Standalone repository: `C:\Users\teodo\Documents\LLM_PROJECTS\VisualBee`
**Deployed Host Path:** `%APPDATA%\MusicBee\Plugins\mb_VisualBee.dll`

---

## 1. Current State & Release Summary (v1.1.0)
- **Version:** `v1.1.0` (Studio VFD Rack & Glass Pillar Release)
- **Standalone Binary:** `release/mb_VisualBee.dll` (~1.23 MB, x86, net48, zero external dependencies).
- **Release Archive:** `release/VisualBee_v1.0.zip`
- **Embedded Assemblies:** SharpDX, SharpDX.Direct3D11, SharpDX.DXGI, SharpDX.D3DCompiler, SharpDX.Mathematics.
- **Presentation:** DXGI Flip-Model (`DXGI_SWAP_EFFECT_FLIP_DISCARD`) with waitable swapchain latency objects (`DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT`).
- **Pacing:** Silky smooth delivery at 60 Hz, 120 Hz, 144 Hz, 240 Hz, or uncapped.
- **Audio Engine:** 64-bit BASS stream tap with sub-millisecond audio pacing via `PreciseTimer` and Gaussian FFT.

---

## 2. Key Features & Visual Pipeline (v1.1.0)

### A. Glass Pillar Enclosures & Ghosted Rungs (The "Unlit State")
- **Persistent Full-Height Glass Enclosures**: Each frequency bar is framed by a permanent translucent glass capsule with a rounded dome top extending from ground to display ceiling (`91%` height).
- **Ghosted Unlit Ladder Rungs**: In the space between the active volume ($hPx$) and the peak envelope ($peakPx$), faint, translucent horizontal rungs glow with delicate cyan/slate VFD phosphor filaments.
- **Architectural Ladder Geometry**: All segmented ticks across every bar snap to identical horizontal scanline grid lines across the entire display.
- **Floating Peak Hold Needles**: A razor-thin, floating horizontal peak cap rests cleanly at the summit of the unlit ladder.

### B. Amplitude-Driven HDR Incandescence ("White-Hot Overdrive")
- **Energy Coupling**: Rather than mapping color by spatial height alone, color core is coupled to instantaneous signal wattage ($rawH$).
- **Thermal Ramp**:
  - Low/Mid amplitude ($< 55\%$): Deep saturated cobalt and electric cyan/turquoise (no white blowout).
  - High amplitude ($> 60\text{--}75\%$): Surges into HDR overdrive ($> 1.0$ linear luminance), ramping the center core to incandescent white (`float3(1.70, 1.70, 1.70)`).
  - Electric Corona: ACES tone mapping cleanly preserves the white center core while the two-pass Gaussian bloom filter spreads a wide electric cyan aura around the outer edges.

### C. Studio Decibel Reference Grid Lines
- 5 ultra-fine horizontal calibration lines spanning the background at $-24\text{ dB}$, $-18\text{ dB}$, $-12\text{ dB}$, $-6\text{ dB}$, and $0\text{ dB}$.
- Configurable toggle and opacity slider (2% to 40%, default 12%).

### D. Wet Lacquer / Studio Glass Horizon Reflection
- Glowing ground horizon datum line anchors the meters to the desk surface.
- Downward reflection attenuated with realistic exponential vertical falloff (`exp(-dist * 11.0)`) and dark navy lacquer absorption tint (`float3(0.08, 0.20, 0.42)`).

### E. 1-Click Studio VFD Setup
- Added a dedicated setup button in Settings under the **Bars** page:
  `⚡ Apply Studio VFD Rack Preset (1-Click Setup)`
  Instantly selects Grounded mode, 28-bar architectural layout, `VFD Studio Glass` cyan palette, wet lacquer reflection, and studio grid lines.

---

## 3. Directory Map

```
VisualBee/
├── VisualBee.sln                 # Standalone solution
├── release/                      # Precompiled binary & zip
│   ├── mb_VisualBee.dll
│   └── VisualBee_v1.0.zip
├── screenshots/                  # Clean captures of bars & settings
└── src/VisualBee/
    ├── VisualBee.csproj          # SDK-style x86 .csproj with embedded SharpDX target
    ├── MusicBeeInterface.cs      # Pinned MusicBee API interop mirror
    ├── EmbeddedResolver.cs       # Resolves embedded SharpDX DLLs from assembly resources
    ├── Plugin.Lifecycle.cs       # Initialise, Close, Uninstall, PluginInfo metadata
    ├── Plugin.Commands.cs        # MusicBee menu commands & hotkey hooks
    ├── Plugin.Config.cs          # Configure dialog hook
    ├── Plugin.UiPanel.cs         # Dockable panel provider for MusicBee
    ├── Audio/
    │   ├── AudioTap.cs           # BASS stream interceptor
    │   ├── AudioEngine.cs        # Main audio dispatcher
    │   ├── AudioEngine.Analysis.cs# FFT processing & frequency binning
    │   ├── FoobarAnalyzer.cs     # Gaussian FFT windowing & dynamic dB scaling
    │   └── PreciseTimer.cs       # High-precision multimedia audio loop timer
    ├── Render/
    │   ├── VisualBeeControl.cs   # WinForms host control with render thread loop
    │   ├── VisualBeeControl.Menu.cs # Context menu & quick toggles
    │   ├── BarDynamics.cs        # Independent attack/decay physics & gravity peak drop
    │   └── D3D/
    │       ├── D3DRenderer.cs    # D3D11 device, swapchain, context management
    │       ├── D3DRenderer.Frame.cs# Per-frame constant buffer dispatch & draw calls
    │       ├── D3DRenderer.Pipeline.cs# Target textures, shader compilation, samplers
    │       ├── D3DShaders.cs     # Constant buffers & Bloom post-process shaders
    │       ├── D3DShaders.Bars.cs# High-performance HLSL shader for VFD spectrum bars
    │       ├── D3DShaders.Ribbon.cs# Liquid Neon oscilloscope waveform shader
    │       ├── FramePacer.cs     # Waitable object pacer with Win32 CloseHandle
    │       └── DisplayInfo.cs    # Display refresh rate detection
    └── Settings/
        ├── VisualBeeSettings.cs  # In-memory settings state model
        ├── VisualBeeSettings.Io.cs# Debounced file save (400ms timer) & migration
        ├── VisualBeeSettingsForm.cs # Modeless, non-blocking 780x600 settings dialog
        ├── VisualBeeSettingsForm.Pages.*.cs # Modular tab pages (Bars, Colours, Peaks, Effects, etc.)
        └── Ui/                   # Custom owner-drawn dark theme UI controls
```

---

## 4. How to Build, Test, and Deploy

### 1. Build Standalone Solution
```bash
cd C:\Users\teodo\Documents\LLM_PROJECTS\VisualBee
dotnet build VisualBee.sln -c Release
```
Output: `release/mb_VisualBee.dll`

### 2. Build Workspace Project
```bash
dotnet build projects/VisualBee/VisualBee.csproj -warnaserror
```

### 3. Deploy to MusicBee
```bash
python tools/mb_plugin.py deploy VisualBee --to-user --confirm-user-host --force
```

### 4. Run Verification Suite & Gates
```bash
python tools/mb_plugin.py test VisualBee
python tools/verify.py --gates
```
