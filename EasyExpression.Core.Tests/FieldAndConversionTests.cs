using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class FieldAndConversionTests
	{
		private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
		{
			var options = new ExpressionEngineOptions();
			cfg?.Invoke(options);
			var services = new EngineServices(options);
			return new ExpressionEngine(services);
		}

		[Fact]
		public void Field_Default_String_And_Type_Annotation()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?> { {"A", "10" }, {"D", "2024-01-01 00:00:00"} };
			var script = @"
			{
				set(x, [A] + '1')
				set(y, [A:decimal] + 1)
				set(z, [D:datetime] == ToDateTime('2024-01-01 00:00:00'))
			}";
			var res = e.Execute(script, inputs);
			res.Assignments["x"].ShouldBe("101");
			res.Assignments["y"].ShouldBe(11m);
			res.Assignments["z"].ShouldBe(true);
		}

		[Fact]
		public void Set_With_Field_Name_In_Brackets_Allows_Spaces()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?>();
			var script = @"
			{
				set([field name], 'x')
			}";
			var res = e.Execute(script, inputs);
			res.Assignments["field name"].ShouldBe("x");
		}

		[Fact]
		public void Custom_FieldName_Validator_Allows_Dash()
		{
			var e = CreateEngine(o =>
			{
				o.FieldNameValidator = name => name.IndexOf('-') >= 0 || name.IndexOf(' ') >= 0 || name.AsSpan().IndexOfAny("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray()) >= 0;
			});
			var inputs = new Dictionary<string, object?>();
			var script = @"
			{
				set([field-name], 'x')
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeFalse();
			res.Assignments["field-name"].ShouldBe("x");
		}

		[Fact]
		public void String_Literal_Escape_Sequences()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(a, 'line1\nline2')
				set(b, 'tab\tend')
				set(c, 'quote\'ok')
				set(d, '1\n\n2')
				set(e, '1\'')
				set(f, '1\t')
				set(g, '1\n\n')
				set(h, '\n\n1')
				set(i, '\'1')
				set(j, '\t1')
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe("line1\nline2");
			res.Assignments["b"].ShouldBe("tab\tend");
			res.Assignments["c"].ShouldBe("quote'ok");
            res.Assignments["d"].ShouldBe("1\n\n2");
            res.Assignments["e"].ShouldBe("1'");
            res.Assignments["f"].ShouldBe("1\t");
            res.Assignments["g"].ShouldBe("1\n\n");
            res.Assignments["h"].ShouldBe("\n\n1");
            res.Assignments["i"].ShouldBe("'1");
            res.Assignments["j"].ShouldBe("\t1");
        }

		[Fact]
		public void Unknown_Field_Should_Error()
		{
			var e = CreateEngine();
			var script = @"
			{
				set(x, [NotExist])
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
		}

		[Fact]
		public void Converter_Fails_Should_Set_Error()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?> { {"A", "not-a-number"} };
			var script = @"
			{
				set(x, [A:decimal])
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("not-a-number");
			res.ErrorMessage.ShouldContain("Cannot convert field A");
			res.ErrorLine.ShouldBeGreaterThan(0);
			res.ErrorColumn.ShouldBeGreaterThan(0);
		}

		[Fact]
		public void Validate_Should_Fail_When_Nodes_Exceed_MaxNodes()
		{
			var e = CreateEngine(o => o.MaxNodes = 2);
			var script = @"{ set(a, 1) set(b, 2) set(c, 3) }";
			var vr = e.Validate(script);
			vr.Success.ShouldBeFalse();
			vr.ErrorMessage.ShouldContain("Script too large");
		}

		[Fact]
		public void Unknown_Type_Annotation_Should_Error()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?> { {"A", "10"} };
			var script = @"
			{
				set(x, [A:unknownType])
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Unknown type annotation");
		}

		[Fact]
		public void Validate_Should_Succeed_For_Simple_Script()
		{
			var e = CreateEngine();
			var vr = e.Validate("{ set(a, 1+2) }");
			vr.Success.ShouldBeTrue();
			vr.TotalNodes.ShouldBeGreaterThan(0);
		}

		[Fact]
		public void Null_Decimal_Bool_DateTime_Behavior_By_Options()
		{
			var e = CreateEngine(opt =>
			{
				opt.TreatNullDecimalAsZero = true;
				opt.TreatNullBoolAsFalse = true;
				opt.NullDateTimeDefault = new DateTime(2024,1,1,0,0,0);
			});
			var inputs = new Dictionary<string, object?>
			{
				{"ND", null},
				{"NB", null},
				{"NT", null}
			};
			var script = @"
			{
				set(a, [ND:decimal])
				set(b, [NB:bool])
				set(c, [NT:datetime])
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(0m);
			res.Assignments["b"].ShouldBe(false);
			res.Assignments["c"].ShouldBe(new DateTime(2024,1,1,0,0,0));
		}

		[Fact]
		public void BuiltIn_Number_To_Decimal_Converters_Sample()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?>
			{
				{"I", 1},
				{"L", 2L},
				{"D", 3.5},
				{"F", 4.5f}
			};
			var script = @"
			{
				set(a, [I:decimal] + [L:decimal])
				set(b, [D:decimal])
				set(c, [F:decimal])
			}";
			var res = e.Execute(script, inputs);
			res.Assignments["a"].ShouldBe(3m);
			res.Assignments["b"].ShouldBe(3.5m);
			res.Assignments["c"].ShouldBe(4.5m);
		}

		[Fact]
		public void Set_With_Typed_Target_Performs_Conversion()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?>();
			var script = @"
			{
				set([amount:decimal], '123.45')
				set([flag:bool], 'true')
				set([dt:datetime], '2025-08-11 12:12:12')
				set([text:string], 789)
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeFalse();
			res.Assignments["amount"].ShouldBe(123.45m);
			res.Assignments["flag"].ShouldBe(true);
			res.Assignments["dt"].ShouldBe(new DateTime(2025,8,11,12,12,12));
			res.Assignments["text"].ShouldBe("789");
		}

		[Fact]
		public void Set_With_Typed_Target_Conversion_Fails_Should_Error()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?>();
			var script = @"
			{
				set([amount:decimal], 'NaN')
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Cannot convert value");
		}

		[Fact]
		public void Set_Without_Type_Should_Remain_Compatible_With_Old_Syntax()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?>();
			var script = @"
			{
				set(a, '123456')
				set([b], '654321')
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe("123456");
			res.Assignments["b"].ShouldBe("654321");
		}

		[Fact]
		public void Bool_Type_Annotation_Invalid_String_Should_Error()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?> { {"B", "not-bool"} };
			var script = @"
			{
				set(x, [B:bool])
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Cannot convert field B value 'not-bool' to bool");
			res.ErrorMessage.ShouldContain("bool");
		}

		[Fact]
		public void DateTime_Type_Annotation_Invalid_String_Should_Error()
		{
			var e = CreateEngine();
			var inputs = new Dictionary<string, object?> { {"T", "2024/01/01"} }; // Does not match the default format
			var script = @"
			{
				set(x, [T:datetime])
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeTrue();
			res.ErrorMessage.ShouldContain("Cannot convert field T value '2024/01/01' to datetime");
			res.ErrorMessage.ShouldContain("datetime");
		}
	}
}


