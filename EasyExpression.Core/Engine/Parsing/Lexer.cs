using System;
using System.Text;
using EasyExpression.Core.Engine;

namespace EasyExpression.Core.Engine.Parsing
{
	public sealed class Lexer
	{
		private readonly string _text;
		private int _pos;
		private int _line = 1;
		private int _col = 1;
		private bool _fieldNameMode;
		private readonly bool _enableComments;

		public Lexer(string text)
		{
			_text = text ?? string.Empty;
			_enableComments = false;
		}

		public Lexer(string text, ExpressionEngineOptions options)
		{
			_text = text ?? string.Empty;
			_enableComments = options?.EnableComments ?? false;
		}

		private char Current => _pos < _text.Length ? _text[_pos] : '\0';
		private char Peek(int offset) => _pos + offset < _text.Length ? _text[_pos + offset] : '\0';

		private void Advance(int count = 1)
		{
			for (int i = 0; i < count; i++)
			{
				if (Current == '\n') { _line++; _col = 1; }
				else { _col++; }
				_pos++;
			}
		}

		public void BeginFieldName() => _fieldNameMode = true;
		public void EndFieldName() => _fieldNameMode = false;

		public Token Next()
		{
			// 跳过空白与注释
			while (true)
			{
				bool progressed = false;
				// 空白处理
				while (char.IsWhiteSpace(Current))
				{
					progressed = true;
					if (Current == '\n' || Current == '\r')
					{
						// 将 CRLF 和 LF 统一视作 NewLine
						if (Current == '\r' && Peek(1) == '\n') Advance(2);
						else Advance();
						return new Token(TokenKind.NewLine, "\n", _line, _col);
					}
					// 在字段名模式中，空格应作为标识符内容的一部分，不在此处跳过
					if (_fieldNameMode && Current == ' ')
					{
						break;
					}
					Advance();
				}
				// 注释处理：按选项控制；默认禁用
				if (_enableComments)
				{
					if (Current == '/' && Peek(1) == '/')
					{
						progressed = true;
						// 跳到行尾但不消费换行，交由下一轮返回 NewLine
						while (Current != '\0' && Current != '\n' && Current != '\r') Advance();
						continue;
					}
					if (Current == '/' && Peek(1) == '*')
					{
						progressed = true;
						Advance(2);
						while (Current != '\0')
						{
							if (Current == '*' && Peek(1) == '/') { Advance(2); break; }
							Advance();
						}
						continue;
					}
				}
				if (!progressed) break;
			}

			if (_pos >= _text.Length)
				return new Token(TokenKind.EOF, string.Empty, _line, _col);

			var startLine = _line;
			var startCol = _col;

			char c = Current;
			if (_fieldNameMode && c != ']' && c != ':')
			{
				// 在字段名模式下，收集到 ']' 或 ':' 为止，允许空格
				var sb = new StringBuilder();
				while (Current != '\0' && Current != ']' && Current != ':')
				{
					if (Current == '\n' || Current == '\r')
						throw new ExpressionParseException("Field name must be in single line", ExpressionErrorCode.InvalidFieldName, _line, _col);
					sb.Append(Current);
					Advance();
				}
				return new Token(TokenKind.Identifier, sb.ToString().Trim(), startLine, startCol);
			}

			if (char.IsLetter(c) || c == '_' )
			{
				var sb = new StringBuilder();
				while (char.IsLetterOrDigit(Current) || Current == '_')
				{
					sb.Append(Current);
					Advance();
				}
				return new Token(TokenKind.Identifier, sb.ToString(), startLine, startCol);
			}

			if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek(1))))
			{
				var sb = new StringBuilder();
				bool hasDot = false;
				while (char.IsDigit(Current) || (!hasDot && Current == '.'))
				{
					if (Current == '.') hasDot = true;
					sb.Append(Current);
					Advance();
				}
				return new Token(TokenKind.Number, sb.ToString(), startLine, startCol);
			}

			if (c == '\'')
			{
				Advance();
				var sb = new StringBuilder();
				while (Current != '\0')
				{
					if (Current == '\'') { Advance(); return new Token(TokenKind.String, sb.ToString(), startLine, startCol); }
					if (Current == '\\')
					{
						Advance();
						if (Current == '\0')
							throw new ExpressionParseException("Unterminated string literal", ExpressionErrorCode.UnterminatedString, startLine, startCol);

						char esc = Current;
						Advance();
						switch (esc)
						{
							case '\'':
								sb.Append('\'');
								break;
							case 'r':
								sb.Append('\r');
								break;
							case 'n':
								sb.Append('\n');
								break;
							case 't':
								sb.Append('\t');
								break;
							case '\\':
								sb.Append('\\');
								break;
							default:
								sb.Append('\\');
								sb.Append(esc);
								break;
						}
					}
					else
					{
						sb.Append(Current);
						Advance();
					}
				}
				throw new ExpressionParseException("Unterminated string literal", ExpressionErrorCode.UnterminatedString, startLine, startCol);
			}

			switch (c)
			{
				case '(': Advance(); return new Token(TokenKind.LParen, "(", startLine, startCol);
				case ')': Advance(); return new Token(TokenKind.RParen, ")", startLine, startCol);
				case '{': Advance(); return new Token(TokenKind.LBrace, "{", startLine, startCol);
				case '}': Advance(); return new Token(TokenKind.RBrace, "}", startLine, startCol);
				case '[': Advance(); return new Token(TokenKind.LBracket, "[", startLine, startCol);
				case ']': Advance(); return new Token(TokenKind.RBracket, "]", startLine, startCol);
				case ',': Advance(); return new Token(TokenKind.Comma, ",", startLine, startCol);
				case ':': Advance(); return new Token(TokenKind.Colon, ":", startLine, startCol);
				case '+': Advance(); return new Token(TokenKind.Plus, "+", startLine, startCol);
				case '-':
					Advance();
					return new Token(TokenKind.Minus, "-", startLine, startCol);
				case '*': Advance(); return new Token(TokenKind.Star, "*", startLine, startCol);
				case '/': Advance(); return new Token(TokenKind.Slash, "/", startLine, startCol);
				case '%': Advance(); return new Token(TokenKind.Percent, "%", startLine, startCol);
				case '&':
					if (Peek(1) == '&') { Advance(2); return new Token(TokenKind.AndAnd, "&&", startLine, startCol); }
					break;
				case '|':
					if (Peek(1) == '|') { Advance(2); return new Token(TokenKind.OrOr, "||", startLine, startCol); }
					break;
				case '=':
					if (Peek(1) == '=') { Advance(2); return new Token(TokenKind.EqualEqual, "==", startLine, startCol); }
					break;
				case '!':
					if (Peek(1) == '=') { Advance(2); return new Token(TokenKind.BangEqual, "!=", startLine, startCol); }
					Advance();
					return new Token(TokenKind.Bang, "!", startLine, startCol);
				case '>':
					Advance();
					if (Current == '=') { Advance(); return new Token(TokenKind.GreaterEqual, ">=", startLine, startCol); }
					return new Token(TokenKind.Greater, ">", startLine, startCol);
				case '<':
					Advance();
					if (Current == '=') { Advance(); return new Token(TokenKind.LessEqual, "<=", startLine, startCol); }
					return new Token(TokenKind.Less, "<", startLine, startCol);
			}

			throw new ExpressionParseException($"Unexpected char '{c}'", ExpressionErrorCode.UnexpectedToken, startLine, startCol);
		}
	}
}


