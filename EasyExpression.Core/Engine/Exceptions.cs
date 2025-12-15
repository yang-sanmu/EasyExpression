using System;

namespace EasyExpression.Core.Engine
{
	/// <summary>
	/// Error code enumeration.
	/// </summary>
	public enum ExpressionErrorCode
	{
		// General errors (0-99)
		Unknown = 0,
		
		// Parse errors (100-199)
		UnexpectedToken = 100,
		UnterminatedString = 101,
		InvalidNumber = 102,
		InvalidIdentifier = 103,
		UnexpectedEndOfFile = 104,
		SyntaxError = 105,
		InvalidFieldName = 106,
		
		// Runtime errors (200-299)
		UnknownField = 200,
		TypeMismatch = 201,
		DivideByZero = 202,
		ModuloByZero = 203,
		UnknownFunction = 204,
		InvalidFunctionArguments = 205,
		ConversionError = 206,
		AssertionFailed = 207,
		UnknownOperator = 208,
		NullReference = 209,
		
		// Limit errors (300-399)
		MaxNodesExceeded = 300,
		MaxVisitsExceeded = 301,
		MaxDepthExceeded = 302,
		ExecutionTimeout = 303,
		ScriptTooLarge = 304,
	}

	public class ExpressionException : Exception
	{
		public int Line { get; }
		public int Column { get; }
		public ExpressionErrorCode ErrorCode { get; }
		
		public ExpressionException(string message, ExpressionErrorCode errorCode = ExpressionErrorCode.Unknown, int line = 0, int column = 0, Exception? inner = null)
			: base(message, inner)
		{
			Line = line;
			Column = column;
			ErrorCode = errorCode;
		}
	}

	public sealed class ExpressionParseException : ExpressionException
	{
		public ExpressionParseException(string message, ExpressionErrorCode errorCode = ExpressionErrorCode.SyntaxError, int line = 0, int column = 0, Exception? inner = null)
			: base(message, errorCode, line, column, inner) { }
	}

	public sealed class ExpressionRuntimeException : ExpressionException
	{
		public ExpressionRuntimeException(string message, ExpressionErrorCode errorCode = ExpressionErrorCode.Unknown, int line = 0, int column = 0, Exception? inner = null)
			: base(message, errorCode, line, column, inner) { }
	}

	public sealed class ExpressionLimitException : ExpressionException
	{
		public ExpressionLimitException(string message, ExpressionErrorCode errorCode, int line = 0, int column = 0, Exception? inner = null)
			: base(message, errorCode, line, column, inner) { }
	}
}


