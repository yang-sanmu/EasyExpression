using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class UnaryLogicalNotTests
	{
		private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
		{
			var options = new ExpressionEngineOptions();
			cfg?.Invoke(options);
			var services = new EngineServices(options);
			return new ExpressionEngine(services);
		}

		[Fact]
		public void Not_True_Is_False()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, !true)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(false);
		}

		[Fact]
		public void Not_False_Is_True()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, !false)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(true);
		}

		[Fact]
		public void Not_With_Grouping_And_Or()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, !(false || true))
				set(b, !false && false)
				set(c, !(false && false) || false)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(false);
			res.Assignments["b"].ShouldBe(false);
			res.Assignments["c"].ShouldBe(true);
		}

		[Fact]
		public void Not_On_NonBoolean_Should_Error()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, !'x')
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Expected boolean in logical operation");
		}
	}
}



