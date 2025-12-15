using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class FieldExistsTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void FieldExists_Single_Existing_Key_Returns_True()
        {
            var e = CreateEngine();
            var inputs = new Dictionary<string, object?> { { "A", 1 } };
            var script = @"{ set(r, FieldExists('A')) }";
            var res = e.Execute(script, inputs);
            res.HasError.ShouldBeFalse();
            res.Assignments["r"].ShouldBe(true);
        }

        [Fact]
        public void FieldExists_Single_Missing_Key_Returns_False()
        {
            var e = CreateEngine();
            var inputs = new Dictionary<string, object?>();
            var script = @"{ set(r, FieldExists('A')) }";
            var res = e.Execute(script, inputs);
            res.HasError.ShouldBeFalse();
            res.Assignments["r"].ShouldBe(false);
        }

        [Fact]
        public void FieldExists_Multiple_All_Exist_Returns_True()
        {
            var e = CreateEngine();
            var inputs = new Dictionary<string, object?> { { "A", 1 }, { "B", 2 } };
            var script = @"{ set(r, FieldExists('A','B')) }";
            var res = e.Execute(script, inputs);
            res.HasError.ShouldBeFalse();
            res.Assignments["r"].ShouldBe(true);
        }

        [Fact]
        public void FieldExists_Multiple_One_Missing_Returns_False()
        {
            var e = CreateEngine();
            var inputs = new Dictionary<string, object?> { { "A", 1 } };
            var script = @"{ set(r, FieldExists('A','B')) }";
            var res = e.Execute(script, inputs);
            res.HasError.ShouldBeFalse();
            res.Assignments["r"].ShouldBe(false);
        }

        [Fact]
        public void FieldExists_Respects_Default_CaseInsensitive_True()
        {
            var e = CreateEngine(); // CaseInsensitiveFieldNames defaults to true
            var inputs = new Dictionary<string, object?> { { "Age", 18 } };
            var script = @"{ set(a, FieldExists('age')) set(b, FieldExists('AGE')) }";
            var res = e.Execute(script, inputs);
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe(true);
            res.Assignments["b"].ShouldBe(true);
        }

        [Fact]
        public void FieldExists_Respects_CaseSensitivity_When_Disabled()
        {
            var e = CreateEngine(o => o.CaseInsensitiveFieldNames = false);
            var inputs = new Dictionary<string, object?> { { "Age", 18 } };
            var script = @"{ set(a, FieldExists('age')) set(b, FieldExists('Age')) }";
            var res = e.Execute(script, inputs);
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe(false);
            res.Assignments["b"].ShouldBe(true);
        }

        [Fact]
        public void FieldExists_No_Arguments_Should_Error()
        {
            var e = CreateEngine();
            var inputs = new Dictionary<string, object?>();
            var script = @"{ set(r, FieldExists()) }";
            var res = e.Execute(script, inputs);
            res.HasError.ShouldBeTrue();
            res.ErrorMessage.ShouldContain("FieldExists");
            res.ErrorMessage.ShouldContain("expects at least");
        }
    }
}
