using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine.Functions;

namespace EasyExpression.Core.Engine
{
	public interface IExpressionEngineFactory
	{
		ExpressionEngine Create(Action<ExpressionEngineOptions>? configureOptions = null, IEnumerable<IEngineContributor>? contributors = null);
	}

	public sealed class DefaultExpressionEngineFactory : IExpressionEngineFactory
	{
		public ExpressionEngine Create(Action<ExpressionEngineOptions>? configureOptions = null, IEnumerable<IEngineContributor>? contributors = null)
		{
			var options = new ExpressionEngineOptions();
			configureOptions?.Invoke(options);
			var services = new EngineServices(options);
			if (contributors != null)
			{
				foreach (var c in contributors)
				{
					services.UseContributor(c);
				}
			}
			return new ExpressionEngine(services);
		}
	}
}


