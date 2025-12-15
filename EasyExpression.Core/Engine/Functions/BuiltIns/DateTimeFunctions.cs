using System;
using System.Globalization;

namespace EasyExpression.Core.Engine.Functions.BuiltIns
{
	internal static class DateUtil
	{
		public static DateTime ToDateTime(object? v, string format)
		{
			if (v == null) throw new ArgumentException("Null cannot convert to DateTime");
			if (v is DateTime dt) return dt;
			if (v is string s)
			{
				if (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
				throw new ArgumentException($"Cannot parse datetime: {s}");
			}
			throw new ArgumentException($"Unsupported datetime type: {v.GetType().FullName}");
		}

		public static decimal ToDecimal(object? v) => DecimalUtil.ToDecimal(v);
	}

	public sealed class ToDateTimeFunction : IFunction
	{
		public string Name => "ToDateTime";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 1) throw new ArgumentException("ToDateTime expects 1 arg");
			return DateUtil.ToDateTime(args[0], ctx.Options.DateTimeFormat);
		}
	}

	public sealed class AddDaysFunction : IFunction
	{
		public string Name => "AddDays";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 2) throw new ArgumentException("AddDays expects 2 args");
			var dt = DateUtil.ToDateTime(args[0], ctx.Options.DateTimeFormat);
			var days = (double)DateUtil.ToDecimal(args[1]);
			return dt.AddDays(days);
		}
	}

	public sealed class AddDayFunction : IFunction
	{
		public string Name => "AddDay";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 2) throw new ArgumentException("AddDay expects 2 args");
			var dt = DateUtil.ToDateTime(args[0], ctx.Options.DateTimeFormat);
			var days = (double)DateUtil.ToDecimal(args[1]);
			return dt.AddDays(days);
		}
	}

	public sealed class AddMinutesFunction : IFunction
	{
		public string Name => "AddMinutes";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 2) throw new ArgumentException("AddMinutes expects 2 args");
			var dt = DateUtil.ToDateTime(args[0], ctx.Options.DateTimeFormat);
			var minutes = (double)DateUtil.ToDecimal(args[1]);
			return dt.AddMinutes(minutes);
		}
	}

	public sealed class AddHoursFunction : IFunction
	{
		public string Name => "AddHours";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 2) throw new ArgumentException("AddHours expects 2 args");
			var dt = DateUtil.ToDateTime(args[0], ctx.Options.DateTimeFormat);
			var hours = (double)DateUtil.ToDecimal(args[1]);
			return dt.AddHours(hours);
		}
	}

	public sealed class AddSecondsFunction : IFunction
	{
		public string Name => "AddSeconds";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 2) throw new ArgumentException("AddSeconds expects 2 args");
			var dt = DateUtil.ToDateTime(args[0], ctx.Options.DateTimeFormat);
			var seconds = (double)DateUtil.ToDecimal(args[1]);
			return dt.AddSeconds(seconds);
		}
	}

	public sealed class TimeSpanFunction : IFunction
	{
		public string Name => "TimeSpan";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 2 || args.Length > 3) throw new ArgumentException("TimeSpan expects 2 or 3 args");
			var dt1 = DateUtil.ToDateTime(args[0], ctx.Options.DateTimeFormat);
			var dt2 = DateUtil.ToDateTime(args[1], ctx.Options.DateTimeFormat);
			var type = args.Length == 3 ? (args[2]?.ToString() ?? string.Empty) : "h";
			var span = dt2 - dt1;
			switch (type)
			{
				case "ms":
					return (decimal)span.TotalMilliseconds;
				case "s":
					return (decimal)span.TotalSeconds;
				case "m":
					return (decimal)span.TotalMinutes;
				case "h":
					return (decimal)span.TotalHours;
				case "d":
					return (decimal)span.TotalDays;
				case "":
					return (decimal)span.TotalHours; // Default: h
				default:
					throw new ArgumentException($"Unknown totalType: {type}");
			}
		}
	}

	public sealed class FormatDateTimeFunction : IFunction
	{
		public string Name => "FormatDateTime";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 1 || args.Length > 2) throw new ArgumentException("FormatDateTime expects 1 or 2 args");
			var dt = ArgHelper.GetDateTime(args, 0, ctx);
			var fmt = args.Length == 2 ? ArgHelper.GetString(args, 1) : ctx.Options.DateTimeFormat;
			return dt.ToString(fmt);
		}
	}
}


