using System;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private void Analyse(SpectrumLook look, bool stereo)
	{
		bool needCentres = false;
		if (look.Engine != _engine || _analysedBands != _bands || look.FftSize != _fftChoice || look.Window != _windowShape || look.Value != _value || look.Scale != _scale || look.MinHz != _minHz || look.MaxHz != _maxHz || look.Channels != _channelMode || stereo != _stereo)
		{
			_engine = look.Engine;
			_analysedBands = _bands;
			_fftChoice = look.FftSize;
			_windowShape = look.Window;
			_value = look.Value;
			_scale = look.Scale;
			_minHz = look.MinHz;
			_maxHz = look.MaxHz;
			_channelMode = look.Channels;
			_stereo = stereo;
			_window = default;
			_geo = null;
			needCentres = true;
		}

		int source = look.AudioSource;
		if (source == SpectrumLook.AudioSourceWasapiLoopback)
		{
			AnalyseLoopback(look, stereo, needCentres);
		}
		else if (source == SpectrumLook.AudioSourceAlwaysApi)
		{
			AnalyseApi(look, needCentres);
		}
		else if (source == SpectrumLook.AudioSourceBassOnly)
		{
			AnalyseDirect(look, stereo, needCentres, allowApiFallback: false, allowLoopbackFallback: false);
		}
		else if (source == SpectrumLook.AudioSourcePlayerOnly)
		{
			AnalyseDirect(look, stereo, needCentres, allowApiFallback: true, allowLoopbackFallback: false);
		}
		else
		{
			AnalyseDirect(look, stereo, needCentres, allowApiFallback: true, allowLoopbackFallback: true);
		}
	}

	private bool AnalyseDirect(SpectrumLook look, bool stereo, bool needCentres, bool allowApiFallback, bool allowLoopbackFallback)
	{
		bool isPlaying = Host?.CachedPlayState == PlayState.Playing;
		if (isPlaying && _plugin.ReadWindow(look.Engine, look.FftSize, look.Channels, stereo, look.DelayMs, ref _window, ref _left, ref _right, out var fresh))
		{
			if (_window.Rate != _rate)
			{
				_rate = _window.Rate;
				_geo = null;
				needCentres = true;
			}
			_hasAudio = true;
			if (needCentres || _centres.Length != _bands) Centres(look);
			if (!fresh) return true;
			ProcessPcm(_window.Size, _rate, look);
			return true;
		}
		if (allowLoopbackFallback && Host?.Loopback != null && Host.Loopback.HasActiveAudio())
		{
			if (AnalyseLoopback(look, stereo, needCentres)) return true;
		}
		if (!isPlaying && _plugin.ReadWindow(look.Engine, look.FftSize, look.Channels, stereo, look.DelayMs, ref _window, ref _left, ref _right, out fresh))
		{
			if (_window.Rate != _rate)
			{
				_rate = _window.Rate;
				_geo = null;
				needCentres = true;
			}
			_hasAudio = true;
			if (needCentres || _centres.Length != _bands) Centres(look);
			if (!fresh) return true;
			ProcessPcm(_window.Size, _rate, look);
			return true;
		}
		if (allowApiFallback)
		{
			return AnalyseApi(look, needCentres);
		}
		return false;
	}

	private bool AnalyseLoopback(SpectrumLook look, bool stereo, bool needCentres)
	{
		var loopback = Host?.Loopback;
		if (loopback == null) return false;
		int rate = loopback.SampleRate;
		if (rate <= 0) rate = 48000;
		int size = Math.Min(65536, (look.Engine == 0) ? GaussianAnalyzer.WindowSize(rate) : SpectrumAnalyzer.WindowSize(look.FftSize, rate));
		EnsureBuffers(size, stereo);
		if (!loopback.ReadWindow(size, _left, _right, stereo, look.Channels, ref _lastLoopbackHead, out int actualRate, out bool fresh))
		{
			return false;
		}
		if (actualRate != _rate)
		{
			_rate = actualRate;
			_geo = null;
			needCentres = true;
		}
		_hasAudio = true;
		if (needCentres || _centres.Length != _bands) Centres(look);
		if (!fresh) return true;
		ProcessPcm(size, actualRate, look);
		return true;
	}

	private bool AnalyseApi(SpectrumLook look, bool needCentres)
	{
		if (!_plugin.ReadApiFft(out var magnitudes)) return false;
		_hasAudio = true;
		if (_rate != 44100)
		{
			_rate = 44100;
			needCentres = true;
		}
		if (needCentres || _centres.Length != _bands) Centres(look);
		for (int j = 0; j < _channels; j++)
		{
			if (look.Engine == 0)
			{
				GaussianAnalyzer.BandsFromMagnitudes(magnitudes, magnitudes.Length, 44100, _bands, _raw[j]);
			}
			else
			{
				_modern[j].FromMagnitudes(magnitudes, magnitudes.Length, 44100, look.Value, _bands, look.MinHz, TopHz(look), look.Scale, _raw[j]);
			}
		}
		return true;
	}

	private void EnsureBuffers(int size, bool stereo)
	{
		if (_left == null || _left.Length < size) _left = new float[size];
		if (stereo && (_right == null || _right.Length < size)) _right = new float[size];
	}

	private void ProcessPcm(int size, int rate, SpectrumLook look)
	{
		if (look.Engine != 0 && _channels == 2)
		{
			_modern[0].AnalyzeStereo(_left, _right, size, rate, look.Window, look.Value, _bands, look.MinHz, TopHz(look), look.Scale, _raw[0], _raw[1], _modern[1]);
		}
		else
		{
			for (int i = 0; i < _channels; i++)
			{
				float[] array = (i == 0) ? _left : _right;
				if (look.Engine == 0)
				{
					_classic[i].Analyze(array, size, rate, _bands, _raw[i]);
				}
				else
				{
					_modern[i].Analyze(array, size, rate, look.Window, look.Value, _bands, look.MinHz, TopHz(look), look.Scale, _raw[i]);
				}
			}
		}
	}
}
