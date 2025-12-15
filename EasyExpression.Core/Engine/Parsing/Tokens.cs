using System;

namespace EasyExpression.Core.Engine.Parsing
{
	public enum TokenKind
	{
		EOF,
		NewLine,
		Identifier,
		Number,
		String,
		LParen, RParen,
		LBrace, RBrace,
		LBracket, RBracket,
		Comma,
		Colon,
		Plus, Minus, Star, Slash, Percent,
		Greater, Less, GreaterEqual, LessEqual,
		EqualEqual, BangEqual, Bang,
		AndAnd, OrOr
	}

	public readonly struct Token
	{
		public readonly TokenKind Kind;
		public readonly string Text;
		public readonly int Line;
		public readonly int Column;

		public Token(TokenKind kind, string text, int line, int column)
		{
			Kind = kind;
			Text = text;
			Line = line;
			Column = column;
		}

		public override string ToString() => $"{Kind} '{Text}' @({Line},{Column})";
	}
}


