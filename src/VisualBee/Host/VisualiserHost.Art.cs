using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class VisualiserHost
{
	private volatile ArtImage _art;
	private int _artRequest;
	private bool _artWanted;
	private System.Threading.Timer _artRetryTimer;
	private int _trackSerial;
	private volatile float _trackGainDb = float.NaN;
	private volatile float _albumGainDb = float.NaN;
	private volatile bool _artDisposed;

	public ArtImage Art => _art;
	public int TrackSerial => Volatile.Read(ref _trackSerial);
	public float TrackGainDb => _trackGainDb;
	public float AlbumGainDb => _albumGainDb;

	public void TrackChanged()
	{
		Interlocked.Increment(ref _trackSerial);
		ReadReplayGainTags();
		_artWanted = false;
		if (Store?.Current?.WantsArt != true)
		{
			Interlocked.Increment(ref _artRequest);
			CancelArtRetry();
			_art = null;
			return;
		}
		RequestArtwork(authoritative: false);
	}

	public void ArtworkReady()
	{
		_artWanted = false;
		if (Store?.Current?.WantsArt != true)
		{
			Interlocked.Increment(ref _artRequest);
			CancelArtRetry();
			_art = null;
			return;
		}
		RequestArtwork(authoritative: true);
	}

	private void ArtMaybeWanted()
	{
		if (Store?.Current?.WantsArt != true || _artWanted)
		{
			return;
		}
		RequestArtwork(authoritative: false);
	}

	private void RequestArtwork(bool authoritative)
	{
		if (_artDisposed || Store?.Current?.WantsArt != true)
		{
			return;
		}
		if (authoritative)
		{
			CancelArtRetry();
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
		if (string.IsNullOrEmpty(data) && !authoritative)
		{
			ScheduleArtRetry();
			return;
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
				_view?.Redraw();
				_fullScreenView?.Redraw();
			}
		});
	}

	private void ScheduleArtRetry()
	{
		if (_artDisposed)
		{
			return;
		}
		if (_artRetryTimer == null)
		{
			_artRetryTimer = new System.Threading.Timer(_ => OnArtRetryTimer(), null, 1500, Timeout.Infinite);
		}
		else
		{
			try { _artRetryTimer.Change(1500, Timeout.Infinite); } catch (ObjectDisposedException) { }
		}
	}

	private void CancelArtRetry()
	{
		try { _artRetryTimer?.Change(Timeout.Infinite, Timeout.Infinite); } catch (ObjectDisposedException) { }
	}

	private void OnArtRetryTimer()
	{
		if (_artDisposed)
		{
			return;
		}
		Control panel = _panel;
		if (panel != null && !panel.IsDisposed && panel.IsHandleCreated && panel.InvokeRequired)
		{
			try
			{
				panel.BeginInvoke(new Action(ArtworkReady));
				return;
			}
			catch (Exception)
			{
			}
		}
		ArtworkReady();
	}

	private void DisposeArt()
	{
		_artDisposed = true;
		System.Threading.Timer t = Interlocked.Exchange(ref _artRetryTimer, null);
		if (t != null)
		{
			try { t.Dispose(); } catch (Exception) { }
		}
	}

	private void ReadReplayGainTags()
	{
		string trackStr = null;
		string albumStr = null;
		try
		{
			if (_api.NowPlaying_GetFileProperty != null)
			{
				trackStr = _api.NowPlaying_GetFileProperty(FilePropertyType.ReplayGainTrack);
				albumStr = _api.NowPlaying_GetFileProperty(FilePropertyType.ReplayGainAlbum);
			}
			if ((string.IsNullOrWhiteSpace(trackStr) || string.IsNullOrWhiteSpace(albumStr)) && _api.Library_GetFileProperty != null && _api.NowPlaying_GetFileUrl != null)
			{
				string url = _api.NowPlaying_GetFileUrl();
				if (!string.IsNullOrEmpty(url))
				{
					if (string.IsNullOrWhiteSpace(trackStr))
						trackStr = _api.Library_GetFileProperty(url, FilePropertyType.ReplayGainTrack);
					if (string.IsNullOrWhiteSpace(albumStr))
						albumStr = _api.Library_GetFileProperty(url, FilePropertyType.ReplayGainAlbum);
				}
			}
		}
		catch (Exception)
		{
		}
		_trackGainDb = ParseGainDb(trackStr);
		_albumGainDb = ParseGainDb(albumStr);
	}

	private static float ParseGainDb(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return float.NaN;
		}
		string s = raw.Trim();
		if (s.EndsWith("dB", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(0, s.Length - 2).Trim();
		}
		if (s.StartsWith("+", StringComparison.Ordinal))
		{
			s = s.Substring(1).Trim();
		}
		if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) && !float.IsNaN(val) && !float.IsInfinity(val))
		{
			return val;
		}
		return float.NaN;
	}
}
