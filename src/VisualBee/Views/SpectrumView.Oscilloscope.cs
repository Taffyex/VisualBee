using System;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private float _waveMax = 0.5f;
	private float[] _reflCurve;

	private void DrawParticles(Canvas c, SpectrumLook look, ref Frame f)
	{
		if (_particleCount != 0)
		{
			c.Blend = BlendMode.Add;
			for (int i = 0; i < _particleCount; i++)
			{
				Particle particle = _particles[i];
				float num = 1f - particle.Age / particle.Life;
				Vec4 colour = particle.Colour.Faded(num * num);
				float streakX = (look.ParticleSparks ? (particle.Vx * 0.035f) : 0f);
				float streakY = (look.ParticleSparks ? (particle.Vy * 0.035f) : 0f);
				c.Sprite(SpriteShape.Make(particle.X, particle.Y, particle.Size, SpriteKind.Glow, colour, streakX, streakY));
			}
			c.Blend = BlendMode.Over;
		}
	}

	private void DrawOscilloscope(Canvas c, SpectrumLook look, Geometry g, ref Frame f)
	{
		float left = g.Left;
		float top = g.Top;
		float width = g.Right - g.Left;
		float height = g.Bottom - g.Top;
		float centerY = top + height * 0.5f;

		if (look.ScopeGraticule)
		{
			Vec4 graticuleCol = f.Colours[10].Faded(0.18f);
			c.Line(LineShape.Make(left, centerY, left + width, centerY, 1f, graticuleCol, graticuleCol, LineCap.Butt));
			float centerX = left + width * 0.5f;
			c.Line(LineShape.Make(centerX, top, centerX, top + height, 1f, graticuleCol, graticuleCol, LineCap.Butt));
			float tickSpacing = width / 10f;
			for (float tx = left + tickSpacing; tx < left + width - 1f; tx += tickSpacing)
			{
				c.Line(LineShape.Make(tx, centerY - 3f, tx, centerY + 3f, 1f, graticuleCol, graticuleCol, LineCap.Butt));
			}
			float tickYSpacing = height / 8f;
			for (float ty = top + tickYSpacing; ty < top + height - 1f; ty += tickYSpacing)
			{
				c.Line(LineShape.Make(centerX - 3f, ty, centerX + 3f, ty, 1f, graticuleCol, graticuleCol, LineCap.Butt));
			}
		}

		float[] samples = _left;
		if (samples == null || samples.Length == 0)
		{
			if (_synth.Length < 1024)
			{
				_synth = new float[1024];
			}
			samples = _synth;
			float t = f.Time * 14f;
			float bass = (_bands > 4 && _level[0] != null) ? (float)Norm(look, _level[0][1]) : 0.5f;
			float mid = (_bands > 16 && _level[0] != null) ? (float)Norm(look, _level[0][8]) : 0.4f;
			float treble = (_bands > 32 && _level[0] != null) ? (float)Norm(look, _level[0][24]) : 0.2f;
			for (int k = 0; k < 1024; k++)
			{
				float p = (float)k / 1024f * 24f + t;
				samples[k] = (float)(Math.Sin(p) * (0.12 + 0.68 * bass) + Math.Sin(p * 2.3) * (0.08 + 0.42 * mid) + Math.Sin(p * 4.1) * (0.04 + 0.22 * treble));
			}
		}
		int len = samples.Length;
		int triggerIdx = 0;
		float triggerFrac = 0f;
		int maxSearch = Math.Min(len / 4, 2048);
		for (int i = 1; i < maxSearch; i++)
		{
			if (samples[i - 1] <= 0f && samples[i] > 0f)
			{
				triggerIdx = i - 1;
				float d = samples[i] - samples[i - 1];
				triggerFrac = (d > 1e-6f) ? (-samples[i - 1] / d) : 0f;
				break;
			}
		}
		float trigger = triggerIdx + triggerFrac;

		int points = Math.Max(64, Math.Min(512, (int)width));
		if (_curve.Length < points * 2)
		{
			_curve = new float[points * 2];
		}

		int windowSamples = Math.Max(64, Math.Min(len - (int)trigger - 2, points * 2));
		float step = (float)windowSamples / (points - 1);
		float ampScale = height * 0.44f;
		if (look.Flip)
		{
			ampScale = -ampScale;
		}

		float gain = (float)(look.WaveGain / 100.0);
		if (look.WaveNormalize)
		{
			float peak = 0.001f;
			for (int k = 0; k < len; k++)
			{
				float a = Math.Abs(samples[k]);
				if (a > peak) peak = a;
			}
			_waveMax = Math.Max(0.04f, _waveMax * 0.94f + peak * 0.06f);
			gain *= (0.75f / _waveMax);
		}

		for (int i = 0; i < points; i++)
		{
			float sampleIdx = trigger + i * step;
			int idx0 = Math.Min((int)sampleIdx, len - 1);
			int idx1 = Math.Min(idx0 + 1, len - 1);
			float frac = sampleIdx - (int)sampleIdx;
			float s = (samples[idx0] * (1f - frac) + samples[idx1] * frac) * gain;
			s = (float)Math.Tanh(s * 1.5f);

			float px = left + (float)i / (points - 1) * width;
			float py = centerY - s * ampScale;
			_curve[i * 2] = px;
			_curve[i * 2 + 1] = py;
		}

		Vec4 curveCol = f.Colours[15];
		if (curveCol.W <= 0.01f)
		{
			curveCol = f.Colours[11];
		}

		if (look.CurveFill > 0.0)
		{
			float fillAlpha = (float)(look.CurveFill / 100.0) * 0.35f;
			Vec4 fillCol = new Vec4(curveCol.X, curveCol.Y, curveCol.Z, curveCol.W * fillAlpha);
			Stroke fillStroke = Stroke.Solid(0f, fillCol);
			c.Area(_curve, points, fillStroke);
		}

		float beamWidth = (float)(look.CurveWidth * (double)f.Scale);
		if (beamWidth < 1f) beamWidth = 1.5f;

		if (look.WaveFloorReflection && g.Reflection > 0f)
		{
			float reflBase = centerY + ampScale * 0.9f;
			c.Blend = BlendMode.Over;
			if (_reflCurve == null || _reflCurve.Length < _curve.Length)
			{
				_reflCurve = new float[_curve.Length];
			}
			for (int i = 0; i < points; i++)
			{
				_reflCurve[i * 2] = _curve[i * 2];
				_reflCurve[i * 2 + 1] = reflBase + (reflBase - _curve[i * 2 + 1]) * 0.40f;
			}
			c.Polyline(_reflCurve, points, Stroke.Solid(beamWidth * 1.2f, curveCol.Faded(0.20f)));
		}

		if (look.ScopeStyle == 0)
		{
			c.Blend = BlendMode.Add;
			c.Polyline(_curve, points, Stroke.Solid(beamWidth * 3.5f, curveCol.Faded(0.25f)));
			c.Polyline(_curve, points, Stroke.Solid(beamWidth, curveCol));
			c.Blend = BlendMode.Over;
		}
		else
		{
			c.Blend = BlendMode.Add;
			for (int i = 0; i < points - 1; i++)
			{
				float x0 = _curve[i * 2];
				float y0 = _curve[i * 2 + 1];
				float x1 = _curve[(i + 1) * 2];
				float y1 = _curve[(i + 1) * 2 + 1];
				float dx = x1 - x0;
				float dy = y1 - y0;
				float slew = (float)Math.Sqrt(1.0 + 0.35 * (dy * dy) / (dx * dx + 1e-4f));
				float intensity = 1f / slew;
				Vec4 segCol = curveCol.Faded(intensity);
				c.Line(LineShape.Make(x0, y0, x1, y1, beamWidth, segCol, segCol, LineCap.Round).Beam());
			}
			c.Blend = BlendMode.Over;
		}
	}
}
