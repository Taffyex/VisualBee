using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MusicBeePlugin;

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
		Dictionary<short, OpCode> dictionary = new Dictionary<short, OpCode>();
		FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
		for (int i = 0; i < fields.Length; i++)
		{
			if (fields[i].GetValue(null) is OpCode value)
			{
				dictionary[value.Value] = value;
			}
		}
		return dictionary;
	}

	public static List<Instr> Decode(MethodBase m)
	{
		MethodBody methodBody;
		try
		{
			methodBody = m.GetMethodBody();
		}
		catch (Exception)
		{
			return null;
		}
		if (methodBody == null)
		{
			return null;
		}
		byte[] iLAsByteArray = methodBody.GetILAsByteArray();
		if (iLAsByteArray == null)
		{
			return null;
		}
		List<Instr> list = new List<Instr>();
		int num = 0;
		while (num < iLAsByteArray.Length)
		{
			int offset = num;
			short num2 = iLAsByteArray[num++];
			if (num2 == 254 && num < iLAsByteArray.Length)
			{
				num2 = (short)(0xFE00 | iLAsByteArray[num++]);
			}
			if (!Ops.TryGetValue(num2, out var value))
			{
				return list;
			}
			int operand = 0;
			switch (value.OperandType)
			{
			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				num++;
				break;
			case OperandType.InlineVar:
				num += 2;
				break;
			case OperandType.InlineI8:
			case OperandType.InlineR:
				num += 8;
				break;
			case OperandType.InlineSwitch:
			{
				int num3 = BitConverter.ToInt32(iLAsByteArray, num);
				num += 4 + 4 * num3;
				break;
			}
			default:
				operand = BitConverter.ToInt32(iLAsByteArray, num);
				num += 4;
				break;
			case OperandType.InlineNone:
				break;
			}
			list.Add(new Instr
			{
				Op = value,
				Operand = operand,
				Offset = offset
			});
		}
		return list;
	}

	public static MethodBase ResolveMethod(MethodBase context, int token)
	{
		try
		{
			Type[] genericTypeArguments = ((context.DeclaringType != null && context.DeclaringType.IsGenericType) ? context.DeclaringType.GetGenericArguments() : null);
			Type[] genericMethodArguments = (context.IsGenericMethod ? context.GetGenericArguments() : null);
			return context.Module.ResolveMethod(token, genericTypeArguments, genericMethodArguments);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static FieldInfo ResolveField(MethodBase context, int token)
	{
		try
		{
			Type[] genericTypeArguments = ((context.DeclaringType != null && context.DeclaringType.IsGenericType) ? context.DeclaringType.GetGenericArguments() : null);
			Type[] genericMethodArguments = (context.IsGenericMethod ? context.GetGenericArguments() : null);
			return context.Module.ResolveField(token, genericTypeArguments, genericMethodArguments);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
