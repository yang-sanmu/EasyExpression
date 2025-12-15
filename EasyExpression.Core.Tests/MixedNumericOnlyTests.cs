using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class MixedNumericOnlyTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            options.EqualityCoercion = EqualityCoercionMode.MixedNumericOnly;
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void Double_String_Should_Compare_As_String()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, '2.0' == '2') }";
            var result = engine.Execute(script, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(false);
        }

        [Fact]
        public void Number_And_StringNumber_Should_Compare_Numerically()
        {
            var engine = CreateEngine();
            var script = @"{
                set(a, 2 == '2.0')
                set(b, '2' == 2.0)
                set(c, '12x' == 12)
            }";
            var result = engine.Execute(script, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(true);
            result.Assignments["b"].ShouldBe(true);
            result.Assignments["c"].ShouldBe(false); // 转换失败则退回字符串比较
        }

        [Fact]
        public void Other_Mismatch_Falls_Back_To_String_Comparison()
        {
            var engine = CreateEngine();
            var script = @"{
                set(a, true == 'true')
                set(b, 'abc' == 123)
            }";
            var result = engine.Execute(script, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(true);  // 字符串比较（忽略大小写）
            result.Assignments["b"].ShouldBe(false); // 'abc' vs '123'
        }

        [Fact]
        public void NonString_Types_Keep_Existing_Semantics()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, 2.0 == 2) }";
            var result = engine.Execute(script, new Dictionary<string, object?>());
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(true); // 数字与数字仍按数值比较
        }
    }
}


