# VisualBee v2.1 Architecture & Engineering Handoff

> **Release:** VisualBee v2.1  
> **Date:** October 28, 2026  
> **Status:** Fully Built, Verified, and Deployed to Host & Sandbox  
> **Target:** MusicBee 3.7.9779 (`x86`, `.NET 4.8`)  
> **Architecture:** Zero-dependency Native Direct3D 11 (`d3d11.dll`, `dxgi.dll`, `d3dcompiler_47.dll`)  
> **Assembly:** `mb_VisualBee.dll` (435 KB binary footprint)  
> **Public Repository:** [https://github.com/Taffyex/VisualBee](https://github.com/Taffyex/VisualBee)  

---

## 1. Executive Summary

VisualBee v2.1 introduces the modular **VisualBee Studio Rack** interface, resolves host dockable panel metric corruption and scrollbar overhang, and physically differentiates **Glass Pillars** (pure refractive crystal) from **Nixie Rungs** (twin anode guide rails, cathode mesh, and incandescent ladder rungs).

### Key Upgrades in v2.1:
1. **Dockable Panel Self-Healing (`HealPanelMetrics`):**
   - Automatically detects corrupted negative height values (`<= -32767` / `65734px`) stored in MusicBee's internal dictionary and `MusicBee3Settings.ini`.
   - Repairs damaged metrics in memory and on disk back to pristine resizable dimensions.
2. **Scrollbar Overhang Elimination:**
   - Clears MusicBee's internal `ScrollBars` flags via reflection and clamps `_view` bounds to the visible dock container region, eliminating the 17px rightward cutoff.
3. **True Separation of Glass Pillars & Nixie Rungs:**
   - `StyleGlass` renders continuous crystal columns with Snell's law chromatic dispersion and fluid core glow.
   - `StyleNixie` renders physical vacuum tubes with twin metallic guide rails, laser-cut filament rungs, and cathode wire mesh.
   - Added `GlassFill` selector (`Pure Crystal Glass`, `Bioluminescent Plasma`, `Glowing Filaments`, `Stepped Ladder Rungs`).
4. **VisualBee Studio Rack UI:**
   - 5 top studio deck tabs (`Presets`, `Studio`, `Optics & FX`, `Colours`, `Audio`).
   - Persistent quick-switch ribbon with instant preset switching and real-time telemetry.
   - Responsive 2-column hardware rack cards with per-section reset.
   - Dual hover tooltips for all acoustic and optical parameters.
5. **12 Original Factory Presets & 10 Palettes:**
   - Replaced all legacy presets with 12 studio-grade original presets and 10 color palettes.
