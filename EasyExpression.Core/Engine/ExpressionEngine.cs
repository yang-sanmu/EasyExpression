using System.Collections.Generic;
using System.Collections.Concurrent;
using EasyExpression.Core.Engine.Parsing;
using EasyExpression.Core.Engine.Runtime;

namespace EasyExpression.Core.Engine
{
	public sealed class ExpressionEngine
	{
		private readonly EngineServices _services;
		private readonly ConcurrentDictionary<string, Ast.Block> _compilationCache;
		private readonly ConcurrentDictionary<string, string[]> _linesCache;

		public ExpressionEngine(EngineServices services)
		{
			_services = services;
			_compilationCache = new ConcurrentDictionary<string, Ast.Block>();
			_linesCache = new ConcurrentDictionary<string, string[]>();
		}

		public EngineServices Services => _services;

		public Runtime.ValidationResult Validate(string script)
		{
			try
			{
				var block = GetOrCompileScript(script);
				var analyzer = new Runtime.ScriptAnalyzer(_services);
				var result = analyzer.Analyze(block);
				return result;
			}
			catch (ExpressionException ex)
			{
				var vr = new Runtime.ValidationResult
				{
					Success = false,
					ErrorMessage = ex.Message,
					ErrorLine = ex.Line,
					ErrorColumn = ex.Column,
					ErrorCode = ex.ErrorCode
				};
				if (ex.Line > 0)
				{
					var lines = GetOrCacheLines(script);
					if (ex.Line - 1 >= 0 && ex.Line - 1 < lines.Length)
						vr.ErrorSnippet = lines[ex.Line - 1];
				}
				return vr;
			}
		}

		public ExecutionResult Execute(string script, Dictionary<string, object?> inputs)
		{
			try
			{
				var block = GetOrCompileScript(script);
				var evaluator = new Evaluator(_services);
				var result = evaluator.Execute(block, inputs);
				if (result.ErrorLine > 0)
				{

                    SetErrorSnippet(script, result);
                }
				return result;

            }
			catch (ExpressionException ex)
			{
				var res = new ExecutionResult
				{
					HasError = true,
					ErrorMessage = ex.Message,
					ErrorLine = ex.Line,
					ErrorColumn = ex.Column,
					ErrorCode = ex.ErrorCode
				};
				// Generate error snippet (the line text)
				if (ex.Line > 0)
				{
					SetErrorSnippet(script, res);

                }
				return res;
			}
		}

		public Ast.Block Compile(string script)
		{
			return GetOrCompileScript(script);
		}

		public ExecutionResult Execute(Ast.Block compiled, Dictionary<string, object?> inputs)
		{
			try
			{
				var evaluator = new Evaluator(_services);
				return evaluator.Execute(compiled, inputs);
			}
			catch (ExpressionException ex)
			{
				var res = new ExecutionResult
				{
					HasError = true,
					ErrorMessage = ex.Message,
					ErrorLine = ex.Line,
					ErrorColumn = ex.Column,
					ErrorCode = ex.ErrorCode
				};
				return res;
			}
		}

		private static int CountNodes(Ast.Block block)
		{
			int count = 1; // block itself
			foreach (var s in block.Statements)
			{
				count += CountStmt(s);
			}
			return count;
		}

		private static int CountStmt(Ast.Stmt stmt)
		{
			int c = 1;
			switch (stmt)
			{
				case Ast.SetStmt s:
					c += CountExpr(s.Value); break;
				case Ast.MsgStmt:
					break;
				case Ast.ReturnStmt:
					break;
				case Ast.AssertStmt a:
					c += CountExpr(a.Condition); break;
				case Ast.IfStmt i:
					c += CountExpr(i.Condition);
					c += CountNodes(i.Then);
					foreach (var pair in i.ElseIfs)
					{
						c += CountExpr(pair.Cond);
						c += CountNodes(pair.Body);
					}
					if (i.Else != null) c += CountNodes(i.Else);
					break;
				case Ast.LocalStmt l:
					c += CountNodes(l.Body); break;
			}
			return c;
		}

		private static int CountExpr(Ast.Expr expr)
		{
			int c = 1;
			switch (expr)
			{
				case Ast.LiteralExpr:
					break;
				case Ast.FieldExpr:
					break;
				case Ast.UnaryExpr u:
					c += CountExpr(u.Inner); break;
				case Ast.BinaryExpr b:
					c += CountExpr(b.Left);
					c += CountExpr(b.Right); break;
				case Ast.CallExpr call:
					foreach (var a in call.Args) c += CountExpr(a); break;
			}
			return c;
		}

		/// <summary>
		/// Get or compile a script; uses cache to improve performance.
		/// </summary>
		private Ast.Block GetOrCompileScript(string script)
		{
			if (script == null)
				throw new ExpressionParseException("Script cannot be null", ExpressionErrorCode.SyntaxError);

			// Handle empty script - return an empty block
			if (script.Length == 0)
			{
				return new Ast.Block(1, 1); // Empty block, position at line 1, column 1
			}

			// Check cache
			if (_services.Options.EnableCompilationCache && _compilationCache.TryGetValue(script, out var cached))
			{
				return cached;
			}

			// Compile new script
			var parser = new Parser(script, _services.Options);
			var block = parser.ParseBlock();
			var totalNodes = CountNodes(block);
			if (totalNodes > _services.Options.MaxNodes)
				throw new ExpressionLimitException($"Script too large: nodes={totalNodes} > MaxNodes={_services.Options.MaxNodes}", ExpressionErrorCode.ScriptTooLarge, block.Line, block.Column);

			// Cache compiled result
			if (_services.Options.EnableCompilationCache)
			{
				_compilationCache.TryAdd(script, block);
			}

			return block;
		}

		/// <summary>
		/// Get or cache script lines for error reporting.
		/// </summary>
		private string[] GetOrCacheLines(string script)
		{
			if (string.IsNullOrEmpty(script))
				return new string[0];

			if (_linesCache.TryGetValue(script, out var cached))
			{
				return cached;
			}

			var lines = script.Replace("\r\n", "\n").Split('\n');
			_linesCache.TryAdd(script, lines);
			return lines;
		}

		private void SetErrorSnippet(string script, ExecutionResult result)
		{
            var lines = GetOrCacheLines(script);
            if (result.ErrorLine - 1 >= 0 && result.ErrorLine - 1 < lines.Length)
                result.ErrorSnippet = lines[result.ErrorLine - 1];
        }

        /// <summary>
        /// Clear compilation cache.
        /// </summary>
        public void ClearCache()
		{
			_compilationCache.Clear();
			_linesCache.Clear();
		}
	}
}


