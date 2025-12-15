using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class TextFunctionTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void Substring_With_Start_Only()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello', 2))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe("llo");
        }

        [Fact]
        public void Substring_Start_OutOfRange_Should_Error()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello', 10))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeTrue();
            res.ErrorMessage.ShouldContain("Substring start index 10 is beyond string length 5");
        }

        [Fact]
        public void Substring_Negative_Start_Should_Error()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello', -1))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeTrue();
            res.ErrorMessage.ShouldContain("Function 'Substring' argument 2 must be at least 0, but got -1");
        }

        [Fact]
        public void Substring_Length_OutOfRange_Should_Error()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello', 2, 10))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeTrue();
            res.ErrorMessage.ShouldContain("Substring start index 2 + length 10 = 12 is beyond string length 5");
        }

        [Fact]
        public void Substring_Negative_Length_Should_Error()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello', 2, -1))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeTrue();
            res.ErrorMessage.ShouldContain("Function 'Substring' argument 3 must be at least 0, but got -1");
        }

        [Fact]
        public void Substring_With_Start_And_Length()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello', 1, 3))
                set(b, Substring('hello', 0, 5))
                set(c, Substring('hello', 0, 0))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe("ell");
            res.Assignments["b"].ShouldBe("hello");
            res.Assignments["c"].ShouldBe("");
        }

        [Fact]
        public void Substring_Empty_String()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('', 0))
                set(b, Substring('', 0, 0))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe("");
            res.Assignments["b"].ShouldBe("");
        }

        [Fact]
        public void Substring_Wrong_Argument_Count_Should_Error()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello'))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeTrue();
            res.ErrorMessage.ShouldContain("Function 'Substring' expects at least 2 argument(s), but got 1");
        }

        [Fact]
        public void Substring_Too_Many_Arguments_Should_Error()
        {
            var e = CreateEngine();
            var script = @"
            {
                set(a, Substring('hello', 0, 1, 2))
            }";
            var res = e.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeTrue();
            res.ErrorMessage.ShouldContain("Function 'Substring' expects at most 3 argument(s), but got 4");
        }

		[Fact]
		public void Replace_IgnoreCase_With_Timeout_Works()
		{
			var e = CreateEngine(o => o.RegexTimeoutMilliseconds = 5);
			var script = @"
			{
				set(a, Replace('AaAa', 'a', 'b', 'i'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe("bbbb");
		}
    }
}

