using System;

namespace MusicBeePlugin;

internal static partial class SpectrumSettings
{
	private static void AddHud(Schema s)
	{
		s.Page("Mini-Player").Section("Visuals & Opacity");
		s.Number("hud.backdropOpacity", "Backdrop opacity", "Opacity of the frosted background layer. 0% turns the background completely transparent, leaving only the floating D3D11 visualizer and HUD controls over your desktop or game.", 0.0, 100.0, 5.0, 0, "%", (SpectrumLook l) => l.HudBackdropOpacity, (SpectrumLook l, double v) =>
		{
			l.HudBackdropOpacity = v;
		}).Words("transparency glass background alpha");

		s.Number("hud.opacityActive", "Active hover opacity", "Overall window opacity when the mouse is hovering over the Mini-Player HUD.", 10.0, 100.0, 5.0, 0, "%", (SpectrumLook l) => l.HudOpacityActive, (SpectrumLook l, double v) =>
		{
			l.HudOpacityActive = v;
		}).Words("hover focus opacity");

		s.Number("hud.opacityIdle", "Idle unfocused opacity", "Overall window opacity when the mouse leaves the HUD. Smoothly fades to this value to prevent distraction while gaming or working.", 10.0, 100.0, 5.0, 0, "%", (SpectrumLook l) => l.HudOpacityIdle, (SpectrumLook l, double v) =>
		{
			l.HudOpacityIdle = v;
		}).Words("idle dim fade blur");

		s.Choice("hud.controlsMode", "Controls visibility", "When to reveal the playback transport, Love button, and scrubbable seekbar.", SpectrumLook.HudControlsNames, SpectrumLook.HudControlsTokens, (SpectrumLook l) => l.HudControlsMode, (SpectrumLook l, int v) =>
		{
			l.HudControlsMode = v;
		}).Words("reveal fade hover buttons seekbar");

		s.Section("Track Info & Controls");
		s.Toggle("hud.showArt", "Show album artwork", "Displays a rounded cover art thumbnail on the floating player.", (SpectrumLook l) => l.HudShowArt, (SpectrumLook l, bool v) =>
		{
			l.HudShowArt = v;
		}).Words("album art cover thumbnail");

		s.Toggle("hud.showLove", "Show Love (♥) button", "1-click Heart icon on the HUD that loves/unloves the currently playing track in your MusicBee library tags.", (SpectrumLook l) => l.HudShowLove, (SpectrumLook l, bool v) =>
		{
			l.HudShowLove = v;
		}).Words("love heart rating favorite");

		s.Toggle("hud.showBadge", "Show audio format badge", "Shows discrete studio codec badges (e.g. FLAC 96kHz, 320k MP3) next to the track metadata.", (SpectrumLook l) => l.HudShowBadge, (SpectrumLook l, bool v) =>
		{
			l.HudShowBadge = v;
		}).Words("codec flac mp3 bitrate sample rate badge");

		s.Section("Window & Lifecycle Sync");
		s.Toggle("hud.topMost", "Always on top", "Keeps the floating HUD above all normal desktop windows and borderless games.", (SpectrumLook l) => l.HudTopMost, (SpectrumLook l, bool v) =>
		{
			l.HudTopMost = v;
		}).Mine().Words("topmost pin float");

		s.Toggle("hud.clickThrough", "Ghost mode (click-through)", "Passes all mouse clicks directly through the HUD to underlying games and apps. Hotkey: Ctrl+Shift+T.", (SpectrumLook l) => l.HudClickThrough, (SpectrumLook l, bool v) =>
		{
			l.HudClickThrough = v;
		}).Mine().Words("ghost clickthrough lock passthrough");

		s.Choice("hud.syncMode", "MusicBee integration", "How the floating HUD responds to MusicBee Mini-Player (Ctrl+Shift+F9) and minimize events.", SpectrumLook.HudSyncNames, SpectrumLook.HudSyncTokens, (SpectrumLook l) => l.HudSyncMode, (SpectrumLook l, int v) =>
		{
			l.HudSyncMode = v;
		}).Words("sync mini player replace magnetic tray dock");

		s.Toggle("hud.globalHotkeys", "Enable global hotkeys", "Registers Ctrl+Shift+O (Toggle HUD), Ctrl+Shift+T (Toggle Ghost Mode), and Ctrl+Shift+L (Love Track) globally across Windows.", (SpectrumLook l) => l.HudGlobalHotkeys, (SpectrumLook l, bool v) =>
		{
			l.HudGlobalHotkeys = v;
		}).Mine().Words("hotkeys shortcuts global background");
	}
}
