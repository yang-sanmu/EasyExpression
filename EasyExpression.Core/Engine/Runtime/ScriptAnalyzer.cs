using System;
using System.Collections.Generic;
using System.Linq;
using EasyExpression.Core.Engine.Ast;

namespace EasyExpression.Core.Engine.Runtime
{
	/// <summary>
	/// 脚本分析器，用于收集脚本的详细验证信息
	/// </summary>
	internal sealed class ScriptAnalyzer
	{
		private readonly EngineServices _services;
		private readonly ValidationResult _result;
		private int _currentDepth;
		private int _maxDepth;

		public ScriptAnalyzer(EngineServices services)
		{
			_services = services;
			_result = new ValidationResult { Success = true };
		}

		public ValidationResult Analyze(Block block)
		{
			_result.TotalNodes = CountNodes(block);
			AnalyzeBlock(block, 0);
			_result.Complexity.NestedBlockDepth = _maxDepth;
			return _result;
		}

		private void AnalyzeBlock(Block block, int depth)
		{
			_currentDepth = depth;
			_maxDepth = Math.Max(_maxDepth, depth);

			foreach (var statement in block.Statements)
			{
				AnalyzeStatement(statement);
			}
		}

		private void AnalyzeStatement(Stmt statement)
		{
			switch (statement)
			{
				case SetStmt set:
					AnalyzeAssignment(set);
					break;
				case IfStmt ifStmt:
					AnalyzeIfStatement(ifStmt);
					break;
				case LocalStmt local:
					AnalyzeLocalStatement(local);
					break;
				case ReturnStmt:
					// 控制流语句，不需要特殊处理
					break;
				case MsgStmt msg:
					AnalyzeMsgStatement(msg);
					break;
				case AssertStmt assert:
					AnalyzeAssertStatement(assert);
					break;
			}
		}

		private void AnalyzeAssignment(SetStmt set)
		{
			if (!_result.DeclaredVariables.Contains(set.FieldName))
			{
				_result.DeclaredVariables.Add(set.FieldName);
			}
			AnalyzeExpression(set.Value);
		}

		private void AnalyzeIfStatement(IfStmt ifStmt)
		{
			_result.Complexity.ConditionalStatements++;
			AnalyzeExpression(ifStmt.Condition);
			AnalyzeBlock(ifStmt.Then, _currentDepth + 1);

			foreach (var elseIf in ifStmt.ElseIfs)
			{
				_result.Complexity.ConditionalStatements++;
				AnalyzeExpression(elseIf.Cond);
				AnalyzeBlock(elseIf.Body, _currentDepth + 1);
			}

			if (ifStmt.Else != null)
			{
				AnalyzeBlock(ifStmt.Else, _currentDepth + 1);
			}
		}

		private void AnalyzeLocalStatement(LocalStmt local)
		{
			AnalyzeBlock(local.Body, _currentDepth + 1);
		}

		private void AnalyzeMsgStatement(MsgStmt msg)
		{
			// MsgStmt 使用文本字段，不需要表达式分析
			// 这里可以添加对消息复杂度的统计
		}

		private void AnalyzeAssertStatement(AssertStmt assert)
		{
			AnalyzeExpression(assert.Condition);
		}

		private void AnalyzeExpression(Expr expression)
		{
			_result.Complexity.TotalExpressions++;

			switch (expression)
			{
				case BinaryExpr binary:
					AnalyzeBinaryExpression(binary);
					break;
				case UnaryExpr unary:
					AnalyzeUnaryExpression(unary);
					break;
				case CallExpr func:
					AnalyzeFunctionCall(func);
					break;
				case FieldExpr field:
					AnalyzeFieldReference(field);
					break;
				case LiteralExpr:
					// 字面量，无需特殊处理
					break;
			}
		}

		private void AnalyzeBinaryExpression(BinaryExpr binary)
		{
			switch (binary.Op)
			{
				case BinaryOp.Add:
				case BinaryOp.Sub:
				case BinaryOp.Mul:
				case BinaryOp.Div:
				case BinaryOp.Mod:
					_result.Complexity.ArithmeticOperations++;
					break;
				case BinaryOp.Eq:
				case BinaryOp.Ne:
				case BinaryOp.Lt:
				case BinaryOp.Le:
				case BinaryOp.Gt:
				case BinaryOp.Ge:
					_result.Complexity.ComparisonOperations++;
					break;
				case BinaryOp.And:
				case BinaryOp.Or:
					_result.Complexity.LogicalOperations++;
					break;
			}

			AnalyzeExpression(binary.Left);
			AnalyzeExpression(binary.Right);
		}

		private void AnalyzeUnaryExpression(UnaryExpr unary)
		{
			if (unary.Op == "!")
			{
				_result.Complexity.LogicalOperations++;
			}
			AnalyzeExpression(unary.Inner);
		}

		private void AnalyzeFunctionCall(CallExpr func)
		{
			_result.Complexity.FunctionCalls++;
			
			if (!_result.UsedFunctions.Contains(func.Name))
			{
				_result.UsedFunctions.Add(func.Name);
			}

			// 检查函数是否存在
			try
			{
				_services.Functions.Resolve(func.Name);
			}
			catch (ArgumentException)
			{
				AddWarning($"Unknown function '{func.Name}'", func.Line, func.Column, WarningType.PotentialIssue);
			}

			foreach (var arg in func.Args)
			{
				AnalyzeExpression(arg);
			}
		}

		private void AnalyzeFieldReference(FieldExpr field)
		{
			var fieldRef = new FieldReference
			{
				Name = field.Name,
				TypeHint = field.TypeAnnotation,
				Line = field.Line,
				Column = field.Column
			};

			if (!_result.ReferencedFields.Any(f => f.Name == field.Name))
			{
				_result.ReferencedFields.Add(fieldRef);
			}
		}

		private void AddWarning(string message, int line, int column, WarningType type)
		{
			_result.Warnings.Add(new ValidationWarning
			{
				Message = message,
				Line = line,
				Column = column,
				Type = type
			});
		}

		private int CountNodes(AstNode node)
		{
			int count = 1;

			switch (node)
			{
				case Block block:
					count += block.Statements.Sum(CountNodes);
					break;
				case SetStmt set:
					count += CountNodes(set.Value);
					break;
				case IfStmt ifStmt:
					count += CountNodes(ifStmt.Condition);
					count += CountNodes(ifStmt.Then);
					count += ifStmt.ElseIfs.Sum(ei => CountNodes(ei.Cond) + CountNodes(ei.Body));
					if (ifStmt.Else != null)
						count += CountNodes(ifStmt.Else);
					break;
				case LocalStmt local:
					count += CountNodes(local.Body);
					break;
				case AssertStmt assert:
					count += CountNodes(assert.Condition);
					break;
				case BinaryExpr binary:
					count += CountNodes(binary.Left) + CountNodes(binary.Right);
					break;
				case UnaryExpr unary:
					count += CountNodes(unary.Inner);
					break;
				case CallExpr func:
					count += func.Args.Sum(CountNodes);
					break;
			}

			return count;
		}
	}
}
