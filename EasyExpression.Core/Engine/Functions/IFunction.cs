using System;

namespace EasyExpression.Core.Engine.Functions
{
	public interface IFunction
	{
		string Name { get; }
		object? Invoke(object?[] args, InvocationContext ctx);
	}

	public sealed class InvocationContext
	{
		public ExpressionEngineOptions Options { get; }
		public Conversion.TypeConversionRegistry Converters { get; }
		public System.Collections.Generic.IReadOnlyDictionary<string, object?> InputFields { get; }

		public InvocationContext(ExpressionEngineOptions options, Conversion.TypeConversionRegistry converters, System.Collections.Generic.IReadOnlyDictionary<string, object?> inputFields)
		{
			Options = options;
			Converters = converters;
			InputFields = inputFields;
		}
	}

	/// <summary>
	/// Engine extension contributor: used to register functions and converters in an upper-layer framework (e.g., ABP).
	/// </summary>
	public interface IEngineContributor
	{
		void Configure(Engine.EngineServices services);
	}
}


