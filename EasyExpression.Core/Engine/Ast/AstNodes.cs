using System;
using System.Collections.Generic;

namespace EasyExpression.Core.Engine.Ast
{
	public abstract class AstNode
	{
		public int Line { get; }
		public int Column { get; }
		protected AstNode(int line, int column) { Line = line; Column = column; }
	}

	public abstract class Expr : AstNode
	{
		protected Expr(int line, int column) : base(line, column) { }
	}

	public sealed class LiteralExpr : Expr
	{
		public object? Value { get; }
		public LiteralExpr(object? value, int line, int column) : base(line, column) { Value = value; }
	}

	public sealed class FieldExpr : Expr
	{
		public string Name { get; }
		public string? TypeAnnotation { get; }
		public FieldExpr(string name, string? typeAnnotation, int line, int column) : base(line, column)
		{
			Name = name; TypeAnnotation = typeAnnotation;
		}
	}

	public enum BinaryOp
	{
		Add, Sub, Mul, Div, Mod,
		Gt, Lt, Ge, Le, Eq, Ne,
		And, Or
	}

	public sealed class UnaryExpr : Expr
	{
		public string Op { get; }
		public Expr Inner { get; }
		public UnaryExpr(string op, Expr inner, int line, int column) : base(line, column) { Op = op; Inner = inner; }
	}

	public sealed class BinaryExpr : Expr
	{
		public BinaryOp Op { get; }
		public Expr Left { get; }
		public Expr Right { get; }
		public BinaryExpr(BinaryOp op, Expr left, Expr right, int line, int column) : base(line, column)
		{ Op = op; Left = left; Right = right; }
	}

	public sealed class CallExpr : Expr
	{
		public string Name { get; }
		public List<Expr> Args { get; }
		public CallExpr(string name, List<Expr> args, int line, int column) : base(line, column) { Name = name; Args = args; }
	}

	public abstract class Stmt : AstNode
	{
		protected Stmt(int line, int column) : base(line, column) { }
	}

	public sealed class Block : Stmt
	{
		public List<Stmt> Statements { get; } = new List<Stmt>();
		public Block(int line, int column) : base(line, column) { }
	}

	public sealed class SetStmt : Stmt
	{
		public string FieldName { get; }
		public Expr Value { get; }
		public string? TypeAnnotation { get; }
		public SetStmt(string fieldName, Expr value, int line, int column, string? typeAnnotation = null) : base(line, column)
		{ FieldName = fieldName; Value = value; TypeAnnotation = typeAnnotation; }
	}

	public sealed class MsgStmt : Stmt
	{
		public string Text { get; }
		public string? Level { get; }
		public MsgStmt(string text, string? level, int line, int column) : base(line, column)
		{ Text = text; Level = level; }
	}

	public enum ReturnKind { Return, ReturnLocal }

	public sealed class ReturnStmt : Stmt
	{
		public ReturnKind Kind { get; }
		public ReturnStmt(ReturnKind kind, int line, int column) : base(line, column) { Kind = kind; }
	}

	public sealed class AssertStmt : Stmt
	{
		public Expr Condition { get; }
		public string Action { get; }
		public string Message { get; }
		public string? MsgLevel { get; }
		public AssertStmt(Expr condition, string action, string message, string? msgLevel, int line, int column) : base(line, column)
		{ Condition = condition; Action = action; Message = message; MsgLevel = msgLevel; }
	}

	public sealed class IfStmt : Stmt
	{
		public Expr Condition { get; }
		public Block Then { get; }
		public List<(Expr Cond, Block Body)> ElseIfs { get; } = new List<(Expr Cond, Block Body)>();
		public Block? Else { get; set; }
		public IfStmt(Expr condition, Block then, int line, int column) : base(line, column)
		{ Condition = condition; Then = then; }
	}

	public sealed class LocalStmt : Stmt
	{
		public Block Body { get; }
		public LocalStmt(Block body, int line, int column) : base(line, column) { Body = body; }
	}
}


