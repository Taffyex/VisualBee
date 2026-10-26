using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbSegmented _segMode;

        // Bars Geometry
        private VbSegmented _segBarStyle;
        private VbSlider _sldBarCount;
        private VbSlider _sldBarGap;
        private VbSlider _sldRoundness;
        private VbSlider _sldTrebleTaper;
        private VbToggle _togHighlightTips;

        // Glass & Rungs Sub-panel
        private Panel _pnlGlassCard;
        private VbSegmented _segGlassFill;
        private VbToggle _togGlassOverlay;
        private VbSlider _sldGlassOpacity;
        private VbSlider _sldLedSegH;
        private VbSlider _sldLedSegGap;
        private VbSlider _sldGhostRange;
        private VbSlider _sldGhostOpacity;

        // Symmetry & Layout
        private VbSegmented _segVerticalLayout;
        private VbToggle _togCenterLaser;
        private VbSlider _sldCenterLaser;
        private VbToggle _togCenterColor;
        private VbToggle _togMirrorH;
        private VbSegmented _segStereo;

        // Peaks
        private VbToggle _togPeakEnabled;
        private VbSlider _sldPeakHold;
        private VbSlider _sldPeakGravity;
        private VbSlider _sldPeakThickness;

        // Waveform Controls
        private VbSlider _sldRibbonThickness;
        private VbToggle _togRibbonFill;
        private VbToggle _togRibbonFloor;

        private void BuildStudioPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("VISUALIZER MODE"));
            _segMode = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Spectrum Bars", "Liquid Neon Waveform" },
                SelectedIndex = (int)_settings.Mode
            };
            _segMode.SelectedIndexChanged += idx =>
            {
                _settings.Mode = (VisualizerMode)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
                UpdateStudioVisibility();
            };
            pnl.Controls.Add(_segMode);
            pnl.Controls.Add(CreateSpacer(8));

            // Bar Style
            pnl.Controls.Add(CreateHeaderLabel("BAR VISUAL STYLE"));
            _segBarStyle = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Smooth Rounded Pills", "Frosted Segmented LEDs", "Solid Neo-Bars", "Glass Pillars (VFD Studio)" },
                SelectedIndex = (int)_settings.BarStyle
            };
            _segBarStyle.SelectedIndexChanged += idx =>
            {
                _settings.BarStyle = (BarVisualEffect)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
                UpdateGlassVisibility();
            };
            pnl.Controls.Add(_segBarStyle);
            pnl.Controls.Add(CreateSpacer(6));

            // Glass Pillars Card
            _pnlGlassCard = new Panel { Width = 540, Height = 340, BackColor = VbTheme.BgCard, Padding = new Padding(8), Margin = new Padding(0, 4, 0, 8) };
            var flowGlass = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            flowGlass.Controls.Add(CreateHeaderLabel("GLASS TUBE & VFD RUNGS STUDIO"));

            _segGlassFill = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Mixed Plasma & Rungs (Ref 1)", "Segmented Rungs Only (Ref 2)", "Solid Glass Core Only" },
                SelectedIndex = (int)_settings.GlassFill
            };
            _segGlassFill.SelectedIndexChanged += idx =>
            {
                _settings.GlassFill = (GlassFillMode)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            flowGlass.Controls.Add(_segGlassFill);

            _togGlassOverlay = new VbToggle { Width = 520, Label = "Show Outer Glass Capsule Enclosure & Sheen", Checked = _settings.GlassTubeOverlay };
            _togGlassOverlay.CheckedChanged += chk =>
            {
                _settings.GlassTubeOverlay = chk;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            flowGlass.Controls.Add(_togGlassOverlay);

            _sldGlassOpacity = new VbSlider { Width = 520, Label = "Glass Tube Reflection & Sheen Opacity", Minimum = 5f, Maximum = 100f, DefaultValue = 45f, Value = _settings.GlassOpacity * 100f, Unit = "%" };
            _sldGlassOpacity.ValueChanged += val => { _settings.GlassOpacity = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            flowGlass.Controls.Add(_sldGlassOpacity);

            _sldLedSegH = new VbSlider { Width = 520, Label = "Horizontal Rung Thickness", Minimum = 2f, Maximum = 20f, DefaultValue = 5f, Value = _settings.LedSegmentHeight, DecimalPlaces = 1, Unit = "px" };
            _sldLedSegH.ValueChanged += val => { _settings.LedSegmentHeight = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            flowGlass.Controls.Add(_sldLedSegH);

            _sldLedSegGap = new VbSlider { Width = 520, Label = "Rung Spacing / Gap", Minimum = 1f, Maximum = 10f, DefaultValue = 2f, Value = _settings.LedSegmentGap, DecimalPlaces = 1, Unit = "px" };
            _sldLedSegGap.ValueChanged += val => { _settings.LedSegmentGap = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            flowGlass.Controls.Add(_sldLedSegGap);

            _sldGhostOpacity = new VbSlider { Width = 520, Label = "Unlit Ghost Rung Phosphor Opacity (Above Active Bar)", Minimum = 0f, Maximum = 70f, DefaultValue = 28f, Value = _settings.RungGhostOpacity * 100f, Unit = "%" };
            _sldGhostOpacity.ValueChanged += val => { _settings.RungGhostOpacity = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            flowGlass.Controls.Add(_sldGhostOpacity);

            _sldGhostRange = new VbSlider { Width = 520, Label = "Ghosted Ladder Trail Height", Minimum = 0f, Maximum = 100f, DefaultValue = 35f, Value = _settings.RungGhostRange * 100f, Unit = "%" };
            _sldGhostRange.ValueChanged += val => { _settings.RungGhostRange = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            flowGlass.Controls.Add(_sldGhostRange);

            _pnlGlassCard.Controls.Add(flowGlass);
            pnl.Controls.Add(_pnlGlassCard);

            // Bar Geometry
            pnl.Controls.Add(CreateHeaderLabel("BAR GEOMETRY & SIZING"));
            _sldBarCount = new VbSlider { Width = 540, Label = "Bar Count", Minimum = 16, Maximum = 160, DefaultValue = 32, Value = _settings.BarCount, DecimalPlaces = 0, Unit = "bars" };
            _sldBarCount.ValueChanged += val => { _settings.BarCount = (int)Math.Round(val); _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldBarCount);

            _sldBarGap = new VbSlider { Width = 540, Label = "Bar Spacing / Gap", Minimum = 0f, Maximum = 60f, DefaultValue = 26f, Value = _settings.BarGap * 100f, DecimalPlaces = 0, Unit = "%" };
            _sldBarGap.ValueChanged += val => { _settings.BarGap = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldBarGap);

            _sldRoundness = new VbSlider { Width = 540, Label = "Corner Roundness", Minimum = 0f, Maximum = 100f, DefaultValue = 60f, Value = _settings.BarRoundness * 100f, DecimalPlaces = 0, Unit = "%" };
            _sldRoundness.ValueChanged += val => { _settings.BarRoundness = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldRoundness);

            _sldTrebleTaper = new VbSlider { Width = 540, Label = "Treble Taper (Narrower Highs)", Minimum = 0f, Maximum = 80f, DefaultValue = 5f, Value = _settings.TrebleTaper * 100f, DecimalPlaces = 0, Unit = "%" };
            _sldTrebleTaper.ValueChanged += val => { _settings.TrebleTaper = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldTrebleTaper);

            _togHighlightTips = new VbToggle { Width = 540, Label = "Highlight Bar Tips & Centers (Color D Accent)", Checked = _settings.HighlightBarTips };
            _togHighlightTips.CheckedChanged += chk => { _settings.HighlightBarTips = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togHighlightTips);

            // Symmetry & Layout Card
            pnl.Controls.Add(CreateSpacer(6));
            pnl.Controls.Add(CreateHeaderLabel("LAYOUT & SYMMETRY"));

            _segVerticalLayout = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Grounded (Bottom-Up)", "Center-Symmetric (Up & Down Axis)" },
                SelectedIndex = (_settings.Mirror & BarMirror.Vertical) != 0 ? 1 : 0
            };
            _segVerticalLayout.SelectedIndexChanged += idx =>
            {
                if (idx == 1) _settings.Mirror |= BarMirror.Vertical;
                else _settings.Mirror &= ~BarMirror.Vertical;
                _settings.NotifyChanged();
                _settings.RequestSave();
                UpdateSymmetryVisibility();
            };
            pnl.Controls.Add(_segVerticalLayout);

            _togCenterLaser = new VbToggle { Width = 540, Label = "Center Horizon Laser Beam Line (Ref 2)", Checked = _settings.CenterLineGlow };
            _togCenterLaser.CheckedChanged += chk => { _settings.CenterLineGlow = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togCenterLaser);

            _sldCenterLaser = new VbSlider { Width = 540, Label = "Laser Line Glow Intensity", Minimum = 0f, Maximum = 200f, DefaultValue = 60f, Value = _settings.CenterLineGlowIntensity * 100f, Unit = "%" };
            _sldCenterLaser.ValueChanged += val => { _settings.CenterLineGlowIntensity = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldCenterLaser);

            _togCenterColor = new VbToggle { Width = 540, Label = "Equal Up/Down Color & Hot-Core Distribution", Checked = _settings.SymmetricCenterColor };
            _togCenterColor.CheckedChanged += chk => { _settings.SymmetricCenterColor = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togCenterColor);

            _togMirrorH = new VbToggle { Width = 540, Label = "Horizontal Mirror (Bass centered, expanding outward)", Checked = (_settings.Mirror & BarMirror.Horizontal) != 0 };
            _togMirrorH.CheckedChanged += chk =>
            {
                if (chk) _settings.Mirror |= BarMirror.Horizontal;
                else _settings.Mirror &= ~BarMirror.Horizontal;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togMirrorH);

            _segStereo = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Standard Mono", "Center-Out Stereo Wings", "Split L/R" },
                SelectedIndex = (int)_settings.StereoMode
            };
            _segStereo.SelectedIndexChanged += idx =>
            {
                _settings.StereoMode = (StereoMode)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_segStereo);

            // Floating Peak Caps Card
            pnl.Controls.Add(CreateSpacer(6));
            pnl.Controls.Add(CreateHeaderLabel("FLOATING PEAK CAPS"));
            _togPeakEnabled = new VbToggle { Width = 540, Label = "Enable Floating Peak Hold Caps", Checked = _settings.PeakHoldEnabled };
            _togPeakEnabled.CheckedChanged += chk => { _settings.PeakHoldEnabled = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togPeakEnabled);

            _sldPeakHold = new VbSlider { Width = 540, Label = "Peak Hold Time", Minimum = 50f, Maximum = 1500f, DefaultValue = 350f, Value = _settings.PeakHoldMs, Unit = "ms" };
            _sldPeakHold.ValueChanged += val => { _settings.PeakHoldMs = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldPeakHold);

            _sldPeakGravity = new VbSlider { Width = 540, Label = "Fall Speed (Gravity)", Minimum = 0.5f, Maximum = 8.0f, DefaultValue = 2.8f, Value = _settings.PeakGravity, DecimalPlaces = 1, Unit = "g" };
            _sldPeakGravity.ValueChanged += val => { _settings.PeakGravity = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldPeakGravity);

            _sldPeakThickness = new VbSlider { Width = 540, Label = "Peak Cap Thickness", Minimum = 1.0f, Maximum = 6.0f, DefaultValue = 2.0f, Value = _settings.PeakCapThickness, DecimalPlaces = 1, Unit = "px" };
            _sldPeakThickness.ValueChanged += val => { _settings.PeakCapThickness = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldPeakThickness);

            // Waveform Controls
            pnl.Controls.Add(CreateSpacer(6));
            pnl.Controls.Add(CreateHeaderLabel("WAVEFORM STUDIO"));

            _sldRibbonThickness = new VbSlider { Width = 540, Label = "Line Thickness", Minimum = 0.5f, Maximum = 8.0f, DefaultValue = 2.5f, Value = _settings.RibbonThickness, DecimalPlaces = 1, Unit = "px" };
            _sldRibbonThickness.ValueChanged += val => { _settings.RibbonThickness = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldRibbonThickness);

            _togRibbonFill = new VbToggle { Width = 540, Label = "Audio-Reactive Baseline Luminous Glow Fill", Checked = _settings.RibbonFill };
            _togRibbonFill.CheckedChanged += chk => { _settings.RibbonFill = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togRibbonFill);

            _togRibbonFloor = new VbToggle { Width = 540, Label = "Waveform Floor Mirror Reflection", Checked = _settings.RibbonFloorReflection };
            _togRibbonFloor.CheckedChanged += chk => { _settings.RibbonFloorReflection = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togRibbonFloor);

            UpdateStudioVisibility();
            host.Controls.Add(pnl);
        }

        private void UpdateStudioVisibility()
        {
            UpdateGlassVisibility();
            UpdateSymmetryVisibility();
        }

        private void UpdateGlassVisibility()
        {
            bool isGlassOrSeg = (_settings.BarStyle == BarVisualEffect.GlassPillar || _settings.BarStyle == BarVisualEffect.Segmented);
            if (_pnlGlassCard != null) _pnlGlassCard.Visible = isGlassOrSeg;
            if (_segGlassFill != null) _segGlassFill.Enabled = (_settings.BarStyle == BarVisualEffect.GlassPillar);
            if (_togGlassOverlay != null) _togGlassOverlay.Enabled = (_settings.BarStyle == BarVisualEffect.GlassPillar);
            if (_sldGlassOpacity != null) _sldGlassOpacity.Enabled = (_settings.BarStyle == BarVisualEffect.GlassPillar);
        }

        private void UpdateSymmetryVisibility()
        {
            bool isVert = (_settings.Mirror & BarMirror.Vertical) != 0;
            if (_togCenterLaser != null) _togCenterLaser.Enabled = isVert;
            if (_sldCenterLaser != null) _sldCenterLaser.Enabled = isVert && _settings.CenterLineGlow;
            if (_togCenterColor != null) _togCenterColor.Enabled = isVert;
        }

        private void RefreshStudioPageUi()
        {
            if (_segMode != null) _segMode.SelectedIndex = (int)_settings.Mode;
            if (_segBarStyle != null) _segBarStyle.SelectedIndex = (int)_settings.BarStyle;
            if (_segGlassFill != null) _segGlassFill.SelectedIndex = (int)_settings.GlassFill;
            if (_togGlassOverlay != null) _togGlassOverlay.Checked = _settings.GlassTubeOverlay;
            if (_sldGlassOpacity != null) _sldGlassOpacity.Value = _settings.GlassOpacity * 100f;
            if (_sldLedSegH != null) _sldLedSegH.Value = _settings.LedSegmentHeight;
            if (_sldLedSegGap != null) _sldLedSegGap.Value = _settings.LedSegmentGap;
            if (_sldGhostOpacity != null) _sldGhostOpacity.Value = _settings.RungGhostOpacity * 100f;
            if (_sldGhostRange != null) _sldGhostRange.Value = _settings.RungGhostRange * 100f;
            if (_sldBarCount != null) _sldBarCount.Value = _settings.BarCount;
            if (_sldBarGap != null) _sldBarGap.Value = _settings.BarGap * 100f;
            if (_sldRoundness != null) _sldRoundness.Value = _settings.BarRoundness * 100f;
            if (_sldTrebleTaper != null) _sldTrebleTaper.Value = _settings.TrebleTaper * 100f;
            if (_togHighlightTips != null) _togHighlightTips.Checked = _settings.HighlightBarTips;
            if (_segVerticalLayout != null) _segVerticalLayout.SelectedIndex = (_settings.Mirror & BarMirror.Vertical) != 0 ? 1 : 0;
            if (_togCenterLaser != null) _togCenterLaser.Checked = _settings.CenterLineGlow;
            if (_sldCenterLaser != null) _sldCenterLaser.Value = _settings.CenterLineGlowIntensity * 100f;
            if (_togCenterColor != null) _togCenterColor.Checked = _settings.SymmetricCenterColor;
            if (_togMirrorH != null) _togMirrorH.Checked = (_settings.Mirror & BarMirror.Horizontal) != 0;
            if (_segStereo != null) _segStereo.SelectedIndex = (int)_settings.StereoMode;
            if (_togPeakEnabled != null) _togPeakEnabled.Checked = _settings.PeakHoldEnabled;
            if (_sldPeakHold != null) _sldPeakHold.Value = _settings.PeakHoldMs;
            if (_sldPeakGravity != null) _sldPeakGravity.Value = _settings.PeakGravity;
            if (_sldPeakThickness != null) _sldPeakThickness.Value = _settings.PeakCapThickness;
            if (_sldRibbonThickness != null) _sldRibbonThickness.Value = _settings.RibbonThickness;
            if (_togRibbonFill != null) _togRibbonFill.Checked = _settings.RibbonFill;
            if (_togRibbonFloor != null) _togRibbonFloor.Checked = _settings.RibbonFloorReflection;
            UpdateStudioVisibility();
        }
    }
}
