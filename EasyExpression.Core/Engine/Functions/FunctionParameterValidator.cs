using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyExpression.Core.Engine.Functions
{
	/// <summary>
	/// 函数参数验证器，提供更详细的参数验证和错误信息
	/// </summary>
	internal static class FunctionParameterValidator
	{
		/// <summary>
		/// 验证参数数量
		/// </summary>
		public static void ValidateArgumentCount(string functionName, object?[] args, int min, int? max = null)
		{
			if (args.Length < min)
			{
				throw new ArgumentException($"Function '{functionName}' expects at least {min} argument(s), but got {args.Length}");
			}

			if (max.HasValue && args.Length > max.Value)
			{
				throw new ArgumentException($"Function '{functionName}' expects at most {max.Value} argument(s), but got {args.Length}");
			}
		}

		/// <summary>
		/// 验证参数数量（固定数量）
		/// </summary>
		public static void ValidateArgumentCount(string functionName, object?[] args, int expected)
		{
			if (args.Length != expected)
			{
				throw new ArgumentException($"Function '{functionName}' expects exactly {expected} argument(s), but got {args.Length}");
			}
		}

		/// <summary>
		/// 安全获取字符串参数
		/// </summary>
		public static string GetStringArgument(string functionName, object?[] args, int index, bool allowNull = false)
		{
			ValidateArgumentIndex(functionName, args, index);
			var value = args[index];
			
			if (value == null)
			{
				if (allowNull) return string.Empty;
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} cannot be null");
			}

			return value.ToString() ?? string.Empty;
		}

		/// <summary>
		/// 安全获取数值参数
		/// </summary>
		public static decimal GetDecimalArgument(string functionName, object?[] args, int index, InvocationContext ctx)
		{
			ValidateArgumentIndex(functionName, args, index);
			var value = args[index];

			if (value == null)
			{
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} cannot be null");
			}

			try
			{
				if (ctx.Converters.TryConvert(value, typeof(decimal), out var converted) && converted is decimal result)
				{
					return result;
				}

				if (value is IConvertible)
				{
					return Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
				}
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} cannot be converted to number: {ex.Message}");
			}

			throw new ArgumentException($"Function '{functionName}' argument {index + 1} must be a number, but got {value.GetType().Name}");
		}

		/// <summary>
		/// 安全获取整数参数
		/// </summary>
		public static int GetIntArgument(string functionName, object?[] args, int index, InvocationContext ctx, int? min = null, int? max = null)
		{
			var decimal_value = GetDecimalArgument(functionName, args, index, ctx);
			
			if (decimal_value != Math.Truncate(decimal_value))
			{
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} must be an integer, but got {decimal_value}");
			}

			var int_value = Convert.ToInt32(decimal_value);

			if (min.HasValue && int_value < min.Value)
			{
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} must be at least {min.Value}, but got {int_value}");
			}

			if (max.HasValue && int_value > max.Value)
			{
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} must be at most {max.Value}, but got {int_value}");
			}

			return int_value;
		}

		/// <summary>
		/// 安全获取布尔参数
		/// </summary>
		public static bool GetBoolArgument(string functionName, object?[] args, int index, InvocationContext ctx)
		{
			ValidateArgumentIndex(functionName, args, index);
			var value = args[index];

			if (value == null)
			{
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} cannot be null");
			}

			if (value is bool boolValue)
			{
				return boolValue;
			}

			if (ctx.Converters.TryConvert(value, typeof(bool), out var converted) && converted is bool result)
			{
				return result;
			}

			throw new ArgumentException($"Function '{functionName}' argument {index + 1} must be a boolean, but got {value.GetType().Name}");
		}

		/// <summary>
		/// 安全获取日期时间参数
		/// </summary>
		public static DateTime GetDateTimeArgument(string functionName, object?[] args, int index, InvocationContext ctx)
		{
			ValidateArgumentIndex(functionName, args, index);
			var value = args[index];

			if (value == null)
			{
				throw new ArgumentException($"Function '{functionName}' argument {index + 1} cannot be null");
			}

			if (value is DateTime dateTime)
			{
				return dateTime;
			}

			if (ctx.Converters.TryConvert(value, typeof(DateTime), out var converted) && converted is DateTime result)
			{
				return result;
			}

			throw new ArgumentException($"Function '{functionName}' argument {index + 1} must be a DateTime, but got {value.GetType().Name}");
		}

		/// <summary>
		/// 获取可选的字符串参数
		/// </summary>
		public static string? GetOptionalStringArgument(string functionName, object?[] args, int index, string? defaultValue = null)
		{
			if (index >= args.Length) return defaultValue;
			var value = args[index];
			return value?.ToString() ?? defaultValue;
		}

		/// <summary>
		/// 获取可选的整数参数
		/// </summary>
		public static int? GetOptionalIntArgument(string functionName, object?[] args, int index, InvocationContext ctx, int? defaultValue = null)
		{
			if (index >= args.Length) return defaultValue;
			return GetIntArgument(functionName, args, index, ctx);
		}

		/// <summary>
		/// 验证字符串选项参数
		/// </summary>
		public static string ValidateStringOption(string functionName, int argumentIndex, string value, params string[] validOptions)
		{
			if (validOptions.Contains(value, StringComparer.OrdinalIgnoreCase))
			{
				return validOptions.First(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase));
			}

			throw new ArgumentException($"Function '{functionName}' argument {argumentIndex + 1} must be one of: {string.Join(", ", validOptions)}, but got '{value}'");
		}

		/// <summary>
		/// 验证数值范围
		/// </summary>
		public static decimal ValidateDecimalRange(string functionName, int argumentIndex, decimal value, decimal? min = null, decimal? max = null)
		{
			if (min.HasValue && value < min.Value)
			{
				throw new ArgumentException($"Function '{functionName}' argument {argumentIndex + 1} must be at least {min.Value}, but got {value}");
			}

			if (max.HasValue && value > max.Value)
			{
				throw new ArgumentException($"Function '{functionName}' argument {argumentIndex + 1} must be at most {max.Value}, but got {value}");
			}

			return value;
		}

		/// <summary>
		/// 验证字符串长度范围
		/// </summary>
		public static string ValidateStringLength(string functionName, int argumentIndex, string value, int? minLength = null, int? maxLength = null)
		{
			if (minLength.HasValue && value.Length < minLength.Value)
			{
				throw new ArgumentException($"Function '{functionName}' argument {argumentIndex + 1} must be at least {minLength.Value} characters long");
			}

			if (maxLength.HasValue && value.Length > maxLength.Value)
			{
				throw new ArgumentException($"Function '{functionName}' argument {argumentIndex + 1} must be at most {maxLength.Value} characters long");
			}

			return value;
		}

		private static void ValidateArgumentIndex(string functionName, object?[] args, int index)
		{
			if (index >= args.Length)
			{
				throw new ArgumentException($"Function '{functionName}' missing argument at index {index + 1}");
			}
		}
	}

	/// <summary>
	/// 增强的参数帮助类，提供更好的错误消息
	/// </summary>
	internal static class EnhancedArgHelper
	{
		public static string GetString(string functionName, object?[] args, int index, bool allowNull = false)
		{
			return FunctionParameterValidator.GetStringArgument(functionName, args, index, allowNull);
		}

		public static decimal GetDecimal(string functionName, object?[] args, int index, InvocationContext ctx)
		{
			return FunctionParameterValidator.GetDecimalArgument(functionName, args, index, ctx);
		}

		public static int GetInt(string functionName, object?[] args, int index, InvocationContext ctx, int? min = null, int? max = null)
		{
			return FunctionParameterValidator.GetIntArgument(functionName, args, index, ctx, min, max);
		}

		public static bool GetBool(string functionName, object?[] args, int index, InvocationContext ctx)
		{
			return FunctionParameterValidator.GetBoolArgument(functionName, args, index, ctx);
		}

		public static DateTime GetDateTime(string functionName, object?[] args, int index, InvocationContext ctx)
		{
			return FunctionParameterValidator.GetDateTimeArgument(functionName, args, index, ctx);
		}

		public static string? GetOptionalString(string functionName, object?[] args, int index, string? defaultValue = null)
		{
			return FunctionParameterValidator.GetOptionalStringArgument(functionName, args, index, defaultValue);
		}

		public static int? GetOptionalInt(string functionName, object?[] args, int index, InvocationContext ctx, int? defaultValue = null)
		{
			return FunctionParameterValidator.GetOptionalIntArgument(functionName, args, index, ctx, defaultValue);
		}
	}
}

