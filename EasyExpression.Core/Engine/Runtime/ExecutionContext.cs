using System;
using System.Collections.Generic;

namespace EasyExpression.Core.Engine.Runtime
{
	public sealed class ExecutionContext
	{
		public IReadOnlyDictionary<string, object?> InputFields => _fields;
		private readonly Dictionary<string, object?> _fields;

		public Dictionary<string, object?> MutableFields { get; }

		public ExpressionEngineOptions Options { get; }

		public ExecutionContext(Dictionary<string, object?> fields, ExpressionEngineOptions options)
		{
			_fields = new Dictionary<string, object?>(fields ?? new Dictionary<string, object?>(),
				options.CaseInsensitiveFieldNames ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			MutableFields = new Dictionary<string, object?>(_fields,
				options.CaseInsensitiveFieldNames ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			Options = options;
		}
	}
}


