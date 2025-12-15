using System;
using System.Collections.Generic;

namespace EasyExpression.Core.Engine.Functions
{
	public sealed class FunctionRegistry
	{
		private readonly Dictionary<string, IFunction> _functions;
		private readonly StringComparer _comparer;

		public FunctionRegistry(bool caseInsensitive = true)
		{
			_comparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			_functions = new Dictionary<string, IFunction>(_comparer);
		}

		public void Register(IFunction func)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));
			_functions[func.Name] = func; // Overwrite registration
		}

		public IFunction Resolve(string name)
		{
			if (!_functions.TryGetValue(name, out var func))
			{
				throw new ArgumentException($"Unknown function: {name}");
			}
			return func;
		}
	}
}


