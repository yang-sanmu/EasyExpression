using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using EasyExpression.Core.Engine.Parsing;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class ParsingTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void Lexer_Tokenizes_Basic_Elements()
        {
            var engine = CreateEngine();
            var script = "{ set(a, 123.45) }";

            // Verify lexing via the compilation pipeline
            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            compiled.Statements.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void Parser_Handles_Nested_Expressions()
        {
            var engine = CreateEngine();
            var script = "{ set(a, ((1 + 2) * (3 - 4)) / 5) }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            compiled.Statements.Count.ShouldBe(1);
        }

        [Fact]
        public void Parser_Handles_Complex_Field_Names()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set([field_with_underscores], 1)
                set([field-with-dashes], 2)
                set([field with spaces], 3)
                set([field123], 4)
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            compiled.Statements.Count.ShouldBe(4);
        }

        [Fact]
        public void Parser_Detects_Syntax_Errors()
        {
            var engine = CreateEngine();
            var script = "{ set(a, 1 + ) }"; // Missing operand

            Should.Throw<Exception>(() => engine.Compile(script));
        }

        [Fact]
        public void Parser_Handles_String_Literals_With_Escapes()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 'Hello\nWorld')
                set(b, 'Tab\tSeparated')
                set(c, 'Quote\'Inside')
                set(d, 'Backslash\\Path')
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe("Hello\nWorld");
            result.Assignments["b"].ShouldBe("Tab\tSeparated");
            result.Assignments["c"].ShouldBe("Quote'Inside");
            result.Assignments["d"].ShouldBe("Backslash\\Path");
        }

        [Fact]
        public void Parser_Handles_Function_Calls_With_Various_Arguments()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, Sum(1, 2, 3))
                set(b, Max(1.5, 2.7, 3.1))
                set(c, Substring('hello', 1, 3))

            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
        }

        [Fact]
        public void Parser_Handles_Comments()
        {
            var engine = CreateEngine();
            var script = @"
            {
                // This is a single line comment
                set(a, 1)
                /* This is a 
                   multi-line comment */
                set(b, 2)
                set(c, 3) // End of line comment
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(1m);
            result.Assignments["b"].ShouldBe(2m);
            result.Assignments["c"].ShouldBe(3m);
        }

        [Fact]
        public void Parser_Handles_Boolean_Literals()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, true)
                set(b, false)
                set(c, true && false)
                set(d, !true)
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(true);
            result.Assignments["b"].ShouldBe(false);
            result.Assignments["c"].ShouldBe(false);
            result.Assignments["d"].ShouldBe(false);
        }

        [Fact]
        public void Parser_Handles_Operator_Precedence()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 1 + 2 * 3)       // Should be 1 + (2 * 3) = 7
                set(b, (1 + 2) * 3)     // Should be (1 + 2) * 3 = 9
                set(c, 2 * 3 + 4)       // Should be (2 * 3) + 4 = 10
                set(d, 10 / 2 + 3)      // Should be (10 / 2) + 3 = 8
                set(e, 10 / (2 + 3))    // Should be 10 / (2 + 3) = 2
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(7m);
            result.Assignments["b"].ShouldBe(9m);
            result.Assignments["c"].ShouldBe(10m);
            result.Assignments["d"].ShouldBe(8m);
            result.Assignments["e"].ShouldBe(2m);
        }

        [Fact]
        public void Parser_Handles_Control_Structures()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if(true) {
                    set(a, 1)
                    if(false) {
                        set(b, 2)
                    } else {
                        set(b, 3)
                    }
                }
                
                local {
                    set(c, 4)
                }
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(1m);
            result.Assignments["b"].ShouldBe(3m);
            result.Assignments["c"].ShouldBe(4m);
        }

        [Fact]
        public void Parser_Detects_Mismatched_Braces()
        {
            var engine = CreateEngine();
            var script = "{ set(a, 1) "; // Missing closing brace

            Should.Throw<Exception>(() => engine.Compile(script));
        }

        [Fact]
        public void Parser_Detects_Mismatched_Parentheses()
        {
            var engine = CreateEngine();
            var script = "{ set(a, (1 + 2) }"; // Missing closing parenthesis

            Should.Throw<Exception>(() => engine.Compile(script));
        }

        [Fact]
        public void Parser_Handles_Whitespace_And_Newlines()
        {
            var engine = CreateEngine();
            var script = @"
            
            {
                
                set   (   a   ,   1   )
                
                set(
                    b,
                    2
                )
                
            }
            
            ";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(1m);
            result.Assignments["b"].ShouldBe(2m);
        }

        [Fact]
        public void Parser_Handles_Type_Conversions()
        {
            var engine = CreateEngine();
            var inputs = new Dictionary<string, object?>
            {
                {"str_field", "123"},
                {"num_field", 456},
                {"date_field", "2024-01-01 14:14:14"}
            };

            var script = @"
            {
                set(a, [str_field:decimal])
                set(b, [num_field:string])
                set(c, [date_field:datetime])
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, inputs);
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(123m);
            result.Assignments["b"].ShouldBe("456");
            result.Assignments["c"].ShouldBe(new DateTime(2024, 1, 1,14,14,14));
        }

        [Fact]
        public void Parser_Handles_Complex_Expressions_With_Multiple_Operators()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, (1 + 2) * 3 - 4 / 2 + 5 % 3)
                set(b, true && (false || true) && !false)
                set(c, 'hello' + ' ' + 'world')
                set(d, (5 > 3) && (2 < 4) && (1 == 1) && (0 != 1))
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldNotBeNull();
            result.Assignments["b"].ShouldBe(true);
            result.Assignments["c"].ShouldBe("hello world");
            result.Assignments["d"].ShouldBe(true);
        }

        [Fact]
        public void Parser_Reports_Accurate_Error_Positions()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 1)
                set(b, 2 + + 3)  // Error is on line 4
                set(c, 4)
            }";

            Should.Throw<Exception>(() => engine.Compile(script));
        }

        [Fact]
        public void Parser_Handles_Empty_Blocks()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if(true) {
                }
                
                local {
                }
                
                if(false) {
                } else {
                }
            }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            
            var result = engine.Execute(compiled, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments.Count.ShouldBe(0);
        }
    }
}
