using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MusicBeePlugin;

internal sealed partial class WasapiLoopback
{
	internal struct DeviceInfo
	{
		public string Id;
		public string Name;
		public bool IsCapture;

		public DeviceInfo(string id, string name, bool isCapture = false)
		{
			Id = id;
			Name = name;
			IsCapture = isCapture;
		}
	}

	[ComImport]
	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	private class MMDeviceEnumeratorComObject { }

	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDeviceEnumerator
	{
		[PreserveSig] int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);
		[PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
		[PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
		int RegisterEndpointNotificationCallback(IntPtr pClient);
		int UnregisterEndpointNotificationCallback(IntPtr pClient);
	}

	[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDeviceCollection
	{
		[PreserveSig] int GetCount(out uint pcDevices);
		[PreserveSig] int Item(uint nDevice, out IMMDevice ppDevice);
	}

	[Guid("1BE09788-68F6-41ea-96DE-0DF0BDDEE341"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMEndpoint
	{
		[PreserveSig] int GetDataFlow(out int pDataFlow);
	}

	[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDevice
	{
		[PreserveSig] int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
		[PreserveSig] int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
		[PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
		[PreserveSig] int GetState(out int pdwState);
	}

	[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IPropertyStore
	{
		[PreserveSig] int GetCount(out uint cProps);
		[PreserveSig] int GetAt(uint iProp, out PropertyKey pkey);
		[PreserveSig] int GetValue(ref PropertyKey key, out PropVariant pv);
		[PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant propvar);
		[PreserveSig] int Commit();
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct PropertyKey
	{
		public Guid fmtid;
		public uint pid;

		public PropertyKey(Guid id, uint p)
		{
			fmtid = id;
			pid = p;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct PropVariant
	{
		[FieldOffset(0)] public ushort vt;
		[FieldOffset(2)] public ushort wReserved1;
		[FieldOffset(4)] public ushort wReserved2;
		[FieldOffset(6)] public ushort wReserved3;
		[FieldOffset(8)] public IntPtr p;
	}

	[DllImport("ole32.dll")]
	private static extern int PropVariantClear(ref PropVariant pvar);

	[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("mmdevapi.dll", ExactSpelling = true)]
	private static extern int ActivateAudioInterfaceAsync(
		[MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
		ref Guid riid,
		IntPtr activationParams,
		IActivateAudioInterfaceCompletionHandler completionHandler,
		out IActivateAudioInterfaceAsyncOperation activationOperation);

	[Guid("41D82D8A-5FB3-4B26-9826-62A829705E19"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IActivateAudioInterfaceCompletionHandler
	{
		[PreserveSig] int ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation);
	}

	[Guid("72A2E436-4F24-4714-AB16-94BE9A0B7339"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IActivateAudioInterfaceAsyncOperation
	{
		[PreserveSig] int GetActivateResult(out int activateResult, [MarshalAs(UnmanagedType.IUnknown)] out object activatedInterface);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct AUDIOCLIENT_ACTIVATION_PARAMS
	{
		public int ActivationType;
		public AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS ProcessLoopbackParams;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS
	{
		public uint TargetProcessId;
		public int ProcessLoopbackMode;
	}

	private sealed class ProcessLoopbackHandler : IActivateAudioInterfaceCompletionHandler
	{
		public readonly ManualResetEvent Done = new ManualResetEvent(false);
		public int Result = -1;
		public object ActivatedInterface;

		public int ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
		{
			try
			{
				if (activateOperation != null)
				{
					activateOperation.GetActivateResult(out Result, out ActivatedInterface);
				}
			}
			catch (Exception ex)
			{
				Result = ex.HResult;
			}
			finally
			{
				Done.Set();
			}
			return 0;
		}
	}

	private static readonly PropertyKey PKeyDeviceFriendlyName = new PropertyKey(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);
	private static readonly object _nameCacheLock = new object();
	private static readonly Dictionary<string, string> _displayNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool IsProcessLoopbackSupported()
	{
		try
		{
			var os = Environment.OSVersion.Version;
			if (os.Major < 10 || (os.Major == 10 && os.Build < 20348))
			{
				return false;
			}
			IntPtr hMod = GetModuleHandle("mmdevapi.dll");
			if (hMod == IntPtr.Zero) return false;
			return GetProcAddress(hMod, "ActivateAudioInterfaceAsync") != IntPtr.Zero;
		}
		catch
		{
			return false;
		}
	}

	public static List<DeviceInfo> EnumerateDevices(string currentSelectedId = null)
	{
		var list = new List<DeviceInfo>();
		list.Add(new DeviceInfo("default", "Default Playback Device (All System Audio)"));
		if (IsProcessLoopbackSupported())
		{
			list.Add(new DeviceInfo("process", "MusicBee & Browser2 Only (App Process Loopback)"));
		}

		IMMDeviceEnumerator enumerator = null;
		try
		{
			enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
			EnumerateFlow(enumerator, 0, list, false);
			EnumerateFlow(enumerator, 1, list, true);
		}
		catch { }
		finally
		{
			if (enumerator != null)
			{
				try { Marshal.ReleaseComObject(enumerator); } catch { }
			}
		}

		if (!string.IsNullOrEmpty(currentSelectedId) &&
		    !string.Equals(currentSelectedId, "default", StringComparison.OrdinalIgnoreCase) &&
		    !string.Equals(currentSelectedId, "process", StringComparison.OrdinalIgnoreCase))
		{
			bool found = false;
			for (int i = 0; i < list.Count; i++)
			{
				if (string.Equals(list[i].Id, currentSelectedId, StringComparison.OrdinalIgnoreCase))
				{
					found = true;
					break;
				}
			}
			if (!found)
			{
				string cachedName = GetDeviceDisplayName(currentSelectedId);
				list.Add(new DeviceInfo(currentSelectedId, cachedName + " (Disconnected — using Default)"));
			}
		}

		return list;
	}

	private static void EnumerateFlow(IMMDeviceEnumerator enumerator, int dataFlow, List<DeviceInfo> list, bool isCapture)
	{
		IMMDeviceCollection col = null;
		try
		{
			if (enumerator.EnumAudioEndpoints(dataFlow, 1, out col) != 0 || col == null) return;
			if (col.GetCount(out uint count) != 0) return;
			for (uint i = 0; i < count; i++)
			{
				IMMDevice dev = null;
				try
				{
					if (col.Item(i, out dev) != 0 || dev == null) continue;
					if (dev.GetId(out string devId) != 0 || string.IsNullOrEmpty(devId)) continue;
					string name = GetDeviceFriendlyName(dev);
					string displayName = isCapture ? (name + " [Input]") : name;
					lock (_nameCacheLock)
					{
						_displayNameCache[devId] = displayName;
					}
					list.Add(new DeviceInfo(devId, displayName, isCapture));
				}
				finally
				{
					if (dev != null)
					{
						try { Marshal.ReleaseComObject(dev); } catch { }
					}
				}
			}
		}
		finally
		{
			if (col != null)
			{
				try { Marshal.ReleaseComObject(col); } catch { }
			}
		}
	}

	public static string GetDeviceDisplayName(string id)
	{
		if (string.IsNullOrEmpty(id) || string.Equals(id, "default", StringComparison.OrdinalIgnoreCase))
		{
			return "Default Playback Device";
		}
		if (string.Equals(id, "process", StringComparison.OrdinalIgnoreCase))
		{
			return "MusicBee & Browser2 Only";
		}
		lock (_nameCacheLock)
		{
			if (_displayNameCache.TryGetValue(id, out var name))
			{
				return name;
			}
		}
		string friendly = QueryDeviceFriendlyName(id);
		if (!string.IsNullOrEmpty(friendly))
		{
			lock (_nameCacheLock) { _displayNameCache[id] = friendly; }
			return friendly;
		}
		return "Audio Device";
	}

	private static string QueryDeviceFriendlyName(string id)
	{
		IMMDeviceEnumerator enumerator = null;
		IMMDevice dev = null;
		try
		{
			enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
			if (enumerator.GetDevice(id, out dev) == 0 && dev != null)
			{
				return GetDeviceFriendlyName(dev);
			}
		}
		catch { }
		finally
		{
			if (dev != null) { try { Marshal.ReleaseComObject(dev); } catch { } }
			if (enumerator != null) { try { Marshal.ReleaseComObject(enumerator); } catch { } }
		}
		return null;
	}

	private static string GetDeviceFriendlyName(IMMDevice dev)
	{
		IPropertyStore store = null;
		try
		{
			if (dev.OpenPropertyStore(0, out store) == 0 && store != null)
			{
				var key = PKeyDeviceFriendlyName;
				if (store.GetValue(ref key, out var pv) == 0 && pv.vt == 31 && pv.p != IntPtr.Zero)
				{
					string fn = Marshal.PtrToStringUni(pv.p);
					PropVariantClear(ref pv);
					if (!string.IsNullOrWhiteSpace(fn)) return fn.Trim();
				}
			}
		}
		catch { }
		finally
		{
			if (store != null)
			{
				try { Marshal.ReleaseComObject(store); } catch { }
			}
		}
		return "Audio Device";
	}

	private static bool TryActivateProcessLoopback(out IAudioClient client)
	{
		client = null;
		if (!IsProcessLoopbackSupported()) return false;

		IntPtr pParams = IntPtr.Zero;
		IActivateAudioInterfaceAsyncOperation op = null;
		ProcessLoopbackHandler handler = new ProcessLoopbackHandler();
		try
		{
			var pars = new AUDIOCLIENT_ACTIVATION_PARAMS
			{
				ActivationType = 1,
				ProcessLoopbackParams = new AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS
				{
					TargetProcessId = (uint)Process.GetCurrentProcess().Id,
					ProcessLoopbackMode = 0
				}
			};
			pParams = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(AUDIOCLIENT_ACTIVATION_PARAMS)));
			Marshal.StructureToPtr(pars, pParams, false);

			Guid iid = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
			int hr = ActivateAudioInterfaceAsync(@"VAD\Process_Loopback", ref iid, pParams, handler, out op);
			if (hr != 0) return false;

			if (handler.Done.WaitOne(1000) && handler.Result == 0 && handler.ActivatedInterface is IAudioClient ac)
			{
				client = ac;
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
		finally
		{
			if (pParams != IntPtr.Zero) Marshal.FreeCoTaskMem(pParams);
			if (op != null)
			{
				try { Marshal.ReleaseComObject(op); } catch { }
			}
			handler.Done.Dispose();
		}
	}
}
