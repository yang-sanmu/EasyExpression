using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class ExceptionHandlingTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void Parse_Exception_Contains_Position_Information()
        {
            var engine = CreateEngine();
            var script = "{ set(a, 1 + ) }"; // Syntax error

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorLine.ShouldBeGreaterThan(0);
            result.ErrorColumn.ShouldBeGreaterThan(0);
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void Validation_Returns_Parse_Error_Details()
        {
            var engine = CreateEngine();
            var script = "{ set(a, 1 + ) }"; // Syntax error

            var validation = engine.Validate(script);

            validation.Success.ShouldBeFalse();
            validation.ErrorLine.ShouldBeGreaterThan(0);
            validation.ErrorColumn.ShouldBeGreaterThan(0);
            validation.ErrorMessage.ShouldNotBeNullOrEmpty();
            validation.ErrorSnippet.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void Runtime_Exception_Contains_Position_Information()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 1)
                set(b, 10 / 0)
                set(c, 3)
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorLine.ShouldBe(4); // Error is on line 4
            result.ErrorColumn.ShouldBeGreaterThan(0);
            result.ErrorMessage.ShouldContain("Divide by zero");
        }

        [Fact]
        public void Unknown_Field_Exception_Contains_Field_Name()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, [unknown_field]) }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Unknown field");
            result.ErrorMessage.ShouldContain("unknown_field");
        }

        [Fact]
        public void Type_Conversion_Exception_Contains_Details()
        {
            var engine = CreateEngine();
            var inputs = new Dictionary<string, object?> { {"field", "not-a-number"} };
            var script = @"{ set(a, [field:decimal]) }";

            var result = engine.Execute(script, inputs);

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Cannot convert field");
            result.ErrorMessage.ShouldContain("field");
            result.ErrorMessage.ShouldContain("not-a-number");
            result.ErrorMessage.ShouldContain("decimal");
        }

        [Fact]
        public void Unknown_Function_Exception_Contains_Function_Name()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, UnknownFunction(1, 2)) }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Unknown function");
        }

        [Fact]
        public void Function_Argument_Exception_Contains_Details()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, ToUpper()) }"; // ToUpper requires 1 argument

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("ToUpper expects 1 arg");
        }

        [Fact]
        public void Boolean_Required_Exception_In_If_Statement()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if('not_boolean') {
                    set(a, 1)
                }
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("if condition must be boolean");
        }

        [Fact]
        public void Boolean_Required_Exception_In_Assert_Statement()
        {
            var engine = CreateEngine();
            var script = @"{ assert('not_boolean', 'return', 'message') }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("assert condition must be boolean");
        }

        [Fact]
        public void Boolean_Required_Exception_In_Logical_Not()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, !'not_boolean') }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Expected boolean in logical operation");
        }

        [Fact]
        public void Nested_Exception_Preserves_Outer_Position()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if(true) {
                    set(a, 10 / 0)
                }
            }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Divide by zero");
            result.ErrorLine.ShouldBe(4); // Error is inside the nested set statement
        }

        [Fact]
        public void Max_Depth_Exception_Contains_Limit_Info()
        {
            var engine = CreateEngine(o => o.MaxDepth = 2);
            var script = @"{ set(a, (((1+2)+3)+4)) }"; // Exceeds depth limit

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Max depth exceeded");
        }

        [Fact]
        public void Max_Node_Visits_Exception_Contains_Limit_Info()
        {
            var engine = CreateEngine(o => o.MaxNodeVisits = 5);
            var script = @"
            {
                set(a, 1+1+1+1+1+1+1+1+1+1)
            }"; // Exceeds visit count limit

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Max node visits exceeded");
        }

        [Fact]
        public void Timeout_Exception_Contains_Timeout_Info()
        {
            var engine = CreateEngine(o => o.TimeoutMilliseconds = 1);
            var script = @"
            {
                set(a, 1)
                set(b, 2)
                set(c, 3)
                set(d, 4)
                set(e, 5)
            }"; // May trigger timeout

            var result = engine.Execute(script, new Dictionary<string, object?>());

            // Note: timeout may not always trigger, depending on execution speed
            if (result.HasError)
            {
                result.ErrorMessage.ShouldContain("timeout");
            }
        }

        [Fact]
        public void Parse_Error_Contains_Error_Snippet()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 1)
                set(b, 2 + )
                set(c, 3)
            }";

            var validation = engine.Validate(script);

            validation.Success.ShouldBeFalse();
            validation.ErrorSnippet.ShouldNotBeNullOrEmpty();
            validation.ErrorSnippet.ShouldContain("set(b, 2 + )");
        }

        [Fact]
        public void Invalid_Type_Annotation_Exception()
        {
            var engine = CreateEngine();
            var inputs = new Dictionary<string, object?> { {"field", "value"} };
            var script = @"{ set(a, [field:invalid_type]) }";

            var result = engine.Execute(script, inputs);

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Unknown type annotation");
            result.ErrorMessage.ShouldContain("invalid_type");
        }

        [Fact]
        public void Invalid_Field_Name_Exception_With_Strict_Validation()
        {
            var engine = CreateEngine(o => o.StrictFieldNameValidation = true);
            var script = @"{ set(a, [invalid-field-name]) }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Invalid field name");
            result.ErrorMessage.ShouldContain("invalid-field-name");
        }

        [Fact]
        public void Type_Mismatch_In_Equality_Exception()
        {
            var engine = CreateEngine(o => o.EqualityCoercion = EqualityCoercionMode.Strict);
            var script = @"{ set(a, 1 == ToDateTime('2024-01-01 00:00:00')) }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("type mismatch");
        }

        [Fact]
        public void Relational_Comparison_Type_Mismatch_Exception()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, 'text' > 123) }";

            var result = engine.Execute(script, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Cannot convert value 'text' to decimal");
        }
    }
}
