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
	/// 引擎扩展贡献者：用于在上层框架（如 ABP）中集中注册函数与转换器。
	/// </summary>
	public interface IEngineContributor
	{
		void Configure(Engine.EngineServices services);
	}
}


