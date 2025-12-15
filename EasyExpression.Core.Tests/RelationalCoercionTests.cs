using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class RelationalCoercionTests
	{
		private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
		{
			var options = new ExpressionEngineOptions();
			cfg?.Invoke(options);
			var services = new EngineServices(options);
			return new ExpressionEngine(services);
		}

		[Fact]
		public void Both_NonNumeric_And_NonDatetime_Treated_As_Number_Via_Converters()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, '1' < '2')
				set(b, '2' <= '2')
				set(c, '3' > '2')
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(true);
			res.Assignments["c"].ShouldBe(true);
		}

		[Fact]
		public void Number_With_NonNumeric_Treated_As_Number_Conversion_Ok()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, 10 < '20')
				set(b, '30' >= 30)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(true);
		}

		[Fact]
		public void Number_With_NonNumeric_Treated_As_Number_Conversion_Fails()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, 10 < 'xx')
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Cannot convert value 'xx' to decimal");
		}

		[Fact]
		public void Datetime_With_NonDatetime_Treated_As_Datetime_Conversion_Ok()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, '2024-01-01 00:00:00' < now)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(true);
		}

		[Fact]
		public void Datetime_With_Number_Should_Error()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, now < 1)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("cannot compare datetime with number");
		}

		[Fact]
		public void Datetime_Conversion_Fails_Should_Error()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, 'bad' < now)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Cannot convert value 'bad' to datetime");
		}
	}
}


