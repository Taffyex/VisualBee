using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace MusicBeePlugin
{
    internal sealed partial class AudioTap
    {
        internal void BindMethod(MethodInfo api)
        {
            if (api == null || api.DeclaringType == null || api.Module.Assembly == typeof(AudioTap).Assembly)
            {
                Status = "MusicBee audio hook unavailable";
                return;
            }
            if (TryBindDirectReflection(api.Module.Assembly))
                return;
            if (!Search(api, null, -1, 0))
                Status = "MusicBee audio hook not found";
        }

        private bool TryBindDirectReflection(Assembly mbAssembly)
        {
            if (mbAssembly == null) return false;
            Type[] types;
            try { types = mbAssembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }
            catch { return false; }
            if (types == null) return false;

            Type engineType = null;
            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t != null && t.Name.IndexOf("zSjS0Qql2pEx", StringComparison.Ordinal) >= 0)
                {
                    engineType = t;
                    break;
                }
            }
            if (engineType == null) return false;

            FieldInfo playerField = null;
            FieldInfo[] fields = engineType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (f.Name.IndexOf("zBxDLMzNnx", StringComparison.Ordinal) >= 0)
                {
                    playerField = f;
                    break;
                }
            }
            if (playerField == null || playerField.FieldType == null) return false;

            FieldInfo handleField = null;
            FieldInfo[] playerFields = playerField.FieldType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < playerFields.Length; i++)
            {
                FieldInfo f = playerFields[i];
                if (f.FieldType == typeof(int) && f.Name.IndexOf("zOZnxfaY", StringComparison.Ordinal) >= 0)
                {
                    handleField = f;
                    break;
                }
            }
            if (handleField == null) return false;

            _playerField = playerField;
            _handleField = handleField;
            Status = "";
            return true;
        }

        private bool Search(MethodBase method, MethodBase caller, int callOffset, int depth)
        {
            List<IlScan.Instr> code = IlScan.Decode(method);
            if (code == null) return false;

            for (int i = 0; i < code.Count; i++)
            {
                IlScan.Instr instr = code[i];
                if (instr.Op != OpCodes.Call && instr.Op != OpCodes.Callvirt) continue;
                MethodBase target = IlScan.ResolveMethod(method, instr.Operand);
                if (target == null) continue;
                if (IsMixerGetData(target))
                    return BindHandle(method, code, i, caller, callOffset);
                if (depth < 3 && target.Module == method.Module && target != method &&
                    Search(target, method, instr.Offset, depth + 1))
                    return true;
            }
            return false;
        }

        private bool BindHandle(MethodBase wrapper, List<IlScan.Instr> code, int callIndex,
            MethodBase caller, int callOffset)
        {
            FieldInfo handle = null;
            for (int i = callIndex - 1; i >= 0 && handle == null; i--)
            {
                IlScan.Instr instr = code[i];
                if (instr.Op != OpCodes.Ldfld && instr.Op != OpCodes.Ldsfld) continue;
                FieldInfo f = IlScan.ResolveField(wrapper, instr.Operand);
                if (f != null && f.FieldType == typeof(int)) handle = f;
            }
            if (handle == null) return false;

            if (handle.IsStatic)
            {
                _handleField = handle;
                Status = "";
                return true;
            }

            if (caller == null) return false;
            List<IlScan.Instr> callerCode = IlScan.Decode(caller);
            if (callerCode == null) return false;

            Type declaring = handle.DeclaringType;
            for (int i = callerCode.Count - 1; i >= 0; i--)
            {
                IlScan.Instr instr = callerCode[i];
                if (instr.Offset >= callOffset || instr.Op != OpCodes.Ldsfld) continue;
                FieldInfo f = IlScan.ResolveField(caller, instr.Operand);
                if (f != null && f.IsStatic && declaring.IsAssignableFrom(f.FieldType))
                {
                    _playerField = f;
                    _handleField = handle;
                    Status = "";
                    return true;
                }
            }
            return false;
        }

        private static bool IsMixerGetData(MethodBase m)
        {
            if ((m.Attributes & MethodAttributes.PinvokeImpl) == 0) return false;
            object[] attrs = m.GetCustomAttributes(typeof(DllImportAttribute), false);
            string name = m.Name;
            if (attrs.Length > 0)
            {
                var dll = (DllImportAttribute)attrs[0];
                if (!string.IsNullOrEmpty(dll.EntryPoint)) name = dll.EntryPoint;
            }
            return name == "BASS_Mixer_ChannelGetData";
        }
    }
}
