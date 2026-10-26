using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MusicBeePlugin
{
    internal static class IlScan
    {
        internal struct Instr
        {
            public OpCode Op;
            public int Operand;
            public int Offset;
        }

        private static readonly Dictionary<short, OpCode> Ops = BuildOps();

        private static Dictionary<short, OpCode> BuildOps()
        {
            var map = new Dictionary<short, OpCode>();
            foreach (FieldInfo f in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.GetValue(null) is OpCode op) map[op.Value] = op;
            }
            return map;
        }

        public static List<Instr> Decode(MethodBase method)
        {
            MethodBody body;
            try { body = method.GetMethodBody(); }
            catch { return null; }
            if (body == null) return null;

            byte[] il = body.GetILAsByteArray();
            if (il == null) return null;

            var list = new List<Instr>();
            int i = 0;
            while (i < il.Length)
            {
                int offset = i;
                short code = il[i++];
                if (code == 0xFE && i < il.Length)
                    code = (short)(0xFE00 | il[i++]);

                OpCode op;
                if (!Ops.TryGetValue(code, out op)) return list;

                int operand = 0;
                switch (op.OperandType)
                {
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        i++;
                        break;
                    case OperandType.InlineVar:
                        i += 2;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        i += 8;
                        break;
                    case OperandType.InlineSwitch:
                        int n = BitConverter.ToInt32(il, i);
                        i += 4 + 4 * n;
                        break;
                    case OperandType.InlineNone:
                        break;
                    default:
                        operand = BitConverter.ToInt32(il, i);
                        i += 4;
                        break;
                }
                list.Add(new Instr { Op = op, Operand = operand, Offset = offset });
            }
            return list;
        }

        public static MethodBase ResolveMethod(MethodBase context, int token)
        {
            try
            {
                Type[] typeArgs = context.DeclaringType != null && context.DeclaringType.IsGenericType
                    ? context.DeclaringType.GetGenericArguments() : null;
                Type[] methodArgs = context.IsGenericMethod ? context.GetGenericArguments() : null;
                return context.Module.ResolveMethod(token, typeArgs, methodArgs);
            }
            catch { return null; }
        }

        public static FieldInfo ResolveField(MethodBase context, int token)
        {
            try
            {
                Type[] typeArgs = context.DeclaringType != null && context.DeclaringType.IsGenericType
                    ? context.DeclaringType.GetGenericArguments() : null;
                Type[] methodArgs = context.IsGenericMethod ? context.GetGenericArguments() : null;
                return context.Module.ResolveField(token, typeArgs, methodArgs);
            }
            catch { return null; }
        }
    }
}
