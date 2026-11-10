# Changelog

## [2.2.31] - 2026-11-10

### Low-Latency WASAPI Loopback, Level Matching (AGC) & Process Loopback Overhaul

#### Surgical Audio Engine & DSP Upgrades
- **Zero-Injection Dropout Elimination**: Removed destructive 128-zero frame injections during stream gaps and replaced with continuous exponential idle decay (`AdvanceIdleDecay`), removing audio pops and visual stutter.
- **Sub-Packet 144Hz–240Hz Playhead Interpolation**: Replaced static 25ms timer sleeps with high-resolution playhead interpolation, delivering buttery smooth 144Hz/240Hz visual updates without buffer underruns.
- **Automatic Gain Level Matching (AGC)**: Post-fader system audio is often attenuated by 15–25 dB compared to pre-fader BASS signals. Added an intelligent slow-release AGC level matcher (`LoopbackAgc = true`) so WASAPI loopback streams fill the frequency bars with identical punch and dynamic range as direct playback.
- **Asymmetric Causal Windowing**: Fixed the 85–340 ms visual lag inherent in standard symmetric Hann/Blackman-Harris windows. Causal window mode shifts the peak weight to $0.82(N-1)$ directly at the live playhead, aligning visual transients instantly with acoustic playback.
- **Multichannel Downmixing**: Implemented ITU-R BS.775 downmix matrices for stereo, quadraphonic, 5.1 surround, and 7.1 surround for 32-bit float, 32-bit int, 24-bit int, and 16-bit PCM.
- **Windows 10/11 Process Loopback Activation Fix**: Fixed COM marshaling for `AUDIOCLIENT_ACTIVATION_PARAMS` by wrapping it inside `PROPVARIANT` (`VT_BLOB`) and implementing `IAgileObject` on `ProcessLoopbackHandler`, enabling per-process isolation (`MusicBee process (isolated)`) without crashes.
- **Real-Time Oscilloscope Playhead Tracking**: Oscilloscope triggering now inspects the newest sample window near the live playhead instead of the oldest sample in the buffer, eliminating 340ms waveform latency.
- **UI Device Selection Auto-Switching**: Selecting any non-default endpoint or process isolation in the context menu or Settings window immediately activates WASAPI loopback capture.

## [2.1.3] - 2026-11-10

### Audio Device Selection & Low-Latency Event-Driven WASAPI Loopback

#### Dynamic Audio Endpoint Selection
- **Output Device Selector**: Choose any active playback endpoint (Speakers, Headphones, HDMI, USB DAC) or virtual audio routing cable (VB-CABLE Input, Voicemeeter, Virtual Audio Router).
- **Hot-Plug & Dynamic Re-Enumeration**: Scans and populates available endpoints whenever the device dropdown opens, allowing on-the-fly switching without restarting MusicBee.
- **Context Menu Routing**: Right-click context submenus to quickly toggle between internal BASS tapping and WASAPI loopback, or switch the capture device on the fly.

#### Windows 10/11 Process-Isolated Loopback
- **Targeted Application Audio**: Uses CoreAudio process activation (`VAD\Process_Loopback`) on Windows 10 build 20348+ and Windows 11 to isolate and capture audio strictly from MusicBee and secondary media players without recording VoIP communications (Discord, Teams), game audio, or system alerts.

#### Engine Optimization & Telemetry
- **Event-Driven Capture Engine**: Upgraded WASAPI capture to `AUDCLNT_STREAMFLAGS_EVENTCALLBACK` with a 50 ms ring buffer, automatic PCM channel/sample rate negotiation, and smooth idle decay when audio stops.
- **Audio Sync Delay Compensation**: Added 0 ms to 500 ms audio sync delay slider in settings to align visual feedback with high-latency Bluetooth headphones or external DAC DSP buffering.
- **Status HUD & Diagnostics Tooltip**: Live display and hover tooltip showing active endpoint ID, sample rate, channel format, and capture state.

## [2.1.2] - 2026-11-10

### Bug Fixes & UX Polish

