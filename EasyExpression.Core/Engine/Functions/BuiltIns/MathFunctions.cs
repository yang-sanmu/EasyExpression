using System;
using System.Globalization;
using System.Linq;

namespace EasyExpression.Core.Engine.Functions.BuiltIns
{
	internal static class DecimalUtil
	{
		public static decimal ToDecimal(object? v)
		{
			if (v == null) throw new ArgumentException("Null cannot convert to decimal");
			if (v is decimal d) return d;
			if (v is int i) return i;
			if (v is long l) return l;
			if (v is double db) return Convert.ToDecimal(db);
			if (v is float f) return Convert.ToDecimal(f);
			if (v is string s)
			{
				if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var rd)) return rd;
				throw new ArgumentException($"Cannot parse decimal: {s}");
			}
			throw new ArgumentException($"Unsupported number type: {v.GetType().FullName}");
		}
	}

    public sealed class ToDecimalFunction : IFunction
    {
        public string Name => "ToDecimal";
        public object? Invoke(object?[] args, InvocationContext ctx)
        {
            if (args.Length != 1) throw new ArgumentException("ToDecimal expects 1 arg");
			return DecimalUtil.ToDecimal(args[0]);
        }
    }

    public sealed class MaxFunction : IFunction
	{
		public string Name => "Max";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length == 0) throw new ArgumentException("Max expects at least 1 arg");
			return args.Select(DecimalUtil.ToDecimal).Max();
		}
	}

	public sealed class MinFunction : IFunction
	{
		public string Name => "Min";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length == 0) throw new ArgumentException("Min expects at least 1 arg");
			return args.Select(DecimalUtil.ToDecimal).Min();
		}
	}

	public sealed class AverageFunction : IFunction
	{
		public string Name => "Average";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length == 0) throw new ArgumentException("Average expects at least 1 arg");
			return args.Select(DecimalUtil.ToDecimal).Average();
		}
	}

	public sealed class SumFunction : IFunction
	{
		public string Name => "Sum";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length == 0) throw new ArgumentException("Sum expects at least 1 arg");
			return args.Select(DecimalUtil.ToDecimal).Sum();
		}
	}

	public sealed class RoundFunction : IFunction
	{
		public string Name => "Round";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 1 || args.Length > 2) throw new ArgumentException("Round expects 1 or 2 args");
			var value = DecimalUtil.ToDecimal(args[0]);
			var digits = args.Length == 2 ? Convert.ToInt32(DecimalUtil.ToDecimal(args[1])) : ctx.Options.RoundingDigits;
			return Math.Round(value, digits, ctx.Options.MidpointRounding);
		}
	}

	public sealed class AbsFunction : IFunction
	{
		public string Name => "Abs";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 1) throw new ArgumentException("Abs expects 1 arg");
			var value = DecimalUtil.ToDecimal(args[0]);
			return Math.Abs(value);
		}
	}
}


