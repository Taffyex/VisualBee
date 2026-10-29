using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView : GpuView
{
	private struct Particle
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Age;

		public float Life;

		public float Size;

		public Vec4 Colour;
	}

	private sealed class Geometry
	{
		public SpectrumLook Look;

		public int Width;

		public int Height;

		public int Bands;

		public int Rate;

		public float Left;

		public float Top;

		public float Right;

		public float Bottom;

		public float Base;

		public float Span;

		public float Reflection;

		public int Slots;

		public float[] X0;

		public float[] X1;

		public int[] Band;

		public int[] Channel;

		public float CentreX;

		public float CentreY;

		public float Inner;

		public float Outer;

		public float[] Angle;

		public float BarThickness;

		public float DbLeft;

		public float DbRight;

		public float FreqTop;

		public bool DbColumn;

		public bool FreqRow;

		public int Shown;
	}

	private const double FloorDb = -144.49;

	private const int MaxParticles = 4000;

	private static readonly double[] LabelFrequencies = new double[14]
	{
		20.0, 30.0, 50.0, 100.0, 200.0, 300.0, 500.0, 1000.0, 2000.0, 3000.0,
		5000.0, 10000.0, 15000.0, 20000.0
	};

	private readonly Plugin _plugin;

	private readonly GaussianAnalyzer[] _classic = new GaussianAnalyzer[2]
	{
		new GaussianAnalyzer(),
		new GaussianAnalyzer()
	};

	private readonly SpectrumAnalyzer[] _modern = new SpectrumAnalyzer[2]
	{
		new SpectrumAnalyzer(),
		new SpectrumAnalyzer()
	};

	private Plugin.WindowKey _window;

	private long _lastLoopbackHead = -1;

	private float[] _left;

	private float[] _right;

	private int _engine = -1;

	private int _analysedBands;

	private int _rate = 44100;

	private int _fftChoice = -1;

	private int _windowShape = -1;

	private int _value = -1;

	private int _scale = -1;

	private int _channelMode = -1;

	private double _minHz;

	private double _maxHz;

	private bool _stereo;

	private int _bands;

	private int _channels;

	private double[][] _raw = new double[2][];

	private double[][] _level = new double[2][];

	private double[][] _peak = new double[2][];

	private float[][] _peakHeld = new float[2][];

	private float[][] _peakSpeed = new float[2][];

	private float[][] _peakAlpha = new float[2][];

	private float[][] _rise = new float[2][];

	private double[] _centres = new double[0];

	private bool _hasAudio;

	private readonly BeatTracker _beat = new BeatTracker();

	private float _energy;

	private Geometry _geo;

	private readonly TextSheet _labels = new TextSheet();

	private IntPtr _labelFont;

	private double _labelFontSize;

	private bool _labelsBuilt;

	private readonly Particle[] _particles = new Particle[4000];

	private int _particleCount;

	private float[] _flow = new float[0];

	private uint _seed = 2654435769u;

	private float[] _curve = new float[0];

	private float[] _smooth = new float[0];

	private float[] _synth = new float[0];

	private float _analysisTimer;

	private Gdi.SIZE _sizeDb;

	private Gdi.SIZE _sizeFreq;

	private double[] _tiltOctaves = new double[0];

	protected override float Energy => _energy;

	protected override float Beat => _beat.Pulse;

	public SpectrumView(Plugin plugin, VisualiserHost host, bool fullScreen)
		: base(host, fullScreen, "Live Spectrum")
	{
		_plugin = plugin;
	}

	protected override bool Advance(ref Frame f, PlayState state)
	{
		SpectrumLook spectrumLook = (SpectrumLook)f.Look;
		bool stereo = spectrumLook.Stereo;
		int num = ((!stereo) ? 1 : 2);
		int num2 = Math.Max(8, Math.Min(512, spectrumLook.Bands));
		if (num2 != _bands || num != _channels)
		{
			Allocate(num2, num);
		}
		bool isPlaying = state == PlayState.Playing;
		bool isPaused = state == PlayState.Paused;
		bool isLoopback = spectrumLook.AudioSource == SpectrumLook.AudioSourceWasapiLoopback;
		bool isAuto = spectrumLook.AudioSource == SpectrumLook.AudioSourceAuto;
		bool hasLoopbackAudio = Host?.Loopback?.HasActiveAudio() == true;

		bool shouldAnalyse = isPlaying || ((isAuto || isLoopback) && hasLoopbackAudio);
		if (shouldAnalyse)
		{
			int targetHz = SpectrumLook.AnalysisHzRates[Math.Max(0, Math.Min(SpectrumLook.AnalysisHzRates.Length - 1, spectrumLook.AnalysisHz))];
			if (targetHz <= 0)
			{
				Analyse(spectrumLook, stereo);
			}
			else
			{
				float interval = 1f / targetHz;
				_analysisTimer += f.Dt;
				if (_analysisTimer >= interval - 0.0015f)
				{
					_analysisTimer = Math.Max(0f, _analysisTimer - interval);
					Analyse(spectrumLook, stereo);
				}
			}
		}
		else if (!isPaused && _hasAudio)
		{
			_hasAudio = false;
			_window = default;
			_lastLoopbackHead = -1;
			_plugin.StopAudio();
			for (int i = 0; i < _channels; i++)
			{
				Fill(_raw[i], -144.49);
			}
			_beat.Reset();
		}
		return Animate(spectrumLook, f.Dt, ref f);
	}

	private void Allocate(int bands, int channels)
	{
		_bands = bands;
		_channels = channels;
		for (int i = 0; i < 2; i++)
		{
			_raw[i] = new double[bands];
			_level[i] = new double[bands];
			_peak[i] = new double[bands];
			_peakHeld[i] = new float[bands];
			_peakSpeed[i] = new float[bands];
			_peakAlpha[i] = new float[bands];
			_rise[i] = new float[bands];
			Fill(_raw[i], -144.49);
			Fill(_level[i], -144.49);
			Fill(_peak[i], -144.49);
		}
		_flow = new float[bands * 2];
		_window = default;
		_lastLoopbackHead = -1;
		_geo = null;
	}

	private static void Fill(double[] a, double v)
	{
		for (int i = 0; i < a.Length; i++)
		{
			a[i] = v;
		}
	}

	protected override bool BackgroundNeedsRedraw(ref Frame f) => !_labelsBuilt;

	protected override void ReleaseResources()
	{
		_labels.Dispose();
		_labelsBuilt = false;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (_labelFont != IntPtr.Zero)
		{
			Gdi.DeleteObject(_labelFont);
			_labelFont = IntPtr.Zero;
		}
	}

	protected override void AddMenuItems(Menu.MenuItemCollection items)
	{
		items.Add("-");
		items.Add(ChoiceItem("&Layout", "layout"));
		items.Add(ChoiceItem("Bar &style", "bars.style"));
		items.Add(ToggleItem("Floor &reflection", "reflection.horizon"));
		items.Add(ToggleItem("Pea&ks", "peaks"));
		items.Add(ToggleItem("&Glow", "fx.bloom"));
		items.Add(ToggleItem("Pa&rticles", "particles"));
	}
}
