using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using EasyExpression.Core.Engine.Ast;

namespace EasyExpression.Core.Engine.Parsing
{
	public sealed class Parser
	{
		private readonly Lexer _lexer;
		private Token _current;

		public Parser(string text, ExpressionEngineOptions? options = null)
		{
			_lexer = options == null ? new Lexer(text) : new Lexer(text, options);
			_current = _lexer.Next();
		}

		private void Next() => _current = _lexer.Next();

		private bool Match(TokenKind kind)
		{
			if (_current.Kind == kind) { Next(); return true; }
			return false;
		}

		private Token Expect(TokenKind kind, string message)
		{
			if (_current.Kind != kind)
				throw new ExpressionParseException(message, ExpressionErrorCode.SyntaxError, _current.Line, _current.Column);
			var t = _current; Next(); return t;
		}

		private void SkipNewLines()
		{
			while (_current.Kind == TokenKind.NewLine) Next();
		}

		public Block ParseBlock()
		{
			SkipNewLines();
			if (_current.Kind == TokenKind.LBrace)
			{
				// Allow wrapping the whole script with outer braces
				return ParseBlockWithBraces();
			}
			var block = new Block(_current.Line, _current.Column);
			while (_current.Kind != TokenKind.EOF && _current.Kind != TokenKind.RBrace)
			{
				// Ensure all blank lines are skipped before parsing a statement
				SkipNewLines();
				
				// Re-check whether we have reached the end condition
				if (_current.Kind == TokenKind.EOF || _current.Kind == TokenKind.RBrace)
				{
					break;
				}
				
				var stmt = ParseStatement();
				block.Statements.Add(stmt);
				SkipNewLines();
			}
			return block;
		}

		private Stmt ParseStatement()
		{
			if (_current.Kind == TokenKind.Identifier)
			{
				var id = _current.Text;
				var line = _current.Line; var col = _current.Column; Next();
				switch (id.ToLowerInvariant())
				{
					case "set":
						Expect(TokenKind.LParen, "expected '('");
						SkipNewLines(); // Skip possible newlines
						string fieldName;
						string? fieldType = null;
						if (_current.Kind == TokenKind.LBracket)
						{
							// Support set([field name], expr) and set([field name:type], expr)
							_lexer.BeginFieldName();
							Next();
							var nameTok = Expect(TokenKind.Identifier, "expected field name");
							if (Match(TokenKind.Colon))
							{
								var typeTok = Expect(TokenKind.Identifier, "expected type");
								fieldType = typeTok.Text;
							}
							_lexer.EndFieldName();
							Expect(TokenKind.RBracket, "expected ']' ");
							fieldName = nameTok.Text;
						}
						else
						{
							var fieldTok = Expect(TokenKind.Identifier, "expected field name");
							fieldName = fieldTok.Text;
						}
						Expect(TokenKind.Comma, "expected ','");
						SkipNewLines(); // Skip possible newlines
						var expr = ParseExpression();
						SkipNewLines(); // Skip possible newlines
						Expect(TokenKind.RParen, "expected ')'");
						return new SetStmt(fieldName, expr, line, col, fieldType);
					case "msg":
						Expect(TokenKind.LParen, "expected '('");
						var text = ParseExpression();
						string? lvl = null;
						if (Match(TokenKind.Comma))
						{
							var lv = ParseExpression();
							if (lv is LiteralExpr le && le.Value is string s1) lvl = s1; else throw new ExpressionParseException("level must be string literal", ExpressionErrorCode.TypeMismatch, _current.Line, _current.Column);
						}
						Expect(TokenKind.RParen, "expected ')'");
						if (text is LiteralExpr lt && lt.Value is string s) return new MsgStmt(s, lvl, line, col);
						throw new ExpressionParseException("MSG text must be string literal", ExpressionErrorCode.TypeMismatch, _current.Line, _current.Column);
					case "return":
						return new ReturnStmt(ReturnKind.Return, line, col);
					case "return_local":
						return new ReturnStmt(ReturnKind.ReturnLocal, line, col);
					case "assert":
						Expect(TokenKind.LParen, "expected '('");
						var cond = ParseExpression();
						Expect(TokenKind.Comma, "expected ','");
						var actionExpr = ParseExpression();
						Expect(TokenKind.Comma, "expected ','");
						var msgExpr = ParseExpression();
						string? msgLevel = null;
						if (Match(TokenKind.Comma))
						{
							var lv = ParseExpression();
							if (lv is LiteralExpr le && le.Value is string sl) msgLevel = sl; else throw new ExpressionParseException("msgLevel must be string literal", ExpressionErrorCode.TypeMismatch, _current.Line, _current.Column);
						}
						Expect(TokenKind.RParen, "expected ')'");
						if (actionExpr is LiteralExpr la && la.Value is string act && msgExpr is LiteralExpr lm && lm.Value is string msg)
							return new AssertStmt(cond, act, msg, msgLevel, line, col);
						throw new ExpressionParseException("assert(action,msg) must be string literals", ExpressionErrorCode.TypeMismatch, _current.Line, _current.Column);
					case "if":
						Expect(TokenKind.LParen, "expected '('");
						var c = ParseExpression();
						Expect(TokenKind.RParen, "expected ')'");
						var then = ParseBlockWithBraces();
						var ifs = new IfStmt(c, then, line, col);
						SkipNewLines();
						while (_current.Kind == TokenKind.Identifier && _current.Text.Equals("elseif", StringComparison.OrdinalIgnoreCase))
						{
							Next();
							Expect(TokenKind.LParen, "expected '('");
							var c2 = ParseExpression();
							Expect(TokenKind.RParen, "expected ')'");
							var b2 = ParseBlockWithBraces();
							ifs.ElseIfs.Add((c2, b2));
							SkipNewLines();
						}
						if (_current.Kind == TokenKind.Identifier && _current.Text.Equals("else", StringComparison.OrdinalIgnoreCase))
						{
							Next();
							ifs.Else = ParseBlockWithBraces();
						}
						return ifs;
					case "local":
						var body = ParseBlockWithBraces();
						return new LocalStmt(body, line, col);
				}
			}
			throw new ExpressionParseException($"Unexpected token: {_current}", ExpressionErrorCode.UnexpectedToken, _current.Line, _current.Column);
		}

