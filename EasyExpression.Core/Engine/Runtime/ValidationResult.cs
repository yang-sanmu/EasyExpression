using System;
using System.Collections.Generic;

namespace EasyExpression.Core.Engine.Runtime
{
	public sealed class ValidationResult
	{
		public bool Success { get; set; }
		public string ErrorMessage { get; set; } = string.Empty;
		public int ErrorLine { get; set; }
		public int ErrorColumn { get; set; }
		public string ErrorSnippet { get; set; } = string.Empty;
		public int TotalNodes { get; set; }
		public Engine.ExpressionErrorCode ErrorCode { get; set; }

		// Added: script complexity info
		public ScriptComplexity Complexity { get; set; } = new ScriptComplexity();

		// Added: list of used functions
		public List<string> UsedFunctions { get; set; } = new List<string>();

		// Added: list of referenced fields
		public List<FieldReference> ReferencedFields { get; set; } = new List<FieldReference>();

		// Added: list of declared variables
		public List<string> DeclaredVariables { get; set; } = new List<string>();

		// Added: list of warnings
		public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
	}

	public sealed class ScriptComplexity
	{
		public int TotalExpressions { get; set; }
		public int NestedBlockDepth { get; set; }
		public int ConditionalStatements { get; set; }
		public int FunctionCalls { get; set; }
		public int ArithmeticOperations { get; set; }
		public int ComparisonOperations { get; set; }
		public int LogicalOperations { get; set; }
	}

	public sealed class FieldReference
	{
		public string Name { get; set; } = string.Empty;
		public string? TypeHint { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
	}

	public sealed class ValidationWarning
	{
		public string Message { get; set; } = string.Empty;
		public int Line { get; set; }
		public int Column { get; set; }
		public WarningType Type { get; set; }
	}

	public enum WarningType
	{
		Performance,
		Style,
		PotentialIssue,
		Deprecation
	}
}



