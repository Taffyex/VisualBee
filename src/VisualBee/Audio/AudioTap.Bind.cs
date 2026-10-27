using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed partial class AudioTap
{
	public void Bind(Delegate spectrumApi)
	{
		lock (this)
		{
			Polls++;
			if (_bindTried)
			{
				return;
			}
			_bindTried = true;
			try
			{
				if ((object)spectrumApi == null)
				{
					Status = "spectrum API missing";
				}
				else
				{
					BindMethod(spectrumApi.Method);
				}
			}
			catch (Exception ex)
			{
				_playerField = null;
				_handleField = null;
				Status = "MusicBee audio hook failed: " + ex.GetType().Name;
			}
		}
	}

	internal void BindMethod(MethodInfo api)
	{
		if (api == null || api.DeclaringType == null || api.Module.Assembly == typeof(AudioTap).Assembly)
		{
			Status = "MusicBee audio hook unavailable";
		}
		else if (!Search(api, null, -1, 0))
		{
			Status = "MusicBee audio hook not found";
		}
	}

	internal string Describe()
	{
		if (_handleField == null)
		{
			return "unbound";
		}
		return ((_playerField != null) ? (_playerField.DeclaringType.FullName + "::" + _playerField.Name + " -> ") : "") + _handleField.DeclaringType.FullName + "::" + _handleField.Name;
	}

	private bool Search(MethodBase method, MethodBase caller, int callOffset, int depth)
	{
		List<IlScan.Instr> list = IlScan.Decode(method);
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			IlScan.Instr instr = list[i];
			if (instr.Op != OpCodes.Call && instr.Op != OpCodes.Callvirt)
			{
				continue;
			}
			MethodBase methodBase = IlScan.ResolveMethod(method, instr.Operand);
			if (!(methodBase == null))
			{
				if (IsMixerGetData(methodBase))
				{
					return BindHandle(method, list, i, caller, callOffset);
				}
				if (depth < 3 && methodBase.Module == method.Module && methodBase != method && Search(methodBase, method, instr.Offset, depth + 1))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool BindHandle(MethodBase wrapper, List<IlScan.Instr> code, int callIndex, MethodBase caller, int callOffset)
	{
		FieldInfo fieldInfo = null;
		int num = callIndex - 1;
		while (num >= 0 && fieldInfo == null)
		{
			IlScan.Instr instr = code[num];
			if (!(instr.Op != OpCodes.Ldfld) || !(instr.Op != OpCodes.Ldsfld))
			{
				FieldInfo fieldInfo2 = IlScan.ResolveField(wrapper, instr.Operand);
				if (fieldInfo2 != null && fieldInfo2.FieldType == typeof(int))
				{
					fieldInfo = fieldInfo2;
				}
			}
			num--;
		}
		if (fieldInfo == null)
		{
			return false;
		}
		if (fieldInfo.IsStatic)
		{
			_handleField = fieldInfo;
			CompileHandleGetter();
			Status = "";
			return true;
		}
		if (caller == null)
		{
			return false;
		}
		List<IlScan.Instr> list = IlScan.Decode(caller);
		if (list == null)
		{
			return false;
		}
		Type declaringType = fieldInfo.DeclaringType;
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			IlScan.Instr instr2 = list[num2];
			if (instr2.Offset < callOffset && !(instr2.Op != OpCodes.Ldsfld))
			{
				FieldInfo fieldInfo3 = IlScan.ResolveField(caller, instr2.Operand);
				if (fieldInfo3 != null && fieldInfo3.IsStatic && declaringType.IsAssignableFrom(fieldInfo3.FieldType))
				{
					_playerField = fieldInfo3;
					_handleField = fieldInfo;
					CompileHandleGetter();
					Status = "";
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsMixerGetData(MethodBase m)
	{
		if ((m.Attributes & MethodAttributes.PinvokeImpl) == 0)
		{
			return false;
		}
		object[] customAttributes = m.GetCustomAttributes(typeof(DllImportAttribute), inherit: false);
		string text = m.Name;
		if (customAttributes.Length != 0)
		{
			DllImportAttribute dllImportAttribute = (DllImportAttribute)customAttributes[0];
			if (!string.IsNullOrEmpty(dllImportAttribute.EntryPoint))
			{
				text = dllImportAttribute.EntryPoint;
			}
		}
		return text == "BASS_Mixer_ChannelGetData";
	}

	private Func<int> _handleGetter;

	private void CompileHandleGetter()
	{
		try
		{
			DynamicMethod dm = new DynamicMethod("GetTrackHandle", typeof(int), null, typeof(AudioTap).Module, skipVisibility: true);
			ILGenerator il = dm.GetILGenerator();
			if (_playerField != null)
			{
				il.Emit(OpCodes.Ldsfld, _playerField);
				Label nullLabel = il.DefineLabel();
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Brfalse_S, nullLabel);
				il.Emit(OpCodes.Ldfld, _handleField);
				il.Emit(OpCodes.Ret);
				il.MarkLabel(nullLabel);
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ret);
			}
			else
			{
				il.Emit(OpCodes.Ldsfld, _handleField);
				il.Emit(OpCodes.Ret);
			}
			_handleGetter = (Func<int>)dm.CreateDelegate(typeof(Func<int>));
		}
		catch
		{
			_handleGetter = null;
		}
	}

	private int CurrentHandle()
	{
		if (_handleGetter != null)
		{
			return _handleGetter();
		}
		object obj = null;
		if (_playerField != null)
		{
			obj = _playerField.GetValue(null);
			if (obj == null) return 0;
		}
		object value = _handleField.GetValue(obj);
		return (value is int i) ? i : 0;
	}

	private int PickSource(int current)
	{
		int ringHandle = _ringHandle;
		if (ringHandle != 0 && ringHandle != current && Bass.MixerChannelGetPosition(current, 0) <= 0 && Mixing(ringHandle))
		{
			return ringHandle;
		}
		if (Mixing(current))
		{
			return current;
		}
		int num = Bass.MixerChannelGetMixer(current);
		if (num == 0)
		{
			return current;
		}
		int num2;
		try
		{
			num2 = Bass.MixerStreamGetChannels(num, _sources, _sources.Length);
		}
		catch (EntryPointNotFoundException)
		{
			return current;
		}
		for (int i = 0; i < num2 && i < _sources.Length; i++)
		{
			int num3 = _sources[i];
			if (num3 != current && Mixing(num3) && Bass.MixerChannelGetPosition(num3, 0) > 0)
			{
				return num3;
			}
		}
		return current;
	}

	private static bool Mixing(int handle)
	{
		if (!_noMixerIsActive)
		{
			try
			{
				return Bass.MixerChannelIsActive(handle) == 1;
			}
			catch (EntryPointNotFoundException)
			{
				_noMixerIsActive = true;
			}
		}
		if (Bass.ChannelIsActive(handle) == 1)
		{
			return (Bass.MixerChannelFlags(handle, 0, 0) & 0x20000) == 0;
		}
		return false;
	}

	private int ProbeDop(int handle, int freq, int chans)
	{
		int num = DsdConverter.FactorFor(freq);
		if (num == 0)
		{
			return 1;
		}
		if (handle == _probeHandle)
		{
			return _probeFactor;
		}
		if (_probe.Length < 1024 * chans)
		{
			_probe = new float[1024 * chans];
		}
		int num2 = Bass.MixerChannelGetData(handle, _probe, (1024 * chans * 4) | 0x40000000);
		int num3 = ((num2 > 0) ? (num2 / (4 * chans)) : 0);
		if (num3 < 64)
		{
			return 0;
		}
		_probeHandle = handle;
		_probeFactor = ((!DsdConverter.IsDop(_probe, num3, chans)) ? 1 : num);
		return _probeFactor;
	}

	private double OutputQueueSeconds()
	{
		if (QueueProbe != null)
		{
			return QueueProbe();
		}
		if (!_noWasapi)
		{
			try
			{
				if (Wasapi.IsStarted())
				{
					int data = Wasapi.GetData(IntPtr.Zero, 0);
					if (data >= 0 && Wasapi.GetInfo(out var info) && info.freq > 0 && info.chans > 0)
					{
						return (double)data / (double)(info.freq * info.chans * 4);
					}
				}
			}
			catch (DllNotFoundException)
			{
				_noWasapi = true;
			}
			catch (EntryPointNotFoundException)
			{
				_noWasapi = true;
			}
		}
		if (!_noAsio)
		{
			try
			{
				if (Asio.IsStarted())
				{
					int latency = Asio.GetLatency(input: false);
					double rate = Asio.GetRate();
					if (latency > 0 && rate > 0.0)
					{
						return (double)latency / rate;
					}
				}
			}
			catch (DllNotFoundException)
			{
				_noAsio = true;
			}
			catch (EntryPointNotFoundException)
			{
				_noAsio = true;
			}
		}
		return 0.0;
	}

}
