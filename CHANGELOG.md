# Changelog

All notable changes to **VisualBee** will be documented in this file.

## [1.0.0] - 2026-10-26

### Initial Release
- **Hardware-Accelerated Direct3D 11 Rendering**: High-refresh DXGI flip-model swapchain presentation (`DXGI_SWAP_EFFECT_FLIP_DISCARD`) with waitable swap chain objects for ultra-low latency and jitter-free 60/120/144/240 Hz playback.
- **Dedicated Modeless Settings Window**:
  - Independent 780×600 floating configuration dialog (`View > VisualBee Settings` or Right Click > `Settings`).
  - Dark custom owner-drawn UI with segmented tab bars, precision sliders, toggles, and real-time color swatches.
  - Multi-tab organization: General, Bars, Colours, Peaks, Analyzer, Effects, Performance, and About.
  - Diagnostics report generator with one-click clipboard export.
  - Live telemetry display (FPS, frame time, audio tap mode, sample rate, analysis rate).
- **Advanced Bar Dynamics & Shapes**:
  - Selectable bar shapes: Solid Rectangles, Rounded Pills, and LED Segmented Blocks.
  - Configurable LED segment height and segment gap.
  - Corner roundness control (0% to 100%).
  - Treble taper width adjustment.
  - Floor reflection with mirror opacity falloff.
  - Dynamic edge vignette.
- **Spectrum Visualization Modes**:
  - **Bars**: Multi-band spectrum with logarithmic and musical frequency binning.
  - **Ribbon**: Smooth audio waveform and filled glow ribbon.
  - **Radial Nova**: Circular radial spectrum burst.
  - **Phase Scope**: Lissajous stereo field phase scope.
  - **VU Meter**: Analog-style peak and RMS VU meters.
- **Color Gradients & Shading**:
  - 4 distinct bar shading modes: Vertical Gradient, Across Frequencies, Solid Tip, and Dynamic Hue Cycle.
  - Custom base color, tip color, and floating peak cap color.
  - 4 mirror modes: None, Horizontal, Vertical, and Both (quadrant symmetry).
- **Precision Audio Engine**:
  - Low-latency 64-bit BASS audio tap directly from MusicBee's playback pipeline.
  - Gaussian FFT windowing and logarithmic frequency mapping.
  - Independent Attack Time (ms) and Decay Time (ms) for responsive transients without visual jitter.
  - Peak cap physics with configurable hold duration (ms) and gravity drop rate.
  - Treble tilt EQ curve (+0 dB to +24 dB) and dynamic dB floor adjustment (-90 dB to -20 dB).
- **Zero-Dependency Architecture**:
  - Single standalone `mb_VisualBee.dll` (all SharpDX Direct3D 11 runtime libraries embedded directly via assembly resolver).
  - Clean lifecycle management: zero GDI or USER handle leaks on panel docking, undocking, or player shutdown.
