# VisualBee v2.1.1

VisualBee is an ultra high-performance, hardware-accelerated **Direct3D 11 spectrum analyzer and phase-locked analog oscilloscope** for [MusicBee](https://getmusicbee.com/). Engineered with zero managed wrappers and pure native DirectX 11 P/Invoke, it delivers buttery-smooth 120Hz/144Hz visuals, physical optical glass refraction, authentic VFD/Nixie vacuum tube incandescence, and studio CRT waveform monitoring at <1% CPU and <3% GPU load.

---

## Installation

> [!IMPORTANT]
> **Downloading from GitHub:** Always download **[`VisualBee_v2.1.1.zip`](https://github.com/Taffyex/VisualBee/releases/download/v2.1.1/VisualBee_v2.1.1.zip)** or raw **`mb_VisualBee.dll`** directly from the official **[GitHub Releases](https://github.com/Taffyex/VisualBee/releases)** page (under the "Assets" dropdown).
> **DO NOT** right-click `mb_VisualBee.dll` in the GitHub file repository view and click *"Save link as..."*! GitHub will save the GitHub HTML webpage instead of the actual compiled DLL, causing MusicBee to display the error:  
> `Unable to install plugin: mb_VisualBee.dll, Common language runtime detected an invalid program`.

### 🚀 Quick Install (Recommended)
1. Download **[`VisualBee_v2.1.1.zip`](https://github.com/Taffyex/VisualBee/releases/download/v2.1.1/VisualBee_v2.1.1.zip)** (or raw **[`mb_VisualBee.dll`](https://github.com/Taffyex/VisualBee/releases/download/v2.1.1/mb_VisualBee.dll)**) from the [latest release](https://github.com/Taffyex/VisualBee/releases).
2. In MusicBee, press `Ctrl + O` (or go to **Edit** > **Preferences**).
3. Select **Plugins** in the sidebar, click **Add Plugin**, and choose the downloaded file (`VisualBee_v2.1.1.zip` or `mb_VisualBee.dll`).
4. Click **Apply** and restart MusicBee.

### 📁 Manual Drop-in
1. Close MusicBee.
2. Copy `mb_VisualBee.dll` into your MusicBee plugins directory:
   - **Installer edition:** `C:\Program Files (x86)\MusicBee\Plugins\`
   - **Portable edition:** `<MusicBee Folder>\Plugins\`
   - **Windows Store:** `%APPDATA%\MusicBee\Plugins\`
3. Launch MusicBee.

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

## Real-Time VisualBee Studio Rack

Configure presets, bar optics, bloom intensity, color palettes, and audio acoustics in real time with the new modular Studio Rack interface:

![VisualBee Studio Settings](screenshots/settings_studio_dark.png)

- **Top Studio Deck Navigation**: 5 horizontal tabs (`Presets`, `Studio`, `Optics & FX`, `Colours`, `Audio`).
- **Persistent Quick-Switch Ribbon**: Instant 1-click preset switching, active palette chips, and live FPS / sample-rate telemetry.
- **Hardware Rack Architecture**: Responsive 2-column cards with warm amber indicator lights and per-section resets.
- **Dual Tooltip Guidance**: Plain-English hover cards explaining exact acoustic physics and shader parameters.

---

## What's New in VisualBee v2.1.1

### 1. Robust Audio Engine & Startup Hook Binding
- **Direct BASS Audio Hook Fix**: Eliminated the startup race condition where Direct BASS audio capture remained unbound until manual pause/unpause. Audio capture now automatically and lazily binds on the very first audio frame rendered and on player state changes.
- **Resilient Method Handle Generation**: Replaced dynamic IL opcode generation with strongly typed, CLR-verified Expression Trees and fallback reflection to ensure seamless execution across all .NET runtime configurations.
- **Smart WASAPI Loopback Lifecycle**: WASAPI loopback capture now dynamically starts and stops with visualizer panel visibility, preventing background thread execution and idle CPU usage when the visualizer is closed.
- **32-bit Integer PCM Support**: Fixed distorted or silent audio on WASAPI loopback for 32-bit integer audio interfaces by properly detecting `WAVE_FORMAT_EXTENSIBLE` subformats and normalizing signed PCM.

### 2. High-Performance Math & Zero-Allocation Render Loop
- **Zero Heap Allocations**: Added thread-static target clear vectors to Direct3D 11 pass pipelines, completely eliminating GC allocations in the 144Hz render loop.
- **Pinned Pointer Math**: Re-engineered Discrete Fourier Transform (DFT) windowing and Fast Fourier Transform (FFT) passes with unsafe pinned pointer arithmetic, stripping out array bounds checks in time-critical audio calculation paths.
- **Open Generic Memory Safety**: Upgraded generic vertex and geometry streams to pinned `GCHandle` marshaling, fully passing strict CLR verifications.

---

## What's New in VisualBee v2.1

### 1. Panel Docking Self-Healing & Metric Protection
- **No More Fullscreen Traps**: Fixed the critical MusicBee issue where embedding a visualizer panel in sidebars or bottom panels could corrupt layout metrics into a 65,734px height loop that persisted even after deleting the DLL.
- **Automated Self-Healing (`HealPanelMetrics`)**: Automatically repairs corrupted panel metrics in memory and inside `MusicBee3Settings.ini` back to clean, resizable dimensions.
- **Zero Scrollbar Overhang**: Stripped unwanted 17px rightward overhang by disabling MusicBee's internal vertical scrollbar flags via reflection and clipping the DirectX swapchain directly to the visible dock container region.
- **Polished Fullscreen Toggle**: Fullscreen visualizer window is now non-intrusive (`TopMost = false`), allowing seamless Alt+Tab multitasking and clean toggle via double-click, `F11`, or `Escape`.

### 2. Physical Separation: Glass Pillars vs. Nixie Rungs
- **Pure Refractive Glass Pillars (`StyleGlass`)**: Refactored into a crystal cylinder column with Fresnel edge reflections, Snell's law chromatic dispersion, and fluid core glow with zero ladder rungs.
- **Authentic Nixie & VFD Rungs (`StyleNixie`)**: Re-engineered with dual metallic anode guide wires, laser-cut filament ladder rungs, hexagonal cathode wire mesh, and smoked bronze vacuum envelopes.
- **GlassFill Optics Selector**: Added 4 distinct tube fill modes: `Pure Crystal Glass`, `Bioluminescent Plasma`, `Glowing Filaments`, and `Stepped Ladder Rungs`.

### 3. 12 Hand-Crafted Studio Presets & 10 Palettes
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

## Usage & Controls

- **Layout Placement**: Go to **View** > **Arrange Panels...**, then drag **VisualBee Visualizer** to your preferred dock slot (main panel, bottom panel, or sidebars).
- **Popout Window**: Right-click the visualizer and choose **Popout Window** for dedicated multi-monitor viewing.
- **Full Screen**: Double-click or press `F11` to toggle full-screen visualizer mode. Press `Escape` or `F11` to exit.
- **Settings Studio**: Right-click the visualizer and select **Open VisualBee Studio...** (or `Ctrl + Shift + V`) to open the real-time studio rack.
- **Quick Keyboard Shortcuts**:
  - `Ctrl + Shift + L`: Next visual layout
  - `Ctrl + Shift + P`: Next preset
  - `Ctrl + Shift + V`: Open VisualBee Studio

---

## Architecture & Code Quality

VisualBee strictly adheres to high-performance systems programming and modularity standards:
- **Assembly Size**: 435 KB (x86, `.NET Framework 4.8`, native Direct3D 11).
- **Zero Heap Allocations in Render Loop**: Discard-mapped dynamic ring buffers.
- **Zero Managed Wrappers**: Direct native P/Invoke to DirectX 11 and DXGI runtime binaries.
- **License**: MIT
