using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class ArithmeticAndComparisonTests
	{
		private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
		{
			var options = new ExpressionEngineOptions();
			cfg?.Invoke(options);
			var services = new EngineServices(options);
			return new ExpressionEngine(services);
		}

        [Fact]
        public void Set_Can_Create_New_Field()
        {
            var eng = CreateEngine();
            var script = @"
			{
				set(a, 1+1) //add new filed a
				set(b, [a]+2)
			}";
            var res = eng.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe(2m);
            res.Assignments["b"].ShouldBe(4m);
        }

        [Fact]
		public void Add_Sub_Mul_Div_Mod_With_Decimals()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, 1+2*3)
				set(b, (1+2)*3)
				set(c, 7%4)
				set(d, 8/2)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(7m);
			res.Assignments["b"].ShouldBe(9m);
			res.Assignments["c"].ShouldBe(3m);
			res.Assignments["d"].ShouldBe(4m);
		}

		[Fact]
		public void Max_Depth_And_Timeout_Limits()
		{
			var eng = CreateEngine(o => { o.MaxDepth = 2; o.TimeoutMilliseconds = 1; });
			var script = @"
			{
				set(a, (((1+2)+3)+4))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Max depth exceeded");
		}

		[Fact]
		public void String_Concat_When_Either_Is_String()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(x, 'aa' + 1)
				set(y, 1 + 'bb')
				set(z, 'a'+'b')
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["x"].ShouldBe("aa1");
			res.Assignments["y"].ShouldBe("1bb");
			res.Assignments["z"].ShouldBe("ab");
		}

		[Fact]
		public void String_Concat_PreferNumericIfParsable_Sums_When_Both_Parsable()
		{
			var eng = CreateEngine(o => o.StringConcat = StringConcatMode.PreferNumericIfParsable);
			var script = @"
			{
				set(a, '1' + 2)
				set(b, 1 + '2')
				set(c, 'a' + 1)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(3m);
			res.Assignments["b"].ShouldBe(3m);
			res.Assignments["c"].ShouldBe("a1");
		}

		[Fact]
		public void String_Concat_Uses_Converters_For_NonString()
		{
			var eng = CreateEngine(o => { });
			var script = @"
			{
				set(a, ToDateTime('2024-01-01 00:00:00') + ' UTC')
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe("2024-01-01 00:00:00 UTC");
		}

		[Fact]
		public void Unary_Negative_Number()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, -1)
				set(b, -(1+2))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(-1m);
			res.Assignments["b"].ShouldBe(-3m);
		}

		[Fact]
		public void Literals_Are_Case_Sensitive()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, TRUE)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
		}

		[Fact]
		public void Relational_For_Number_And_DateTime()
		{
			var eng = CreateEngine();
			var inputs = new Dictionary<string, object?>
			{
				{"n1", 5m},
				{"n2", 10m},
				{"d1", new DateTime(2024,1,1,0,0,0)},
				{"d2", new DateTime(2024,1,2,0,0,0)}
			};
			var script = @"
			{
				set(a, [n1:decimal] < [n2:decimal])
				set(b, [d1:datetime] < [d2:datetime])
				set(c, [n1] < '20')
				set(d, '2024-01-01 00:00:00' < [d2:datetime])
			}";
			var res = eng.Execute(script, inputs);
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(true);
			res.Assignments["c"].ShouldBe(true);
			res.Assignments["d"].ShouldBe(true);
		}

		[Fact]
		public void Relational_Invalid_Types_Should_Error()
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
		public void Equality_String_Uses_Global_Comparison()
		{
			var eng = CreateEngine(o => o.StringComparison = StringComparison.OrdinalIgnoreCase);
			var script = @"
			{
				set(a, 'Abc' == 'abc')
				set(b, 'Abc' != 'abc')
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(false);
		}

		[Fact]
		public void Equality_NumberFriendly_Allows_String_Number_Compare()
		{
			var eng = CreateEngine(o => o.EqualityCoercion = EqualityCoercionMode.NumberFriendly);
			var script = @"
			{
				set(a, '1' == 1)
				set(b, '1.5' == 1.5)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(true);
			res.Assignments["b"].ShouldBe(true);
		}

		[Fact]
		public void Equality_Type_Mismatch_Should_Error()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, 1 == ToDateTime('2024-01-01 00:00:00'))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("type mismatch");
		}

		[Fact]
		public void Equality_Permissive_Fallsback_To_String_When_Not_Number()
		{
			var eng = CreateEngine(o => o.EqualityCoercion = EqualityCoercionMode.Permissive);
			var script = @"
			{
				set(a, '2024' == ToDateTime('2024-01-01 00:00:00'))
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(false);
		}

		[Fact]
		public void Divide_By_Zero_Should_Report_Location()
		{
			var eng = CreateEngine();
			var script = "set(a, 8/0)";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Divide by zero");
			res.ErrorLine.ShouldBe(1);
			res.ErrorColumn.ShouldBe(9);
		}

		[Fact]
		public void Modulo_By_Zero_Should_Report_Location()
		{
			var eng = CreateEngine();
			var script = "set(a, 8%0)";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Modulo by zero");
			res.ErrorLine.ShouldBe(1);
			res.ErrorColumn.ShouldBeGreaterThan(0);
		}
	}
}


