using System;
using System.Collections.Generic;

namespace EasyExpression.Core.Engine.Conversion
{
	public sealed class TypeConversionRegistry
	{
		private readonly List<ITypeConverter> _converters = new List<ITypeConverter>();

		public void Register(ITypeConverter converter)
		{
			if (converter == null) throw new ArgumentNullException(nameof(converter));
			_converters.Insert(0, converter); // Later registrations take precedence
		}

		public void Register<TIn, TOut>(Func<TIn, TOut> convert)
		{
			if (convert == null) throw new ArgumentNullException(nameof(convert));
			_converters.Insert(0, new SimpleLambdaConverter<TIn, TOut>(convert));
		}

		public bool TryConvert(object? value, Type targetType, out object? result)
		{
			result = null;
			if (value == null)
			{
				if (targetType == typeof(string))
				{
					result = string.Empty;
					return true;
				}
				return false;
			}

			var inputType = value.GetType();
			if (targetType.IsAssignableFrom(inputType))
			{
				result = value;
				return true;
			}

			foreach (var c in _converters)
			{
				if (c.InputType.IsAssignableFrom(inputType) && targetType.IsAssignableFrom(c.OutputType))
				{
					if (c.TryConvert(value, out result))
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	internal sealed class SimpleLambdaConverter<TIn, TOut> : ITypeConverter
	{
		public Type InputType => typeof(TIn);
		public Type OutputType => typeof(TOut);
		private readonly Func<TIn, TOut> _convert;
		public SimpleLambdaConverter(Func<TIn, TOut> convert) { _convert = convert; }
		public bool TryConvert(object? value, out object? result)
		{
			if (value is TIn v) { result = _convert(v); return true; }
			result = null; return false;
		}
	}
}


