# Changelog

## [1.1.0] - 2026-10-26

### Added: Studio VFD Rack & Glass Pillar Pipeline
- **Glass Pillar Enclosures & Ghosted Rungs**: Persistent translucent rounded-pill glass capsules framing each frequency band, with unlit translucent ladder rungs extending up to the peak envelope.
- **Amplitude-Driven HDR Incandescence ("White-Hot Overdrive")**: Signal energy coupling where bars surging into peak amplitude ramp to an incandescent white core (`float3(1.65, 1.65, 1.65)`), generating an electric cyan corona through ACES tone mapping and bloom.
- **Studio Decibel Grid Lines**: 5 ultra-fine reference datum lines at -24 dB, -18 dB, -12 dB, -6 dB, 0 dB with adjustable opacity (2% to 40%).
- **Wet Lacquer Horizon Reflection**: Glowing ground horizon datum line anchoring meters to a reflective desk, featuring exponential falloff attenuation and dark navy lacquer absorption tint.
- **Architectural Ladder Geometry**: Uniform horizontal ladder rung alignment across all bands with fine segment height/gap controls.
- **VFD Studio Glass Theme**: Signature McIntosh / VFD cyan-cobalt preset.

## [1.0.0] - 2026-10-26

### Initial Release
- Hardware-accelerated DirectX 11 rendering with support for 60 to 240+ fps.
- 3 bar shapes: Solid bars, Rounded pills, and LED segmented blocks.
- Real-time physics controls: independent attack/decay times, gravity drop rate, and peak hold times.
- Multiple color modes: vertical gradient, frequency spread, solid tips, and theme-aware hue cycling (sideways wave & spin).
- Modeless dark settings window cleanly organized into Mode-Specific (Bars, Waveform) and Universal tabs.
- Floor reflection and edge vignette effects.
- Single self-contained `mb_VisualBee.dll` with embedded DirectX runtimes.
