using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class FunctionTests
	{
		private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
		{
			var options = new ExpressionEngineOptions();
			cfg?.Invoke(options);
			var services = new EngineServices(options);
			return new ExpressionEngine(services);
		}

		[Fact]
		public void String_Functions_Basic()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, StartsWith('Abc','a'))
				set(b, EndsWith('Abc','BC'))
				set(c, Contains('Abc','b'))
				set(d, ToUpper('a'))
				set(e, ToLower('A'))
				set(f, Substring('hello',1,3))
				set(g, Trim('  a  '))
				set(h, Len('xyz'))
				set(i, Replace('Aa','a','b','i'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(true);
			res.Assignments["c"].ShouldBe(true);
			res.Assignments["d"].ShouldBe("A");
			res.Assignments["e"].ShouldBe("a");
			res.Assignments["f"].ShouldBe("ell");
			res.Assignments["g"].ShouldBe("a");
			res.Assignments["h"].ShouldBe(3m);
			res.Assignments["i"].ShouldBe("bb");
		}

		[Fact]
		public void Function_Name_Is_Case_Insensitive()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, startswith('Abc','a'))
				set(b, CONTAINS('Abc','B'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(true);
		}

		[Fact]
		public void Math_Functions_Basic()
		{
			var e = CreateEngine(o => { o.RoundingDigits = 2; o.MidpointRounding = MidpointRounding.AwayFromZero; });
			var script = @"
			{
				set(a, Max(1,2,3))
				set(b, Min(1,2,3))
				set(c, Average(1,2,3))
				set(d, Sum(1,2,3))
				set(e, Round(2.345))
				set(f, Round(2.345, 1))
				set(g, Abs(-5))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(3m);
			res.Assignments["b"].ShouldBe(1m);
			((decimal)res.Assignments["c"]!).ShouldBe(2m);
			res.Assignments["d"].ShouldBe(6m);
			res.Assignments["e"].ShouldBe(2.35m);
			res.Assignments["f"].ShouldBe(2.3m);
			res.Assignments["g"].ShouldBe(5m);
		}

		[Fact]
		public void DateTime_Functions_Basic()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, ToDateTime('2024-01-01 00:00:00'))
				set(b, AddDays(ToDateTime('2024-01-01 00:00:00'), 1))
				set(b2, AddDay(ToDateTime('2024-01-01 00:00:00'), 1))
				set(c, AddHours(ToDateTime('2024-01-01 00:00:00'), 2))
				set(d, TimeSpan(ToDateTime('2024-01-01 00:00:00'), ToDateTime('2024-01-02 00:00:00'), 'd'))
				set(e, FormatDateTime(ToDateTime('2024-01-01 00:00:00'), 'yyyyMMdd'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(new DateTime(2024,1,1,0,0,0));
			res.Assignments["b"].ShouldBe(new DateTime(2024,1,2,0,0,0));
			res.Assignments["b2"].ShouldBe(new DateTime(2024,1,2,0,0,0));
			res.Assignments["c"].ShouldBe(new DateTime(2024,1,1,2,0,0));
			res.Assignments["d"].ShouldBe(1m);
			res.Assignments["e"].ShouldBe("20240101");
		}

		[Fact]
		public void RegexMatch_Options()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, RegexMatch('Abc','a'))
				set(b, RegexMatch('Abc','^a', 'm'))
				set(c, RegexMatch('Abc','^a', 'i'))
				set(d, RegexMatch('Abc','^a', 'im'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(true); // 受全局 StringComparison 影响，默认忽略大小写
			res.Assignments["b"].ShouldBe(false);
			res.Assignments["c"].ShouldBe(true);
			res.Assignments["d"].ShouldBe(true);
		}


		[Fact]
		public void RegexMatch_Options1()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, RegexMatch('1.0','^-?(0|[1-9]\d*)\.\d$'))
				set(b, RegexMatch('1.1.1','^-?(0|[1-9]\d*)\.\d$'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(true); // 受全局 StringComparison 影响，默认忽略大小写
			res.Assignments["b"].ShouldBe(false);
		}

		[Fact]
		public void RegexMatch_Should_Preserve_Backslashes()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, RegexMatch('1.0','^\d+\.\d$'))
				set(b, RegexMatch('foo+bar','foo\+bar'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(true);
		}


		[Fact]
		public void RegexMatch_Should_Honor_Timeout()
		{
			var e = CreateEngine(o => o.RegexTimeoutMilliseconds = 10);
			var evil = @"^(a+)+$"; // 经典灾难回溯模式
			var input = new string('a', 5000) + "b"; // 末尾不匹配触发回溯
			var script = $"{{ set(a, RegexMatch('{input}','{evil}')) }}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("RegexMatch operation timed out");
		}

		[Fact]
		public void Substring_Out_Of_Range_Should_Error()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, Substring('abc', 2, 5))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
		}

		[Fact]
		public void Now_Uses_LocalTime_When_Enabled()
		{
			var e = CreateEngine(o => o.NowUseLocalTime = true);
			var script = @"
			{
				set(a, now)
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			var actual = (DateTime)res.Assignments["a"]!;
			actual.Kind.ShouldBe(DateTimeKind.Local);
			var delta = Math.Abs((DateTime.Now - actual).TotalSeconds);
			(delta < 10).ShouldBeTrue();
		}

		[Fact]
		public void Round_Default_Digits_From_Options()
		{
			var e = CreateEngine(o => { o.RoundingDigits = 1; o.MidpointRounding = MidpointRounding.AwayFromZero; });
			var script = @"
			{
				set(a, Round(2.34))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(2.3m);
		}

		[Fact]
		public void Now_Uses_Utc_When_Disabled()
		{
			var e = CreateEngine(o => o.NowUseLocalTime = false);
			var script = @"
			{
				set(a, now)
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			var actual = (DateTime)res.Assignments["a"]!;
			actual.Kind.ShouldBe(DateTimeKind.Utc);
			var delta = Math.Abs((DateTime.UtcNow - actual).TotalSeconds);
			(delta < 10).ShouldBeTrue();
		}

		[Fact]
		public void Unknown_Function_Should_Error()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, NotAFunction(1,2))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Unknown function");
		}
	}
}


