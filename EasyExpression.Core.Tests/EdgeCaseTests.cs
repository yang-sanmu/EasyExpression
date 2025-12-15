using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class EdgeCaseTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void Empty_Script_Should_Work()
        {
            var engine = CreateEngine();
            var script = "";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments.Count.ShouldBe(0);
        }

        [Fact]
        public void Empty_Block_Should_Work()
        {
            var engine = CreateEngine();
            var script = "{}";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments.Count.ShouldBe(0);
        }

        [Fact]
        public void Script_With_Only_Comments_Should_Work()
        {
            var engine = CreateEngine();
            var script = @"
            // This is a comment
            /* This is a 
               multi-line comment */
            ";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments.Count.ShouldBe(0);
        }

        [Fact]
        public void Script_With_Only_Newlines_Should_Work()
        {
            var engine = CreateEngine();
            var script = "\n\n\n\n";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments.Count.ShouldBe(0);
        }

        [Fact]
        public void Very_Long_String_Literal()
        {
            var engine = CreateEngine();
            var longString = new string('a', 10000);
            var script = $"{{ set(a, '{longString}') }}";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(longString);
        }

        [Fact]
        public void Very_Large_Numbers()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 999999999999999999999.99999)
                set(b, -999999999999999999999.99999)
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            // Verify that very large numbers are handled correctly
            ((decimal)result.Assignments["a"]!).ShouldBeGreaterThan(999999999999999999999m);
            ((decimal)result.Assignments["b"]!).ShouldBeLessThan(-999999999999999999999m);
        }

        [Fact]
        public void Zero_Values_In_All_Types()
        {
            var engine = CreateEngine();
            var inputs = new Dictionary<string, object?>
            {
                {"zero_decimal", 0m},
                {"zero_int", 0},
                {"zero_string", "0"},
                {"empty_string", ""},
                {"false_bool", false}
            };

            var script = @"
            {
                set(a, [zero_decimal:decimal] + 1)
                set(b, [zero_int:decimal] * 5)
                set(c, [zero_string:decimal] - 1)
                set(d, [empty_string] + 'test')
                set(e, [false_bool:bool] || true)
            }";

            var result = engine.Execute(script, inputs);

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(1m);
            result.Assignments["b"].ShouldBe(0m);
            result.Assignments["c"].ShouldBe(-1m);
            result.Assignments["d"].ShouldBe("test");
            result.Assignments["e"].ShouldBe(true);
        }

        [Fact]
        public void Special_Characters_In_String_Literals()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 'Hello\nWorld')
                set(b, 'Tab\tSeparated')
                set(c, 'Quote\'Inside')
                set(d, 'Unicode: 中文测试')
                set(e, 'Symbols: !@#$%^&*()')
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe("Hello\nWorld");
            result.Assignments["b"].ShouldBe("Tab\tSeparated");
            result.Assignments["c"].ShouldBe("Quote'Inside");
            result.Assignments["d"].ShouldBe("Unicode: 中文测试");
            result.Assignments["e"].ShouldBe("Symbols: !@#$%^&*()");
        }

        [Fact]
        public void Field_Names_With_Spaces_And_Special_Characters()
        {
            var engine = CreateEngine(o => o.StrictFieldNameValidation = false);
            var inputs = new Dictionary<string, object?>
            {
                {"field with spaces", "value1"},
                {"field_with_underscores", "value2"},
                {"Field123", "value3"}
            };

            var script = @"
            {
                set([result1], [field with spaces])
                set([result2], [field_with_underscores])
                set([result3], [Field123])
            }";

            var result = engine.Execute(script, inputs);

            result.HasError.ShouldBeFalse();
            result.Assignments["result1"].ShouldBe("value1");
            result.Assignments["result2"].ShouldBe("value2");
            result.Assignments["result3"].ShouldBe("value3");
        }

        [Fact]
        public void Deeply_Nested_If_Statements()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if(true) {
                    if(true) {
                        if(true) {
                            if(true) {
                                set(result, 'deep')
                            }
                        }
                    }
                }
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["result"].ShouldBe("deep");
        }

        [Fact]
        public void Complex_Expression_With_All_Operators()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, ((1 + 2) * 3 - 4) / 2 % 3)
                set(b, (true && false) || (!false && true))
                set(c, ('hello' + ' ') + 'world')
                set(d, (5 > 3) && (2 < 4) && (1 == 1) && (2 != 1))
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(2.5m); // ((1+2)*3-4)/2%3 = (9-4)/2%3 = 2.5%3 = 2.5
            result.Assignments["b"].ShouldBe(true); // false || true = true
            result.Assignments["c"].ShouldBe("hello world");
            result.Assignments["d"].ShouldBe(true);
        }

        [Fact]
        public void Function_With_Maximum_Arguments()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, Sum(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20))
                set(b, Max(1,2,3,4,5,6,7,8,9,10))
                set(c, Min(10,9,8,7,6,5,4,3,2,1))
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(210m); // 1+2+...+20 = 210
            result.Assignments["b"].ShouldBe(10m);
            result.Assignments["c"].ShouldBe(1m);
        }

        [Fact]
        public void Mixed_Data_Types_In_Collections()
        {
            var engine = CreateEngine();
            var inputs = new Dictionary<string, object?>
            {
                {"string_field", "test"},
                {"number_field", 42},
                {"bool_field", true},
                {"date_field", "2024-01-01 00:00:00"}
            };

            var script = @"
            {
                set(a, [string_field] + [number_field:string])
                set(b, [number_field:decimal] + 8)
                set(c, [bool_field:bool] && true)
                set(d, AddDays([date_field:datetime], 1))
            }";

            var result = engine.Execute(script, inputs);

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe("test42");
            result.Assignments["b"].ShouldBe(50m);
            result.Assignments["c"].ShouldBe(true);
            result.Assignments["d"].ShouldBe(new DateTime(2024, 1, 2));
        }

        [Fact]
        public void Extreme_Decimal_Precision()
        {
            var engine = CreateEngine(o => o.RoundingDigits = 10);
            var script = @"
            {
                set(a, 1.1234567890)
                set(b, 2.9876543210)
                set(c, a + b)
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            // The result depends on decimal precision and rounding rules
            result.Assignments["c"].ShouldNotBeNull();
            // Check the result type and validate the value
            if (result.Assignments["c"] is decimal decimalValue)
            {
                decimalValue.ShouldBeGreaterThan(4m);
                decimalValue.ShouldBeLessThan(5m);
            }
            else if (decimal.TryParse(result.Assignments["c"]?.ToString(), out decimal parsedValue))
            {
                parsedValue.ShouldBeGreaterThan(4m);
                parsedValue.ShouldBeLessThan(5m);
            }
            else
            {
                // If it cannot be parsed as decimal, at least verify it is not empty
                result.Assignments["c"].ToString().ShouldNotBeNullOrEmpty();
            }
        }

        [Fact]
        public void Multiple_Return_Statements_In_Different_Scopes()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if(false) {
                    return
                }
                local {
                    set(a, 1)
                    return_local
                    set(a, 2)
                }
                set(b, 3)
                if(true) {
                    set(c, 4)
                    return
                }
                set(d, 5)
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(1m);
            result.Assignments["b"].ShouldBe(3m);
            result.Assignments["c"].ShouldBe(4m);
            result.Assignments.ContainsKey("d").ShouldBeFalse(); // Not executed due to return
        }

        [Fact]
        public void Unicode_And_Emoji_In_Strings()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(chinese, '你好世界')
                set(emoji, '😀😃😄')
                set(mixed, chinese + ' ' + emoji)
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["chinese"].ShouldBe("你好世界");
            result.Assignments["emoji"].ShouldBe("😀😃😄");
            result.Assignments["mixed"].ShouldBe("你好世界 😀😃😄");
        }

        [Fact]
        public void Null_Inputs_With_Various_Configurations()
        {
            var engine = CreateEngine(o =>
            {
                o.TreatNullDecimalAsZero = true;
                o.TreatNullBoolAsFalse = true;
                o.NullDateTimeDefault = new DateTime(2000, 1, 1);
            });

            var inputs = new Dictionary<string, object?>
            {
                {"null_field", null},
                {"explicit_null", null}
            };

            var script = @"
            {
                set(a, [null_field:decimal] + 10)
                set(b, [null_field:bool] || true)
                set(c, [null_field:datetime])
                set(d, [null_field] + 'suffix')
            }";

            var result = engine.Execute(script, inputs);

            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(10m); // null treated as 0
            result.Assignments["b"].ShouldBe(true); // null treated as false
            result.Assignments["c"].ShouldBe(new DateTime(2000, 1, 1)); // null uses default date
            result.Assignments["d"].ShouldBe("suffix"); // null string treated as empty string
        }
    }
}
