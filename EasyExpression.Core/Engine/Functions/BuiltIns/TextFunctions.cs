using System;

namespace EasyExpression.Core.Engine.Functions.BuiltIns
{
	public sealed class SubstringFunction : IFunction
	{
		public string Name => "Substring";
		public object? Invoke(object?[] args, InvocationContext ctx)
		{
			FunctionParameterValidator.ValidateArgumentCount("Substring", args, 2, 3);
			
			var s = FunctionParameterValidator.GetStringArgument("Substring", args, 0, allowNull: true);
			var start = FunctionParameterValidator.GetIntArgument("Substring", args, 1, ctx, min: 0);
			
			if (start > s.Length)
			{
				throw new ArgumentException($"Substring start index {start} is beyond string length {s.Length}");
			}
			
			if (args.Length == 2)
			{
				return s.Substring(start);
			}
			
			var length = FunctionParameterValidator.GetIntArgument("Substring", args, 2, ctx, min: 0);
			
			if (start + length > s.Length)
			{
				throw new ArgumentException($"Substring start index {start} + length {length} = {start + length} is beyond string length {s.Length}");
			}
			
			return s.Substring(start, length);
		}
	}
}


