using System;

namespace MusicBeePlugin;

internal sealed class ColourAnimator
{
	private Vec4[] _base = new Vec4[0];

	private Vec4[] _target = new Vec4[0];

	private bool[] _settled = new bool[0];

	private bool _primed;

	public Vec4[] Shown = new Vec4[0];

	public bool Moving;

	public void Update(Look look, Look art, float dt, double time)
	{
		int num = look.Colours.Length;
		if (_base.Length != num)
		{
			_base = new Vec4[num];
			_target = new Vec4[num];
			_settled = new bool[num];
			Shown = new Vec4[num];
			_primed = false;
		}
		float num2 = ((art != null && look.ArtColours) ? ((float)(look.ArtColoursAmount / 100.0)) : 0f);
		float num3 = ((look.TransitionMs <= 0.0 || !_primed) ? 1f : (1f - (float)Math.Exp((double)(0f - dt) * 3000.0 / look.TransitionMs)));
		bool moving = false;
		for (int i = 0; i < num; i++)
		{
			Vec4 vec = Vec4.Colour(look.Colours[i]);
			if (num2 > 0f && i != 6)
			{
				Vec4 vec2 = Vec4.Colour(art.Colours[i]);
				vec = ColourMath.Mix(vec, new Vec4(vec2.X, vec2.Y, vec2.Z, vec.W), num2);
			}
			if (!Same(vec, _target[i]))
			{
				_target[i] = vec;
				_settled[i] = false;
			}
			if (!_settled[i])
			{
				Vec4 vec3 = ((num3 >= 1f) ? vec : ColourMath.Mix(_base[i], vec, num3));
				if (Close(vec3, vec))
				{
					vec3 = vec;
					_settled[i] = true;
				}
				else
				{
					moving = true;
				}
				_base[i] = vec3;
			}
		}
		_primed = true;
		if (look.HueCycle)
		{
			double degrees = time * look.HueSpeed % 360.0;
			for (int j = 0; j < num; j++)
			{
				bool flag = j == 0 || j == 1 || j == 2 || j == 7 || j == 8 || j == 9;
				Shown[j] = ((j == 6 || (flag && !look.HueCycleBackground)) ? _base[j] : ColourMath.RotateHue(_base[j], degrees));
			}
			moving = true;
		}
		else
		{
			Array.Copy(_base, Shown, num);
		}
		Moving = moving;
	}

	private static bool Same(Vec4 a, Vec4 b)
	{
		if (a.X == b.X && a.Y == b.Y && a.Z == b.Z)
		{
			return a.W == b.W;
		}
		return false;
	}

	private static bool Close(Vec4 a, Vec4 b)
	{
		if (Math.Abs(a.X - b.X) < 0.0023529413f && Math.Abs(a.Y - b.Y) < 0.0023529413f && Math.Abs(a.Z - b.Z) < 0.0023529413f)
		{
			return Math.Abs(a.W - b.W) < 0.0023529413f;
		}
		return false;
	}
}
