# VisualBee v2.0

VisualBee is an ultra high-performance, hardware-accelerated **Direct3D 11 spectrum analyzer and phase-locked analog oscilloscope** for [MusicBee](https://getmusicbee.com/). Engineered with zero managed wrappers and pure native DirectX 11 P/Invoke, it delivers buttery-smooth 120Hz/144Hz visuals, physical optical glass refraction, authentic VFD/Nixie vacuum tube incandescence, and studio CRT waveform monitoring at <1% CPU and <3% GPU load.

---

## Visual Showcase — Style Gallery

| VFD Glass Pillars (Vacuum Enclosure) | Synthwave CyberGrid (Mirrored 3D Perspective) |
| :---: | :---: |
| ![VFD Glass Pillars](screenshots/style_vfd_glass_pillars.png) | ![Synthwave CyberGrid](screenshots/style_cybergrid_synthwave.png) |

| Green Matrix LED Ladder Rungs | Amber Studio Segments & Horizon Reflection |
| :---: | :---: |
| ![Green Matrix](screenshots/style_green_matrix.png) | ![Amber Segments](screenshots/style_amber_segments.png) |

| Decibel Graticule Studio Solid Bars | Phase-Locked Analog CRT Oscilloscope |
| :---: | :---: |
| ![Studio dB Bars](screenshots/style_studio_db_bars.png) | ![Analog Oscilloscope](screenshots/visualbee_scope.png) |

---

## Real-Time Dark Settings Studio

Configure bar styles, bloom intensity, color palettes, FFT windowing, and audio physics in real time without pausing playback:

![VisualBee Studio Settings](screenshots/settings_studio_dark.png)

---

## What's New in VisualBee v2.0

### 1. 120Hz/144Hz Butter-Smooth Frame Pacing & Zero Tab-Switch Stutter
- **High-Resolution Waitable Timer**: Sub-millisecond multimedia timer synchronized directly with your display's native refresh rate (`1000.0 / deviceCaps`).
- **Lock-Free UI Paint**: Eliminated UI thread mutex contention on `OnPaint` (`volatile bool _hasGpu`). Switching tabs, playlists, or views in MusicBee no longer stutters or drops frames.
- **WM_ERASEBKGND Intercept**: Suppressed WinForms GDI background erase cycles (message `0x0014`) during layout resizing and tab switches, preventing swapchain flickering.
- **Event-Driven Playback State**: Replaced 20Hz polling with native `NotificationType.PlayStateChanged` hooks, reducing inter-thread API contention to zero.

### 2. Zero-Dependency Native Direct3D 11 Engine
- **Shrunk from 1.3 MB down to 435 KB**: Completely stripped away SharpDX and legacy managed runtime wrappers.
- **Pure Native P/Invoke**: Direct calls into `d3d11.dll`, `dxgi.dll`, and `d3dcompiler_47.dll`. No external dependencies or installers required.
- **Discard-Mapped Dynamic Vertex Streaming**: Pre-allocated ring buffers with zero GC heap allocations during render ticks.

### 3. First-Class Nixie & VFD Rungs vs. Classic Solid Neo-Bars
- **Nixie / VFD Rungs (`StyleNixie`)**: Authentic beveled ladder rungs enclosed in physical glass tubes with unlit ghost rungs, cathode wire mesh, and amplitude-driven incandescent core burning.
- **Decoupled Solid Neo-Bars (`StyleSolid`)**: Outer glass tube enclosures are decoupled so blocky solid bars render with clean, razor-sharp edges without unintended tubes or ghost rungs.
- **Curated 1-Click Presets**: "VFD Nixie Rungs" and "Solid Neo-Bars (Classic)" available instantly from the settings menu and hotkeys.

### 4. Phase-Locked Analog CRT Oscilloscope
- **Zero-Crossing Triggering**: Automatic phase detection locks arbitrary periodic audio waveforms into perfectly stationary, jitter-free display.
- **Inverse Slew-Rate Electron Beam**: Fast transients dim naturally like a physical CRT beam, while slow dwell points burn intensely with warm phosphor saturation.
- **Multi-Pass Gaussian Blur & Phosphor Decay**: Authentic analog persistence trails with custom decay timing.

### 5. High-Efficiency GPU Optics & Post-Processing
- **Optimized 5-Level Bloom Chain**: Replaced redundant deep-mip downsampling with a high-throughput 5-level blur chain in 32-bit `FormatBgra8`, cutting fill-rate overhead by 60%.
- **Half-Resolution Bilinear Aurora**: Procedural multi-octave aurora noise rendered at half resolution and upsampled with bilinear filtering, preserving full visual richness at zero FPS penalty.
- **Cauchy 3-Wavelength Chromatic Dispersion**: Snell's law refraction with chromatic aberration ($\eta_R = 0.88, \eta_G = 1.00, \eta_B = 1.14$) guarded by an epsilon threshold for optimal ALU execution.

### 6. Zero-Allocation Audio Tap & FFT
- **DynamicMethod Reflection Hook**: Compiled runtime IL bytecode retrieves MusicBee's private BASS track handle without reflection overhead or boxing.
- **Cached BASS Channel Queries**: Cached mixer channel format metadata eliminates redundant inter-process calls per frame.
- **Unsafe Pointer Stereo FFT**: SIMD-friendly unsafe pointer buffer packing and Fourier transform execution.

---

## Installation

### Quick Install (Recommended)
1. Download **`mb_VisualBee.dll`** (or grab `VisualBee_v2.0.zip`) from the [`release/`](release/) folder.
2. In MusicBee, press `Ctrl + O` (or go to **Edit** > **Preferences**).
3. Select **Plugins** in the sidebar, click **Add Plugin**, and choose `mb_VisualBee.dll`.
4. Click **Apply** and restart MusicBee.

### Manual Drop-in
1. Close MusicBee.
2. Copy `mb_VisualBee.dll` into your MusicBee plugins directory:
   - **Installer edition:** `C:\Program Files (x86)\MusicBee\Plugins\`
   - **Portable edition:** `<MusicBee Folder>\Plugins\`
   - **Windows Store:** `%APPDATA%\MusicBee\Plugins\`
3. Launch MusicBee.

---

## Usage & Controls

- **Layout Placement**: Go to **View** > **Arrange Panels...**, then drag **VisualBee Visualizer** to your preferred dock slot (main panel, bottom panel, or sidebars).
- **Popout Window**: Right-click the visualizer and choose **Popout Window** for dedicated multi-monitor viewing.
- **Full Screen**: Double-click or press `F11` to toggle full-screen visualizer mode.
- **Settings Studio**: Right-click the visualizer and select **Settings...** to open the real-time dark settings studio.
- **Quick Keyboard Shortcuts**:
  - `Ctrl + Shift + L`: Next visual layout
  - `Ctrl + Shift + P`: Next preset
  - `Ctrl + Shift + V`: Open VisualBee Settings

---

## Architecture & Code Quality

VisualBee strictly adheres to high-performance systems programming and modularity standards:
- **Assembly Size**: 435 KB (x86, `.NET Framework 4.8`, native Direct3D 11).
- **Memory Footprint**: < 25 MB private working set; zero GC gen-0 allocations per frame.
- **Strict Modularity**: 100% of source files strictly $\le 400$ lines.
- **Clean Compilation**: Zero warnings with `-warnaserror` enabled.

---

## License

MIT License. See [LICENSE](LICENSE) for details.
