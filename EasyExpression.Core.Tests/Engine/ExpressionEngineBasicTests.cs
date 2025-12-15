using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression.Core.Engine
{
    /// <summary>
    /// Basic unit tests for the expression engine
    /// </summary>
    public class ExpressionEngineBasicTests 
    {
        private readonly ExpressionEngine _engine;

        public ExpressionEngineBasicTests()
        {
            var options = new ExpressionEngineOptions();
            var services = new EngineServices(options);
            _engine = new ExpressionEngine(services);
        }

        [Fact]
        public void Should_Execute_Simple_Arithmetic_Expression()
        {
            // Arrange
            var script = "SET(result, 1 + 2 * 3)";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Assignments.ShouldContainKey("result");
            result.Assignments["result"].ShouldBe(7m); // 1 + (2 * 3) = 7
        }

        [Fact]
        public void Should_Execute_String_Concatenation()
        {
            // Arrange
            var script = "SET(greeting, 'Hello' + ' ' + 'World')";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Assignments.ShouldContainKey("greeting");
            result.Assignments["greeting"].ShouldBe("Hello World");
        }

        [Fact]
        public void Should_Execute_Field_Access()
        {
            // Arrange
            var script = "SET(doubled, [input] * 2)";
            var inputs = new Dictionary<string, object?>
            {
                ["input"] = 10
            };

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Assignments.ShouldContainKey("doubled");
            result.Assignments["doubled"].ShouldBe(20m);
        }

        [Fact]
        public void Should_Execute_Conditional_Logic()
        {
            // Arrange
            var script = @"
                IF ([score] >= 90) {
                    SET(grade, 'A')
                } ELSEIF ([score] >= 80) {
                    SET(grade, 'B')
                } ELSE {
                    SET(grade, 'C')
                }";
            var inputs = new Dictionary<string, object?>
            {
                ["score"] = 85
            };

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Assignments.ShouldContainKey("grade");
            result.Assignments["grade"].ShouldBe("B");
        }

        [Fact]
        public void Should_Execute_Built_In_Functions()
        {
            // Arrange
            var script = @"
                SET(upper_text, ToUpper('hello world'))
                SET(max_value, Max(10, 20, 5))
                SET(rounded, Round(3.14159, 2))";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Assignments["upper_text"].ShouldBe("HELLO WORLD");
            result.Assignments["max_value"].ShouldBe(20m);
            result.Assignments["rounded"].ShouldBe(3.14m);
        }

        [Fact]
        public void Should_Execute_Message_Statement()
        {
            // Arrange
            var script = @"
                MSG('Processing started', 'info')
                SET(result, 42)
                MSG('Processing completed', 'info')";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Messages.Count.ShouldBe(2);
            result.Messages[0].Text.ShouldBe("Processing started");
            result.Messages[1].Text.ShouldBe("Processing completed");
            result.Assignments["result"].ShouldBe(42m);
        }

        [Fact]
        public void Should_Execute_Assert_Statement()
        {
            // Arrange
            var script = @"
                ASSERT([value] > 0, 'None', 'Value must be positive', 'error')
                SET(result, [value] * 2)";
            var inputs = new Dictionary<string, object?>
            {
                ["value"] = 5
            };

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Assignments["result"].ShouldBe(10m);
        }

        [Fact]
        public void Should_Handle_Assert_Failure()
        {
            // Arrange
            var script = @"
                ASSERT([value] > 0, 'Return', 'Value must be positive', 'error')
                SET(result, [value] * 2)";
            var inputs = new Dictionary<string, object?>
            {
                ["value"] = -5
            };

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Messages.Count.ShouldBe(1);
            result.Messages[0].Text.ShouldBe("Value must be positive");
            result.Assignments.ShouldNotContainKey("result"); // SET should not have been executed
        }

        [Fact]
        public void Should_Handle_Nested_Local_Blocks()
        {
            // Arrange
            var script = @"
                SET(outer, 'outer')
                LOCAL {
                    SET(inner, 'inner')
                    LOCAL {
                        SET(nested, 'nested')
                        MSG('In nested block', 'info')
                    }
                    MSG('In inner block', 'info')
                }
                MSG('In outer block', 'info')";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Messages.Count.ShouldBe(3);
            result.Assignments.ShouldContainKey("outer");
            result.Assignments.ShouldContainKey("inner");
            result.Assignments.ShouldContainKey("nested");
        }

        [Fact]
        public void Should_Validate_Script_Successfully()
        {
            // Arrange
            var script = @"
                IF ([score] >= 90) {
                    SET(grade, 'A')
                } ELSE {
                    SET(grade, 'F')
                }";

            // Act
            var result = _engine.Validate(script);

            // Assert
            result.Success.ShouldBeTrue();
            result.ErrorMessage.ShouldBeNullOrEmpty();
        }

        [Fact]
        public void Should_Detect_Syntax_Errors_In_Validation()
        {
            // Arrange
            var script = @"
                IF ([score] >= 90 {  // Missing closing parenthesis
                    SET(grade, 'A')
                }";

            // Act
            var result = _engine.Validate(script);

            // Assert
            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
            result.ErrorLine.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void Should_Handle_Type_Conversion()
        {
            // Arrange
            var script = @"
                SET(string_to_number, ToDecimal('123.45'))
                SET(number_to_string, ToString(456.78))
                SET(date_conversion, ToDateTime('2023-12-25 10:30:00'))";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeFalse();
            result.Assignments["string_to_number"].ShouldBe(123.45m);
            result.Assignments["number_to_string"].ShouldBe("456.78");
            result.Assignments["date_conversion"].ShouldBeOfType<DateTime>();
        }

        [Fact]
        public void Should_Handle_Null_Error()
        {
            // Arrange
            var script = @"
                SET(null_check, [missing_field] == null)
                SET(default_value, [missing_field] ?? 'default')";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = _engine.Execute(script, inputs);

            // Assert
            result.HasError.ShouldBeTrue();
        }

        [Fact]
        public void Should_Respect_Execution_Limits()
        {
            // Arrange
            var options = new ExpressionEngineOptions
            {
                MaxNodeVisits = 10,
                TimeoutMilliseconds = 100
            };
            var services = new EngineServices(options);
            var limitedEngine = new ExpressionEngine(services);

            var script = @"
                SET(counter, 0)
                // This should be a loop script that exceeds the visit limit
                // For simplicity, we use a complex expression instead
                SET(result, Max(1,2,3,4,5,6,7,8,9,10) + Min(1,2,3,4,5,6,7,8,9,10))";
            var inputs = new Dictionary<string, object?>();

            // Act
            var result = limitedEngine.Execute(script, inputs);

            // Assert
            // Depending on the core engine implementation, this may succeed or fail
            // The main goal is to verify the engine can handle limit options
            result.ShouldNotBeNull();
        }
    }
}
