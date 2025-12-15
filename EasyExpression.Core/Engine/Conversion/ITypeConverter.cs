using System;

namespace EasyExpression.Core.Engine.Conversion
{
	public interface ITypeConverter
	{
		Type InputType { get; }
		Type OutputType { get; }
		bool TryConvert(object? value, out object? result);
	}
}


