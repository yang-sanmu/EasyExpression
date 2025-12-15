using System;

namespace EasyExpression.Core.Engine.Functions
{
	internal static class ArgHelper
	{
		public static string GetString(object?[] args, int index)
		{
			return args[index]?.ToString() ?? string.Empty;
		}

		public static decimal GetDecimal(object?[] args, int index, InvocationContext ctx)
		{
			var v = args[index];
			if (ctx.Converters.TryConvert(v, typeof(decimal), out var d) && d is decimal dd) return dd;
			if (v is IConvertible)
			{
				try { return Convert.ToDecimal(v, System.Globalization.CultureInfo.InvariantCulture); } catch { }
			}
			throw new ArgumentException($"Cannot convert arg[{index}] to decimal");
		}

		public static int GetInt(object?[] args, int index, InvocationContext ctx)
		{
			var dec = GetDecimal(args, index, ctx);
			return Convert.ToInt32(dec);
		}

		public static double GetDouble(object?[] args, int index, InvocationContext ctx)
		{
			var dec = GetDecimal(args, index, ctx);
			return (double)dec;
		}

		public static bool GetBool(object?[] args, int index, InvocationContext ctx)
		{
			var v = args[index];
			if (v is bool b) return b;
			if (ctx.Converters.TryConvert(v, typeof(bool), out var res) && res is bool bb) return bb;
			throw new ArgumentException($"Cannot convert arg[{index}] to bool");
		}

		public static DateTime GetDateTime(object?[] args, int index, InvocationContext ctx)
		{
			var v = args[index];
			if (v is DateTime dt) return dt;
			if (ctx.Converters.TryConvert(v, typeof(DateTime), out var res) && res is DateTime d) return d;
			throw new ArgumentException($"Cannot convert arg[{index}] to DateTime");
		}
	}
}