		private Block ParseBlockWithBraces()
		{
			Expect(TokenKind.LBrace, "expected '{'");
			var block = new Block(_current.Line, _current.Column);
			SkipNewLines();
			while (_current.Kind != TokenKind.RBrace)
			{
				// If EOF is encountered, avoid infinite loops
				if (_current.Kind == TokenKind.EOF)
				{
					throw new ExpressionParseException("unexpected end of file, expected '}'", ExpressionErrorCode.UnexpectedEndOfFile, _current.Line, _current.Column);
				}
				
				// Ensure all blank lines are skipped before parsing a statement
				SkipNewLines();
				
				// Re-check whether we have reached the closing brace
				if (_current.Kind == TokenKind.RBrace)
				{
					break;
				}
				
				var stmt = ParseStatement();
				block.Statements.Add(stmt);
				SkipNewLines();
			}
			Expect(TokenKind.RBrace, "expected '}'");
			return block;
		}

		// Expression precedence: same as C#
		// Or ||
		private Expr ParseExpression() => ParseOr();
		private Expr ParseOr()
		{
			var left = ParseAnd();
			while (_current.Kind == TokenKind.OrOr)
			{
				var op = _current; Next();
				var right = ParseAnd();
				left = new BinaryExpr(BinaryOp.Or, left, right, op.Line, op.Column);
			}
			return left;
		}

		private Expr ParseAnd()
		{
			var left = ParseEquality();
			while (_current.Kind == TokenKind.AndAnd)
			{
				var op = _current; Next();
				var right = ParseEquality();
				left = new BinaryExpr(BinaryOp.And, left, right, op.Line, op.Column);
			}
			return left;
		}

		private Expr ParseEquality()
		{
			var left = ParseRelational();
			while (_current.Kind == TokenKind.EqualEqual || _current.Kind == TokenKind.BangEqual)
			{
				var op = _current; Next();
				var right = ParseRelational();
				left = new BinaryExpr(op.Kind == TokenKind.EqualEqual ? BinaryOp.Eq : BinaryOp.Ne, left, right, op.Line, op.Column);
			}
			return left;
		}

