using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EasyExpression.Core.Engine.Runtime
{
	public enum MessageLevel
	{
		Info,
		Warn,
		Error
	}

	public sealed class ExecutionMessage
	{
		public MessageLevel Level { get; }
		public string Text { get; }
		public int Line { get; }
		public int Column { get; }

		public ExecutionMessage(MessageLevel level, string text, int line, int column)
		{
			Level = level;
			Text = text;
			Line = line;
			Column = column;
		}
	}

	public sealed class ExecutionResult
	{
		public List<ExecutionMessage> Messages { get; } = new List<ExecutionMessage>();
		public Dictionary<string, object?> Assignments { get; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
		public TimeSpan Elapsed { get; internal set; }
		public int EndLine { get; internal set; }
		public int EndColumn { get; internal set; }
		public bool HasError { get; internal set; }
		public string ErrorMessage { get; internal set; } = string.Empty;
		public int ErrorLine { get; internal set; }
		public int ErrorColumn { get; internal set; }
		public string ErrorSnippet { get; internal set; } = string.Empty;
		public Engine.ExpressionErrorCode ErrorCode { get; internal set; }

		internal Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
	}
}


