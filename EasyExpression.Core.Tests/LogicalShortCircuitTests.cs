using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class LogicalShortCircuitTests
	{
		private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
		{
			var options = new ExpressionEngineOptions();
			cfg?.Invoke(options);
			var services = new EngineServices(options);
			return new ExpressionEngine(services);
		}

		[Fact]
		public void Or_Should_ShortCircuit_When_Left_True()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, true || NotAFunction(1))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(true);
		}

		[Fact]
		public void And_Should_ShortCircuit_When_Left_False()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, false && NotAFunction(1))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(false);
		}

		[Fact]
		public void Or_Should_Evaluate_Right_When_Left_False_And_Error()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, false || NotAFunction(1))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Unknown function");
		}

		[Fact]
		public void And_Should_Evaluate_Right_When_Left_True_And_Error()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, true && NotAFunction(1))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Unknown function");
		}
	}
}




