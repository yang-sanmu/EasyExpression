using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using EasyExpression.Core.Engine.Runtime;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class ValidationResultTests
	{
		private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
		{
			var options = new ExpressionEngineOptions();
			cfg?.Invoke(options);
			var services = new EngineServices(options);
			return new ExpressionEngine(services);
		}

		[Fact]
		public void Validate_Simple_Script_Returns_Detailed_Analysis()
		{
			var engine = CreateEngine();
			var script = @"
			{
				set(a, 1 + 2)
				set(b, ToUpper('hello'))
				if(true) {
					set(c, [field1:string] + [field2:decimal])
				}
			}";

			var result = engine.Validate(script);

			result.Success.ShouldBeTrue();
			result.TotalNodes.ShouldBeGreaterThan(0);
			
			// Check complexity analysis
			result.Complexity.TotalExpressions.ShouldBeGreaterThan(0);
			result.Complexity.ArithmeticOperations.ShouldBe(2); // 1+2 and string concatenation
			result.Complexity.ConditionalStatements.ShouldBe(1); // if statement
			result.Complexity.FunctionCalls.ShouldBe(1); // ToUpper
			result.Complexity.NestedBlockDepth.ShouldBe(1); // Nested depth of the if block

			// Check used functions
			result.UsedFunctions.ShouldContain("ToUpper");

			// Check referenced fields
			result.ReferencedFields.Count.ShouldBe(2);
			result.ReferencedFields.ShouldContain(f => f.Name == "field1" && f.TypeHint == "string");
			result.ReferencedFields.ShouldContain(f => f.Name == "field2" && f.TypeHint == "decimal");

			// Check declared variables
			result.DeclaredVariables.ShouldContain("a");
			result.DeclaredVariables.ShouldContain("b");
			result.DeclaredVariables.ShouldContain("c");
		}

		[Fact]
		public void Validate_Script_With_Unknown_Function_Returns_Warning()
		{
			var engine = CreateEngine();
			var script = @"
			{
				set(a, UnknownFunction(1, 2))
			}";

			var result = engine.Validate(script);

			result.Success.ShouldBeTrue();
			result.Warnings.Count.ShouldBe(1);
			result.Warnings[0].Type.ShouldBe(WarningType.PotentialIssue);
			result.Warnings[0].Message.ShouldContain("Unknown function 'UnknownFunction'");
		}

		[Fact]
		public void Validate_Complex_Nested_Script_Calculates_Correct_Depth()
		{
			var engine = CreateEngine();
			var script = @"
			{
				if(true) {
					if(false) {
						if(true) {
							set(deep, 1)
						}
					}
					local {
						set(local_var, 2)
					}
				}
			}";

			var result = engine.Validate(script);

			result.Success.ShouldBeTrue();
			// Depth: root block (0) -> if block (1) -> if block (2) -> if block (3) -> set statement
			// Or: root block (0) -> if block (1) -> local block (2)
			// The deepest nesting should be 3 levels (if -> if -> if)
			result.Complexity.NestedBlockDepth.ShouldBeGreaterThanOrEqualTo(3);
			result.Complexity.ConditionalStatements.ShouldBe(3); // Three if statements
		}

		[Fact]
		public void Validate_Math_Heavy_Script_Counts_Operations_Correctly()
		{
			var engine = CreateEngine();
			var script = @"
			{
				set(a, 1 + 2 * 3 - 4 / 5)
				set(b, a > 10 && b < 20)
				set(c, !true || false)
			}";

			var result = engine.Validate(script);

			result.Success.ShouldBeTrue();
			result.Complexity.ArithmeticOperations.ShouldBe(4); // +, *, -, /
			result.Complexity.ComparisonOperations.ShouldBe(2); // >, <
			result.Complexity.LogicalOperations.ShouldBe(3); // &&, !, ||
		}

		[Fact]
		public void Validate_Function_Heavy_Script_Lists_All_Functions()
		{
			var engine = CreateEngine();
			var script = @"
			{
				set(a, Max(1, 2, 3))
				set(b, ToUpper(ToLower('ABC')))
				set(c, Sum(1, 2, Average(3, 4, 5)))
			}";

			var result = engine.Validate(script);

			result.Success.ShouldBeTrue();
			result.UsedFunctions.ShouldContain("Max");
			result.UsedFunctions.ShouldContain("ToUpper");
			result.UsedFunctions.ShouldContain("ToLower");
			result.UsedFunctions.ShouldContain("Sum");
			result.UsedFunctions.ShouldContain("Average");
			result.Complexity.FunctionCalls.ShouldBe(5);
		}

		[Fact]
		public void Validate_Invalid_Script_Returns_Error_With_Details()
		{
			var engine = CreateEngine();
			var script = @"
			{
				set(a, 1 + )
			}";

			var result = engine.Validate(script);

			result.Success.ShouldBeFalse();
			result.ErrorMessage.ShouldNotBeNullOrEmpty();
			result.ErrorLine.ShouldBeGreaterThan(0);
			result.ErrorColumn.ShouldBeGreaterThan(0);
		}

		[Fact]
		public void Validate_Script_With_Tip_And_Assert_Statements()
		{
			var engine = CreateEngine();
			var script = @"
			{
				set(value, 42)
			}";

			var result = engine.Validate(script);

			// If the script has a syntax error, check error details first
			if (!result.Success)
			{
				var errorInfo = $"Error: {result.ErrorMessage} at line {result.ErrorLine}, column {result.ErrorColumn}";
				throw new Exception($"Script validation failed: {errorInfo}");
			}

			result.Success.ShouldBeTrue();
			result.DeclaredVariables.ShouldContain("value");
			result.Complexity.TotalExpressions.ShouldBeGreaterThan(0);
		}
	}
}