#### Settings Window & Search (#4, #3)
- **Invisible Search Results Fixed (#4)**: Called `FitWidth` on `RackSectionCard` containers in search results layout so matching setting rows are correctly sized, positioned, and rendered.
- **Dynamic Section Card Visibility (#3)**: Checked `SettingRow.IsRowVisible` instead of ancestor-masked `row.Visible` so conditional sections (such as `Album art`, `Aurora`, `LED / VFD segments`, `Radial`, etc.) unhide immediately when dependencies switch without requiring page navigation.
- **Reset Link Placement**: Fixed section `Reset section` link positioning during initialization before form display.
- **Responsive Label Scaling & 4-Choice Dropdowns**: Scaled label widths responsively in 2-column cards and constrained `Segmented` widgets to <=3 choices to prevent text truncation and control overflow.
- **Audio Page Hint**: Fixed hint banner for the Audio page.

#### Background Optics & Skin Transparency (#2)
- **Transparent Background Option (#2)**: Added `Transparent` mode under `Optics & FX` -> `Background` that dynamically resolves the active MusicBee skin background color and gradients directly from the docked container hierarchy, seamlessly blending the visualizer into custom MusicBee skins.
- **Dynamic Skin Change Tracking**: Automatically invalidates and repaints the background texture when MusicBee skin colors change at runtime.

#### Presets & Docking Self-Healing
- **Cover Art Chameleon Preset**: Corrected preset keys to properly enable adaptive cover colors (`anim.artColours`), cover blur (`art.blur`), and opacity (`art.opacity`).
- **Obsidian Capsules Preset**: Corrected peak gravity fall token mapping.
- **Restored Dock Self-Healing & Metrics**: Re-hooked `HealPanelMetrics`, `DisableHostScrollOverhang`, and `SyncViewBounds` to eliminate the +17px scrollbar overhang and protect against negative panel height loops.
- **Preset Path Migration**: Migrated preset directory to `MusicBee\VisualBee\Presets` with automatic fallback/migration from legacy paths.

## [2.1.1-beta] - 2026-10-29

### Browser2 (YouTube Music, SoundCloud) & Windows System Audio Support

#### Native WASAPI Loopback Capture Engine
- **Pure COM WASAPI Loopback**: Integrated native WASAPI loopback capture (`AUDCLNT_STREAMFLAGS_LOOPBACK`) with zero external dependencies. Taps into the Windows default multimedia render endpoint to visualize audio played through the [Browser2](https://github.com/tumuyan/mb_browser2) plugin (YouTube Music, SoundCloud, YouTube videos, streams) as well as any other system audio.
- **Auto Source Fallback**: Defaults to **Auto (Direct BASS + API + Loopback Fallback)**. Automatically hooks MusicBee's direct BASS stream during local track playback and seamlessly falls back to loopback when MusicBee is idle or paused and Browser2 audio plays.
- **Audio Capture Hook Selector**: Added user-selectable capture modes in Settings -> Audio -> Audio Engine:
  1. *Auto (Direct BASS + API + Loopback Fallback) [Default]*
  2. *Always MusicBee API (Universal Safe)*
  3. *Direct BASS Stream Only*
  4. *WASAPI Loopback (Browser & System Audio)*
  5. *MusicBee Player Only (Auto BASS + API)*

#### Concurrency, Lifecycle & Power Optimization
- **Per-View Independent Read Heads**: Eliminates frame drops and stuttering when Full Screen is open concurrently with the docked panel by decoupling reader state across views.
- **Zero Idle GPU & CPU Drain**: Smoothly settles and sleeps to 0% GPU / 0% CPU when audio is stopped or paused in both spectrum bar and oscilloscope layouts.
- **Clean COM RCWs & Device Invalidation**: Full COM RCW cleanup and automatic reconnection if audio endpoints change, disconnect, or reset.


### VisualBee Studio Rack, Panel Docking Self-Healing & Distinct Tube Optics

#### Critical Panel Docking & Metric Self-Healing
- **Corrupted Panel Metric Self-Healing (`HealPanelMetrics`)**: Fixed the severe MusicBee bug where embedding a plugin in left/right sidebars or top/bottom panels caused negative height loops (`<= -32767` / `65734px`) that locked the panel into fullscreen and persisted in `MusicBee3Settings.ini` even after DLL removal. VisualBee now inspects and repairs corrupted panel metrics in memory and on disk to pristine resizable dimensions.
- **Scrollbar Overhang Elimination (`DisableHostScrollOverhang` & `SyncViewBounds`)**: Stripped unwanted 17px rightward overhang by disabling MusicBee's internal vertical scrollbar flags via reflection and clipping the DirectX swapchain directly to the visible dock container region.
- **Fullscreen Non-Intrusive Focus**: Changed fullscreen visualizer window to `TopMost = false`, allowing seamless Alt+Tab multitasking and clean toggle via double-click, `F11`, or `Escape`.

#### True Visual Separation: Glass Pillars vs. Nixie Rungs
- **Pure Refractive Glass Pillars (`StyleGlass`)**: Refactored into a crystal cylinder column with Fresnel edge reflections, Snell's law chromatic dispersion, and fluid core glow with zero ladder rungs.
- **Authentic Nixie & VFD Rungs (`StyleNixie`)**: Re-engineered with dual metallic anode guide wires, laser-cut filament ladder rungs, hexagonal cathode wire mesh, and smoked bronze vacuum envelopes.
- **GlassFill Optics Selector**: Added 4 distinct tube fill modes: `Pure Crystal Glass`, `Bioluminescent Plasma`, `Glowing Filaments`, and `Stepped Ladder Rungs`.

#### Original VisualBee Studio Rack Interface
- **Top Studio Deck Navigation**: 5 clean horizontal tabs (`Presets`, `Studio`, `Optics & FX`, `Colours`, `Audio`) replacing legacy foobar2000 dialog styles.
- **Persistent Quick-Switch Ribbon**: Always-visible top ribbon featuring a 1-click preset selector, active palette chips, and live FPS / sample rate telemetry.
- **2-Column Hardware Rack Architecture**: Responsive gunmetal cards with warm amber indicator lights, clear grouping, and individual section reset buttons.
- **Dual Tooltip Guidance**: Plain-English hover cards explaining exact acoustic physics and shader parameters (Attack/Decay time constants, Tilt, Glass Dispersion, Bloom, Floor Lacquer).

#### 12 Hand-Crafted Studio Presets & 10 Color Palettes
- **100% Original Presets**: Purged all legacy presets and introduced 12 original factory presets:
  - *Dieter Studio LED* (Braun/Rams minimalist reference meters)
  - *Nixie Amber Tube* (Warm incandescent vacuum filaments)
  - *Borosilicate VFD* (Refractive cyan-teal laboratory tube)
  - *Tektronix P31 Scope* (Phase-locked green phosphor CRT waveform monitor)
  - *Quartz Prism 3D* (Snell's dispersion chromatic crystal columns)
  - *Cyberpunk 2099* (Hot magenta & cyan neon overdrive)
  - *Matrix Phosphor* (Monochrome green terminal cascade)
  - *Liquid Mercury* (High-contrast metallic chrome reflection)
  - *Solar Flare* (Thermonuclear gold & deep orange incandescence)
  - *Cryo Frost* (Sub-zero ice blue pillars with horizon mist)
  - *Dark Matter* (Deep obsidian stealth meters with violet tips)
  - *Acid Techno* (High-energy electric lime strobe bars)
- **10 Curated Studio Palettes**: *Cryo Cyan*, *Nixie Amber*, *Ultraviolet*, *Emerald CRT*, *Solar Flare*, *Deep Ocean*, *Rose Quartz*, *Gold Reference*, *Titanium Mono*, and *Acid Lime*.

---

## [2.0.0] - 2026-10-27

### The Real V2.0: Direct3D 11 Studio & Nixie Overhaul

#### 120Hz/144Hz Butter-Smooth Frame Pacing & Tab-Switch Stutter Elimination
- **High-Resolution Waitable Timer**: Sub-millisecond multimedia timer pacing render ticks to the display's exact refresh rate (`1000.0 / deviceCaps`).
- **Lock-Free UI Paint**: Replaced `lock (_sync)` in WinForms `OnPaint` with `volatile bool _hasGpu`, completely eliminating UI thread preemption when switching tabs or browsing libraries.
- **WM_ERASEBKGND Intercept**: Suppressed WinForms GDI background erase cycles (message `0x0014`) during layout resizing and tab switches, preventing swapchain flickering.
- **Event-Driven Playback State**: Replaced 20Hz polling with native `NotificationType.PlayStateChanged` hooks, reducing cross-thread lock stalls to zero.
- **Delta-Time Smoothing**: Velocity delta-time smoothing avoids sudden physics spikes across presentation intervals.

#### Zero-Dependency Native Direct3D 11 Engine
- **Lightweight 435 KB Footprint**: Eliminated SharpDX runtime assemblies, cutting DLL size from 1.3 MB down to 435 KB.
- **Pure Native P/Invoke**: Direct low-overhead calls to `d3d11.dll`, `dxgi.dll`, and `d3dcompiler_47.dll`.
- **Immediate-Mode Canvas**: High-performance discard-mapped 2D/3D quad & polyline rendering with zero GC allocations during frame execution.

#### First-Class Nixie Style & Decoupled Neo-Bars
- **First-Class Nixie / VFD Rungs (`StyleNixie`)**: Dedicated vacuum tube mode featuring beveled filament rungs, unlit ghost rungs, cathode wire mesh, and amplitude-driven white-hot incandescence.
- **Decoupled Solid Neo-Bars (`StyleSolid`)**: Glass pillar tube enclosures decoupled so blocky solid bars render cleanly with crisp edges and zero ghost artifacts.
- **Curated Presets**: Added "VFD Nixie Rungs" and "Solid Neo-Bars (Classic)" to the preset collection.

#### Phase-Locked Analog CRT Oscilloscope
- **Zero-Crossing Triggering**: Automatic phase detection locks arbitrary periodic audio waveforms into perfectly stationary, jitter-free display.
- **Inverse Slew-Rate Electron Beam**: Fast transients dim naturally like a physical CRT beam, while slow dwell points burn intensely with warm phosphor saturation.
- **Multi-Pass Gaussian Blur & Phosphor Decay**: Authentic analog persistence trails with custom decay timing.

#### High-Efficiency GPU Optics & Post-Processing
- **Optimized 5-Level Bloom Chain**: Replaced redundant deep-mip downsampling with a high-throughput 5-level blur chain in 32-bit `FormatBgra8`, cutting fill-rate overhead by 60%.
- **Half-Resolution Bilinear Aurora**: Procedural multi-octave aurora noise rendered at half resolution and upsampled with bilinear filtering, preserving full visual richness at zero FPS penalty.
- **Cauchy 3-Wavelength Chromatic Dispersion**: Snell's law refraction with chromatic aberration ($\eta_R = 0.88, \eta_G = 1.00, \eta_B = 1.14$) guarded by an epsilon threshold for optimal ALU execution.

#### Zero-Allocation Audio Tap & FFT
- **DynamicMethod Reflection Hook**: Compiled runtime IL bytecode retrieves MusicBee's private BASS track handle without reflection overhead or boxing.
- **Cached BASS Channel Queries**: Cached mixer channel format metadata eliminates redundant inter-process calls per frame.
- **Unsafe Pointer Stereo FFT**: SIMD-friendly unsafe pointer buffer packing and Fourier transform execution.

---

## [1.1.0] - 2026-10-26

### Added: Studio VFD Rack & Glass Pillar Pipeline
- **Glass Pillar Enclosures & Ghosted Rungs**: Translucent rounded-pill glass capsules framing each frequency band.
- **Amplitude-Driven HDR Incandescence ("White-Hot Overdrive")**: Signal energy coupling where bars surging into peak amplitude ramp to an incandescent white core (`float3(1.65, 1.65, 1.65)`).
- **Studio Decibel Grid Lines**: 5 ultra-fine reference datum lines at -24 dB, -18 dB, -12 dB, -6 dB, 0 dB with adjustable opacity.
- **Wet Lacquer Horizon Reflection**: Glowing ground horizon datum line anchoring meters to a reflective desk.

---

## [1.0.0] - 2026-10-26

### Initial Release
- Hardware-accelerated DirectX 11 rendering with support for 60 to 240+ fps.
- 3 bar shapes: Solid bars, Rounded pills, and LED segmented blocks.
- Real-time physics controls: independent attack/decay times, gravity drop rate, and peak hold times.
