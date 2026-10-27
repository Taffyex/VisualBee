# Changelog

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

#### Dark Studio Settings & Themes
- **Modern Dark Settings Studio**: 4 modular tabs (Studio, Theme, Audio, System) with smooth interactive sliders, toggles, segmented buttons, and colour wells.
- **HUD Flyout & Context Menu**: Seamless on-screen display controls and quick hotkey access.

---

## [1.1.0] - 2026-10-26

### Added: Studio VFD Rack & Glass Pillar Pipeline
- **Glass Pillar Enclosures & Ghosted Rungs**: Translucent rounded-pill glass capsules framing each frequency band, with unlit translucent ladder rungs extending up to the peak envelope.
- **Amplitude-Driven HDR Incandescence ("White-Hot Overdrive")**: Signal energy coupling where bars surging into peak amplitude ramp to an incandescent white core (`float3(1.65, 1.65, 1.65)`), generating an electric cyan corona through ACES tone mapping and bloom.
- **Studio Decibel Grid Lines**: 5 ultra-fine reference datum lines at -24 dB, -18 dB, -12 dB, -6 dB, 0 dB with adjustable opacity.
- **Wet Lacquer Horizon Reflection**: Glowing ground horizon datum line anchoring meters to a reflective desk, featuring exponential falloff attenuation and dark navy lacquer absorption tint.
- **Architectural Ladder Geometry**: Uniform horizontal ladder rung alignment across all bands with fine segment height/gap controls.
- **VFD Studio Glass Theme**: Signature McIntosh / VFD cyan-cobalt preset.

---

## [1.0.0] - 2026-10-26

### Initial Release
- Hardware-accelerated DirectX 11 rendering with support for 60 to 240+ fps.
- 3 bar shapes: Solid bars, Rounded pills, and LED segmented blocks.
- Real-time physics controls: independent attack/decay times, gravity drop rate, and peak hold times.
- Multiple color modes: vertical gradient, frequency spread, solid tips, and theme-aware hue cycling.
- Modeless dark settings window cleanly organized into Mode-Specific and Universal tabs.
- Floor reflection and edge vignette effects.
- Single self-contained `mb_VisualBee.dll` with embedded DirectX runtimes.
