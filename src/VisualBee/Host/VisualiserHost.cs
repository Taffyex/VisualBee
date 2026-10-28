using System.Threading;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class VisualiserHost
{
	public readonly LookStore Store;

	public readonly AudioTap Tap = new AudioTap();

	private readonly Func<VisualiserHost, bool, GpuView> _makeView;

	private MusicBeeApiInterface _api;

	private GpuView _view;

	internal GpuView View => _view;

	private GpuView _fullScreenView;

	private Control _panel;

	private Form _fullScreenForm;

	private SettingsWindow _settings;

	private volatile ArtImage _art;

	private int _artRequest;

	private bool _artWanted;

	public ArtImage Art => _art;

	public VisualiserHost(LookStore store, Func<VisualiserHost, bool, GpuView> makeView)
	{
		Store = store;
		_makeView = makeView;
		store.Changed += (object s, EventArgs a) =>
		{
			ArtMaybeWanted();
		};
	}

	public void SetApi(MusicBeeApiInterface api)
	{
		_api = api;
	}

	public void LoadSettings(string fileName, Func<string, bool> migrate)
	{
		string path = StorageFolder();
		string text = Path.Combine(path, fileName);
		string presetFolder = Path.Combine(path, "VisualBee", "Presets");
		if (!Store.Load(text, presetFolder))
		{
			migrate?.Invoke(text);
			Store.Save();
		}
	}

	public string StorageFolder()
	{
		try
		{
			if (_api.Setting_GetPersistentStoragePath != null)
			{
				string text = _api.Setting_GetPersistentStoragePath();
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch (Exception)
		{
		}
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MusicBee");
	}

	private volatile PlayState _cachedState = PlayState.Playing;

	public PlayState CachedPlayState => _cachedState;

	public void PlayStateChanged()
	{
		_cachedState = State();
	}

	public PlayState State()
	{
		Tap.Bind(_api.NowPlaying_GetSpectrumData);
		if (_api.Player_GetPlayState == null)
		{
			return PlayState.Playing;
		}
		try
		{
			PlayState s = _api.Player_GetPlayState();
			_cachedState = s;
			return s;
		}
		catch (Exception)
		{
			return PlayState.Playing;
		}
	}

	public int NudgeDelay(int deltaMs)
	{
		Setting setting = Store.Schema.Find("sync.delayMs");
		int num = Math.Max(0, Math.Min(500, (int)Math.Round((double)setting.Get(Store.Current)) + deltaMs));
		Store.Set(setting, (double)num);
		return num;
	}

	public void TrackChanged()
	{
		_artWanted = false;
		ArtMaybeWanted();
	}

	private void ArtMaybeWanted()
	{
		if (!Store.Current.WantsArt || _artWanted)
		{
			return;
		}
		_artWanted = true;
		int request = Interlocked.Increment(ref _artRequest);
		string data = null;
		try
		{
			if (_api.NowPlaying_GetArtwork != null)
			{
				data = _api.NowPlaying_GetArtwork();
			}
		}
		catch (Exception)
		{
		}
		ThreadPool.QueueUserWorkItem((object s) =>
		{
			byte[] data2 = null;
			try
			{
				if (!string.IsNullOrEmpty(data))
				{
					data2 = Convert.FromBase64String(data);
				}
			}
			catch (FormatException)
			{
			}
			ArtImage art = ArtImage.Decode(data2);
			if (request == Volatile.Read(ref _artRequest))
			{
				_art = art;
			}
		});
	}

	public void Attach(Control panel)
	{
		Remove();
		_panel = panel;
		HealPanelMetrics(panel);
		DisableHostScrollOverhang(panel);
		_view = _makeView(this, arg2: false);
		_view.Dock = DockStyle.None;
		panel.Controls.Add(_view);
		panel.Disposed += PanelDisposed;
		panel.Resize += PanelResized;
		SyncViewBounds();
		_view.Start();
		ArtMaybeWanted();
	}

	private void PanelResized(object sender, EventArgs e) => SyncViewBounds();

	private void PanelDisposed(object sender, EventArgs e)
	{
		Remove();
	}

	public void Remove()
	{
		CloseFullScreen();
		if (_settings != null && !_settings.IsDisposed)
		{
			try
			{
				_settings.Close();
			}
			catch (Exception)
			{
			}
		}
		_settings = null;
		GpuView view = _view;
		_view = null;
		if (_panel != null)
		{
			_panel.Disposed -= PanelDisposed;
			_panel.Resize -= PanelResized;
		}
		_panel = null;
		if (view == null)
		{
			return;
		}
		try
		{
			view.Dispose();
		}
		catch (Exception)
		{
		}
	}

	private void CloseFullScreen()
	{
		Form fullScreenForm = _fullScreenForm;
		_fullScreenForm = null;
		_fullScreenView = null;
		if (fullScreenForm == null)
		{
			return;
		}
		try
		{
			fullScreenForm.Close();
		}
		catch (Exception)
		{
		}
	}

	public string StatusText()
	{
		string text;
		if (Tap.Polls != 0)
		{
			if (Tap.Direct)
			{
				if (Tap.HistoryRate > 0)
				{
					text = "direct PCM, " + Tap.HistoryRate + " Hz, " + Tap.HistoryChannels + " ch, " + Tap.Timing();
				}
				else
				{
					text = ((Tap.Problem.Length > 0) ? Tap.Problem : "waiting for playback");
				}
			}
			else
			{
				text = ((Tap.Status.Length > 0) ? Tap.Status : "MusicBee FFT API fallback (universal)");
			}
		}
		else
		{
			text = "the visualiser has not started updating";
		}
		GpuView gpuView = _view ?? _fullScreenView;
		return "Audio: " + text + "\nPicture: " + ((gpuView != null) ? gpuView.GpuStatus : "no panel open");
	}

}