		private Expr ParseRelational()
		{
			var left = ParseAdditive();
			while (_current.Kind == TokenKind.Greater || _current.Kind == TokenKind.Less || _current.Kind == TokenKind.GreaterEqual || _current.Kind == TokenKind.LessEqual)
			{
				var op = _current; Next();
				var right = ParseAdditive();
				left = new BinaryExpr(op.Kind switch
				{
					TokenKind.Greater => BinaryOp.Gt,
					TokenKind.Less => BinaryOp.Lt,
					TokenKind.GreaterEqual => BinaryOp.Ge,
					TokenKind.LessEqual => BinaryOp.Le,
					_ => throw new InvalidOperationException()
				}, left, right, op.Line, op.Column);
			}
			return left;
		}

		private Expr ParseAdditive()
		{
			var left = ParseMultiplicative();
			while (_current.Kind == TokenKind.Plus || _current.Kind == TokenKind.Minus)
			{
				var op = _current; Next();
				var right = ParseMultiplicative();
				left = new BinaryExpr(op.Kind == TokenKind.Plus ? BinaryOp.Add : BinaryOp.Sub, left, right, op.Line, op.Column);
			}
			return left;
		}

		private Expr ParseMultiplicative()
		{
			var left = ParseUnary();
			while (_current.Kind == TokenKind.Star || _current.Kind == TokenKind.Slash || _current.Kind == TokenKind.Percent)
			{
				var op = _current; Next();
				var right = ParseUnary();
				var bop = op.Kind == TokenKind.Star ? BinaryOp.Mul : op.Kind == TokenKind.Slash ? BinaryOp.Div : BinaryOp.Mod;
				left = new BinaryExpr(bop, left, right, op.Line, op.Column);
			}
			return left;
		}

		private Expr ParseUnary()
		{
			// Support unary minus and logical not
			if (_current.Kind == TokenKind.Minus || _current.Kind == TokenKind.Bang)
			{
				var tok = _current; Next();
				var inner = ParseUnary();
				var op = tok.Kind == TokenKind.Minus ? "-" : "!";
				return new UnaryExpr(op, inner, tok.Line, tok.Column);
			}
			return ParsePrimary();
		}

		private Expr ParsePrimary()
		{
			// Field [name[:type]] starts with '['
			if (_current.Kind == TokenKind.LBracket)
			{
				var line = _current.Line; var col = _current.Column;
				// Tell the lexer to enter field-name mode
				_lexer.BeginFieldName();
				Next();
				var nameTok = Expect(TokenKind.Identifier, "expected field name");
				string? type = null;
				if (Match(TokenKind.Colon))
				{
					var t2 = Expect(TokenKind.Identifier, "expected type");
					type = t2.Text;
				}
				_lexer.EndFieldName();
				Expect(TokenKind.RBracket, "expected ']' ");
				return new FieldExpr(nameTok.Text, type, line, col);
			}
			if (_current.Kind == TokenKind.Number)
			{
				var t = _current; Next();
				return new LiteralExpr(decimal.Parse(t.Text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture), t.Line, t.Column);
			}
			if (_current.Kind == TokenKind.String)
			{
				var t = _current; Next();
				return new LiteralExpr(t.Text, t.Line, t.Column);
			}
			if (_current.Kind == TokenKind.Identifier)
			{
				var ident = _current.Text; var line = _current.Line; var col = _current.Column; Next();
				// Literal keywords are case-sensitive
				if (ident == "true") return new LiteralExpr(true, line, col);
				if (ident == "false") return new LiteralExpr(false, line, col);
				if (ident == "null") return new LiteralExpr(null, line, col);
				if (ident == "now")
					return new CallExpr("__now__", new List<Expr>(), line, col);

				if (Match(TokenKind.LParen))
				{
					// Function call
					var args = new List<Expr>();
					if (!Match(TokenKind.RParen))
					{
						do
						{
							args.Add(ParseExpression());
						}
						while (Match(TokenKind.Comma));
						Expect(TokenKind.RParen, "expected ')'");
					}
					return new CallExpr(ident, args, line, col);
				}

				return new FieldExpr(ident, null, line, col);
			}
			if (Match(TokenKind.LParen))
			{
				var e = ParseExpression();
				Expect(TokenKind.RParen, "expected ')'");
				return e;
			}

			throw new ExpressionParseException($"Unexpected token: {_current}", ExpressionErrorCode.UnexpectedToken, _current.Line, _current.Column);
		}
	}
}


