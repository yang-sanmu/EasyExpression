using System;
using System.Text.RegularExpressions;

namespace EasyExpression.Core.Engine.Functions.BuiltIns
{
	internal static class StringHelper
	{
		public static StringComparison ResolveComparison(InvocationContext ctx, bool hasIgnoreCaseFlag)
		{
			if (hasIgnoreCaseFlag) return StringComparison.OrdinalIgnoreCase;
			return ctx.Options.StringComparison;
		}
	}
    public sealed class ToStringFunction : IFunction
	{
        public string Name => "ToString";
        public object? Invoke(object?[] args, InvocationContext ctx)
        {
            if (args.Length != 1) throw new ArgumentException("ToString expects 1 arg");
            return args[0]?.ToString() ?? string.Empty;
        }
    }

    public sealed class StartsWithFunction : IFunction
	{
		public string Name => "StartsWith";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 2 || args.Length > 3) throw new ArgumentException("StartsWith expects 2 or 3 args");
			var s = ArgHelper.GetString(args, 0);
			var p = ArgHelper.GetString(args, 1);
			var comp = ctx.Options.StringComparison;
			if (args.Length == 3)
			{
				var cmp = ArgHelper.GetString(args, 2);
				comp = (cmp.Equals("ignorecase", StringComparison.OrdinalIgnoreCase) || string.Equals(cmp, "i", StringComparison.OrdinalIgnoreCase))
					? StringComparison.OrdinalIgnoreCase : ctx.Options.StringComparison;
			}
			return s.StartsWith(p, comp);
		}
	}

	public sealed class EndsWithFunction : IFunction
	{
		public string Name => "EndsWith";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 2 || args.Length > 3) throw new ArgumentException("EndsWith expects 2 or 3 args");
			var s = ArgHelper.GetString(args, 0);
			var p = ArgHelper.GetString(args, 1);
			var comp = ctx.Options.StringComparison;
			if (args.Length == 3)
			{
				var cmp = ArgHelper.GetString(args, 2);
				comp = (cmp.Equals("ignorecase", StringComparison.OrdinalIgnoreCase) || cmp.Equals("i", StringComparison.OrdinalIgnoreCase))
					? StringComparison.OrdinalIgnoreCase : ctx.Options.StringComparison;
			}
			return s.EndsWith(p, comp);
		}
	}

	public sealed class ContainsFunction : IFunction
	{
		public string Name => "Contains";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 2 || args.Length > 3) throw new ArgumentException("Contains expects 2 or 3 args");
			var s = ArgHelper.GetString(args, 0);
			var p = ArgHelper.GetString(args, 1);
			var comp = ctx.Options.StringComparison;
			if (args.Length == 3)
			{
				var cmp = ArgHelper.GetString(args, 2);
				comp = (cmp.Equals("ignorecase", StringComparison.OrdinalIgnoreCase) || string.Equals(cmp, "i", StringComparison.OrdinalIgnoreCase))
					? StringComparison.OrdinalIgnoreCase : ctx.Options.StringComparison;
			}
			return s.IndexOf(p, comp) >= 0;
		}
	}

	public sealed class ToUpperFunction : IFunction
	{
		public string Name => "ToUpper";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 1) throw new ArgumentException("ToUpper expects 1 arg");
			return ArgHelper.GetString(args, 0).ToUpperInvariant();
		}
	}

	public sealed class ToLowerFunction : IFunction
	{
		public string Name => "ToLower";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 1) throw new ArgumentException("ToLower expects 1 arg");
			return ArgHelper.GetString(args, 0).ToLowerInvariant();
		}
	}

	public sealed class TrimFunction : IFunction
	{
		public string Name => "Trim";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 1) throw new ArgumentException("Trim expects 1 arg");
			return (args[0]?.ToString() ?? string.Empty).Trim();
		}
	}

	public sealed class LenFunction : IFunction
	{
		public string Name => "Len";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 1) throw new ArgumentException("Len expects 1 arg");
			var s = args[0]?.ToString() ?? string.Empty;
			return (decimal)s.Length;
		}
	}

	public sealed class ReplaceFunction : IFunction
	{
		public string Name => "Replace";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 3 || args.Length > 4) throw new ArgumentException("Replace expects 3 or 4 args");
			var s = args[0]?.ToString() ?? string.Empty;
			var oldv = args[1]?.ToString() ?? string.Empty;
			var newv = args[2]?.ToString() ?? string.Empty;
			var comp = ctx.Options.StringComparison;
			if (args.Length == 4 && args[3] is string cmp)
			{
				comp = (cmp.Equals("ignorecase", StringComparison.OrdinalIgnoreCase) || string.Equals(cmp, "i", StringComparison.OrdinalIgnoreCase))
					? StringComparison.OrdinalIgnoreCase : ctx.Options.StringComparison;
			}
			if (comp == StringComparison.Ordinal)
				return s.Replace(oldv, newv);
			// Case-insensitive replace (with timeout)
			var options = comp == StringComparison.OrdinalIgnoreCase ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None;
			var timeout = ctx.Options.RegexTimeoutMilliseconds > 0
				? System.TimeSpan.FromMilliseconds(ctx.Options.RegexTimeoutMilliseconds)
				: System.Threading.Timeout.InfiniteTimeSpan;
			return System.Text.RegularExpressions.Regex.Replace(s, System.Text.RegularExpressions.Regex.Escape(oldv), newv, options, timeout);
		}
	}

	public sealed class CoalesceFunction : IFunction
	{
		public string Name => "Coalesce";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length < 2) throw new ArgumentException("Coalesce expects at least 2 args");
			foreach (var a in args)
			{
				if (a != null) return a;
			}
			return null;
		}
	}

	public sealed class IifFunction : IFunction
	{
		public string Name => "Iif";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			if (args.Length != 3) throw new ArgumentException("Iif expects 3 args");
			if (args[0] is bool b) return b ? args[1] : args[2];
			throw new ArgumentException("Iif condition must be boolean");
		}
	}

	public sealed class FieldExistsFunction : IFunction
	{
		public string Name => "FieldExists";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			FunctionParameterValidator.ValidateArgumentCount("FieldExists", args, 1, null);
			for (int i = 0; i < args.Length; i++)
			{
				var fieldName = FunctionParameterValidator.GetStringArgument("FieldExists", args, i);
				if (!ctx.InputFields.ContainsKey(fieldName)) return false;
			}
			return true;
		}
	}

	public sealed class RegexMatchFunction : IFunction
	{
		public string Name => "RegexMatch";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			FunctionParameterValidator.ValidateArgumentCount("RegexMatch", args, 2, 3);
			
			var input = FunctionParameterValidator.GetStringArgument("RegexMatch", args, 0, allowNull: true);
			var pattern = FunctionParameterValidator.GetStringArgument("RegexMatch", args, 1);
			var flags = FunctionParameterValidator.GetOptionalStringArgument("RegexMatch", args, 2, string.Empty);

			// Validate regex pattern
			if (string.IsNullOrEmpty(pattern))
			{
				throw new ArgumentException("RegexMatch pattern cannot be empty");
			}

			var options = RegexOptions.Compiled;
			
			// Parse flag argument (only i/m supported)
			if (!string.IsNullOrEmpty(flags))
			{
				options |= ParseRegexFlags(flags);
			}
			else
			{
				// If no explicit flags are specified, use the global string comparison setting
				if (ctx.Options.StringComparison == StringComparison.OrdinalIgnoreCase)
				{
					options |= RegexOptions.IgnoreCase;
				}
			}

			var timeout = ctx.Options.RegexTimeoutMilliseconds > 0
				? TimeSpan.FromMilliseconds(ctx.Options.RegexTimeoutMilliseconds)
				: System.Threading.Timeout.InfiniteTimeSpan;

			try
			{
				// Validate regex syntax
				var regex = new Regex(pattern, options, timeout);
				return regex.IsMatch(input);
			}
			catch (RegexMatchTimeoutException)
			{
				throw new ArgumentException("RegexMatch operation timed out");
			}
			catch (ArgumentException ex) when (ex.Message.Contains("parsing"))
			{
				throw new ArgumentException($"RegexMatch invalid pattern: {ex.Message}");
			}
		}

		private static RegexOptions ParseRegexFlags(string? flags)
		{
			var options = RegexOptions.None;
			
			// Supported flags: i (IgnoreCase), m (Multiline)
			if (string.IsNullOrEmpty(flags)) return options;
			
			foreach (char flag in flags.ToLowerInvariant())
			{
				switch (flag)
				{
					case 'i':
						options |= RegexOptions.IgnoreCase;
						break;
					case 'm':
						options |= RegexOptions.Multiline;
						break;
					case ' ':
					case '\t':
						// Ignore whitespace characters
						break;
					default:
						throw new ArgumentException($"RegexMatch unsupported flag: '{flag}'. Supported flags are: i (IgnoreCase), m (Multiline)");
				}
			}

			return options;
		}
	}
}

