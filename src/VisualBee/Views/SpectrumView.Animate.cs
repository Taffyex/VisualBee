using System;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private double TopHz(SpectrumLook look)
	{
		return Math.Max(look.MinHz * 1.5, Math.Min(look.MaxHz, (double)_rate * 0.5));
	}

	private void Centres(SpectrumLook look)
	{
		if (_centres.Length != _bands)
		{
			_centres = new double[_bands];
		}
		if (_tiltOctaves == null || _tiltOctaves.Length != _bands)
		{
			_tiltOctaves = new double[_bands];
		}
		for (int i = 0; i < _bands; i++)
		{
			_centres[i] = ((look.Engine == 0) ? FoobarAnalyzer.BandFrequency(i, _bands) : SpectrumAnalyzer.ToHz(((double)i + 0.5) / (double)_bands, look.MinHz, TopHz(look), look.Scale));
			_tiltOctaves[i] = Math.Log(_centres[i] / 1000.0, 2.0);
		}
	}

	private bool Animate(SpectrumLook look, float dt, ref Frame f)
	{
		bool flag = false;
		double num = ((look.AttackMs > 0.0) ? (1.0 - Math.Exp((double)(0f - dt) * 1000.0 / look.AttackMs)) : 1.0);
		double num2 = ((look.ReleaseMs > 0.0) ? (1.0 - Math.Exp((double)(0f - dt) * 1000.0 / look.ReleaseMs)) : 1.0);
		double num3 = look.PeakHoldMs / 1000.0;
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = Math.Max(1.0, look.CeilingDb - look.FloorDb);
		bool flag2 = look.Tilt != 0.0 && _tiltOctaves != null && _tiltOctaves.Length == _bands;
		for (int i = 0; i < _channels; i++)
		{
			double[] array = _raw[i];
			double[] array2 = _level[i];
			double[] array3 = _peak[i];
			float[] array4 = _peakHeld[i];
			float[] array5 = _peakSpeed[i];
			float[] array6 = _peakAlpha[i];
			float[] array7 = _rise[i];
			for (int j = 0; j < _bands; j++)
			{
				double num7 = array[j];
				if (flag2 && num7 > -143.49)
				{
					num7 += look.Tilt * _tiltOctaves[j];
				}
				double num8 = array2[j];
				double num9 = num8 + (num7 - num8) * ((num7 > num8) ? num : num2);
				if (Math.Abs(num9 - num7) < 0.01)
				{
					num9 = num7;
				}
				if (num9 != num8)
				{
					flag = true;
				}
				array2[j] = num9;
				array7[j] = ((dt > 0f) ? ((float)((num9 - num8) / num6 / (double)dt)) : 0f);
				double num10 = array3[j];
				if (num9 >= num10)
				{
					array3[j] = num9;
					array4[j] = 0f;
					array5[j] = 0f;
					array6[j] = 1f;
					continue;
				}
				array4[j] += dt;
				if ((double)array4[j] > num3 && num10 > look.FloorDb - 1.0)
				{
					flag = true;
					if (look.PeakFall == 1)
					{
						array5[j] += (float)(look.PeakGravity * (double)dt);
						num10 -= (double)(array5[j] * dt);
					}
					else if (look.PeakFall == 2)
					{
						array6[j] -= (float)(look.PeakSpeed / 30.0 * (double)dt);
						if (array6[j] <= 0f)
						{
							array6[j] = 1f;
							num10 = num9;
						}
					}
					else
					{
						num10 -= look.PeakSpeed * (double)dt;
					}
					array3[j] = Math.Max(num10, num9);
				}
				else if ((double)array4[j] <= num3)
				{
					flag = true;
				}
			}
			for (int k = 0; k < _bands && k < _centres.Length; k++)
			{
				double num11 = Norm(look, array2[k]);
				num5 += num11;
				if (_centres[k] < 150.0)
				{
					num4 += Math.Pow(10.0, array2[k] / 10.0);
				}
			}
		}
		_energy = (float)(num5 / (double)Math.Max(1, _bands * _channels));
		_beat.Feed(num4, dt);
		if (_beat.Pulse > 0.01f)
		{
			flag = true;
		}
		EnsureGeometry(ref f);
		if (look.Particles || _particleCount > 0)
		{
			flag |= MoveParticles(look, dt, ref f);
		}
		if (look.Layout == 8) flag = true;
		return flag;
	}

	private static double Norm(SpectrumLook look, double db)
	{
		double num = (db - look.FloorDb) / Math.Max(1.0, look.CeilingDb - look.FloorDb);
		num *= (look.Sensitivity / 100.0);
		if (num <= 0.0) return 0.0;
		if (num >= 1.0) return 1.0;
		return num;
	}

	private float Random()
	{
		_seed ^= _seed << 13;
		_seed ^= _seed >> 17;
		_seed ^= _seed << 5;
		return (float)(_seed & 0xFFFFFF) / 16777216f;
	}

	private bool MoveParticles(SpectrumLook look, float dt, ref Frame f)
	{
		Geometry geo = _geo;
		float num = Math.Max(20f, geo.Span);
		float num2 = (float)(look.ParticleGravity / 100.0 * (double)num * 3.0);
		float num3 = (float)Math.Exp((double)(0f - dt) * 0.8);
		int num4 = 0;
		while (num4 < _particleCount)
		{
			Particle particle = _particles[num4];
			particle.Age += dt;
			if (particle.Age >= particle.Life)
			{
				_particles[num4] = _particles[--_particleCount];
				continue;
			}
			particle.Vx *= num3;
			particle.Vy = particle.Vy * num3 + num2 * dt;
			particle.X += particle.Vx * dt;
			particle.Y += particle.Vy * dt;
			_particles[num4] = particle;
			num4++;
		}
		if (!look.Particles || !_hasAudio)
		{
			return _particleCount > 0;
		}
		float amount = (float)(look.ParticleAmount / 100.0);
		float speed = (float)(look.ParticleSpeed / 100.0 * (double)num * 1.6);
		bool beat = _beat.Pulse >= 0.999f;
		for (int i = 0; i < geo.Slots; i++)
		{
			if (look.Layout == 4)
			{
				Emit(look, geo, i, 0, amount, speed, dt, beat, ref f);
				Emit(look, geo, i, 1, amount, speed, dt, beat, ref f);
			}
			else
			{
				Emit(look, geo, i, geo.Channel[i], amount, speed, dt, beat, ref f);
			}
		}
		return true;
	}

	private void Emit(SpectrumLook look, Geometry g, int slot, int channel, float amount, float speed, float dt, bool beat, ref Frame f)
	{
		int num = g.Band[slot];
		if (num >= _bands)
		{
			return;
		}
		float num2 = (float)Norm(look, _level[channel][num]);
		float num4;
		switch (look.Emit)
		{
		case 1:
			num4 = (beat ? (amount * num2 * num2 * 6f) : 0f);
			break;
		case 2:
		{
			int num5 = slot * 2 + channel;
			if (num5 >= _flow.Length)
			{
				return;
			}
			_flow[num5] += amount * num2 * num2 * num2 * 30f * dt;
			num4 = (float)Math.Floor(_flow[num5]);
			_flow[num5] -= num4;
			break;
		}
		default:
		{
			float num3 = _rise[channel][num] - 1.5f;
			num4 = ((num3 > 0f) ? (amount * num3 * dt * 30f) : 0f);
			break;
		}
		}
		if (num4 < 1f && Random() > num4)
		{
			return;
		}
		int num6 = Math.Max(1, (int)num4);
		Tip(g, look, slot, channel, num2, out var num7, out var num8, out var dx, out var dy);
		Vec4 colour = (look.ParticleBarColour ? TipColour(look, f.Colours, g, slot, num, num2) : f.Colours[14]);
		float num9 = (float)(look.ParticleLife / 1000.0);
		for (int i = 0; i < num6; i++)
		{
			if (_particleCount >= 4000)
			{
				break;
			}
			float num10 = (Random() - 0.5f) * 1.1f;
			float num11 = speed * (0.35f + Random() * 0.9f) * (0.5f + num2);
			Particle particle = new Particle
			{
				X = num7 + (Random() - 0.5f) * Math.Max(2f, (g.X1.Length > slot) ? (g.X1[slot] - g.X0[slot]) : g.BarThickness),
				Y = num8,
				Vx = (dx - dy * num10) * num11,
				Vy = (dy + dx * num10) * num11,
				Life = num9 * (0.6f + Random() * 0.6f),
				Size = (float)(look.ParticleSize * (double)f.Scale * (0.6 + (double)Random() * 0.8)),
				Colour = colour
			};
			_particles[_particleCount++] = particle;
		}
	}

	private static void Tip(Geometry g, SpectrumLook look, int slot, int channel, float v, out float x, out float y, out float dx, out float dy)
	{
		if (look.Layout == 6)
		{
			float num = g.Angle[slot];
			dx = (float)Math.Sin(num);
			dy = 0f - (float)Math.Cos(num);
			float num2 = g.Inner + v * g.Span;
			x = g.CentreX + dx * num2;
			y = g.CentreY + dy * num2;
		}
		else
		{
			x = (g.X0[slot] + g.X1[slot]) / 2f;
			bool flag = (look.Flip && look.Layout != 1 && look.Layout != 4) || (look.Layout == 4 && channel == 1);
			dx = 0f;
			dy = (flag ? 1f : (-1f));
			y = g.Base + dy * v * g.Span;
		}
	}

}
