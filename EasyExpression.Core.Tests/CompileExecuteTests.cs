using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using EasyExpression.Core.Engine.Ast;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class CompileExecuteTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void Compile_Simple_Script_Returns_Block()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, 1+2) }";
            
            var block = engine.Compile(script);
            
            block.ShouldNotBeNull();
            block.ShouldBeOfType<Block>();
            block.Statements.Count.ShouldBe(1);
        }

        [Fact]
        public void Compile_Execute_Produces_Same_Result_As_Direct_Execute()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 10 + 5)
                set(b, 'hello' + ' world')
                if(true) {
                    set(c, 42)
                }
            }";
            var inputs = new Dictionary<string, object?>();

            // 直接执行
            var directResult = engine.Execute(script, inputs);
            
            // 编译后执行
            var compiled = engine.Compile(script);
            var compiledResult = engine.Execute(compiled, inputs);

            // 结果应该一致
            directResult.HasError.ShouldBe(compiledResult.HasError);
            directResult.Assignments["a"].ShouldBe(compiledResult.Assignments["a"]);
            directResult.Assignments["b"].ShouldBe(compiledResult.Assignments["b"]);
            directResult.Assignments["c"].ShouldBe(compiledResult.Assignments["c"]);
        }

        [Fact]
        public void Compile_Once_Execute_Multiple_Times_With_Different_Inputs()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(result, [x:decimal] * 2 + [y:decimal])
            }";
            
            var compiled = engine.Compile(script);

            // 第一次执行
            var inputs1 = new Dictionary<string, object?> { {"x", 5}, {"y", 3} };
            var result1 = engine.Execute(compiled, inputs1);
            result1.HasError.ShouldBeFalse();
            result1.Assignments["result"].ShouldBe(13m); // 5*2+3=13

            // 第二次执行，不同输入
            var inputs2 = new Dictionary<string, object?> { {"x", 10}, {"y", 7} };
            var result2 = engine.Execute(compiled, inputs2);
            result2.HasError.ShouldBeFalse();
            result2.Assignments["result"].ShouldBe(27m); // 10*2+7=27

            // 第三次执行，不同输入
            var inputs3 = new Dictionary<string, object?> { {"x", 0}, {"y", 100} };
            var result3 = engine.Execute(compiled, inputs3);
            result3.HasError.ShouldBeFalse();
            result3.Assignments["result"].ShouldBe(100m); // 0*2+100=100
        }

        [Fact]
        public void Compile_With_Invalid_Script_Should_Throw()
        {
            var engine = CreateEngine();
            var invalidScript = @"{ set(a, 1 + ) }"; // 语法错误

            Should.Throw<Exception>(() => engine.Compile(invalidScript));
        }

        [Fact]
        public void Compile_Exceeds_MaxNodes_Should_Throw()
        {
            var engine = CreateEngine(o => o.MaxNodes = 2);
            var script = @"{ set(a, 1) set(b, 2) set(c, 3) }"; // 超过节点限制

            Should.Throw<Exception>(() => engine.Compile(script));
        }

        [Fact]
        public void Execute_Compiled_With_Runtime_Error_Returns_Error_Result()
        {
            var engine = CreateEngine();
            var script = @"{ set(a, [unknown_field]) }";
            
            var compiled = engine.Compile(script); // 编译成功
            var inputs = new Dictionary<string, object?>();
            var result = engine.Execute(compiled, inputs); // 运行时错误

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Unknown field");
        }

        [Fact]
        public void Execute_Compiled_Block_With_Functions()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(upper, ToUpper('hello'))
                set(len, Len('world'))
                set(sum, Sum(1, 2, 3, 4, 5))
            }";
            
            var compiled = engine.Compile(script);
            var result = engine.Execute(compiled, new Dictionary<string, object?>());

            result.HasError.ShouldBeFalse();
            result.Assignments["upper"].ShouldBe("HELLO");
            result.Assignments["len"].ShouldBe(5m);
            result.Assignments["sum"].ShouldBe(15m);
        }

        [Fact]
        public void Execute_Compiled_Block_With_Control_Flow()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if([condition:bool]) {
                    set(result, 'true_branch')
                } else {
                    set(result, 'false_branch')
                }
            }";
            
            var compiled = engine.Compile(script);

            // 测试 true 分支
            var inputsTrue = new Dictionary<string, object?> { {"condition", true} };
            var resultTrue = engine.Execute(compiled, inputsTrue);
            resultTrue.HasError.ShouldBeFalse();
            resultTrue.Assignments["result"].ShouldBe("true_branch");

            // 测试 false 分支
            var inputsFalse = new Dictionary<string, object?> { {"condition", false} };
            var resultFalse = engine.Execute(compiled, inputsFalse);
            resultFalse.HasError.ShouldBeFalse();
            resultFalse.Assignments["result"].ShouldBe("false_branch");
        }

        [Fact]
        public void Compiled_Block_Reuse_Is_Thread_Safe()
        {
            var engine = CreateEngine();
            var script = @"{ set(result, [input:decimal] * 2) }";
            var compiled = engine.Compile(script);

            // 模拟并发执行（虽然这个测试不是真正的并发测试，但验证了基本的重用安全性）
            var results = new List<decimal>();
            for (int i = 0; i < 10; i++)
            {
                var inputs = new Dictionary<string, object?> { {"input", i} };
                var result = engine.Execute(compiled, inputs);
                result.HasError.ShouldBeFalse();
                results.Add((decimal)result.Assignments["result"]!);
            }

            // 验证结果
            for (int i = 0; i < 10; i++)
            {
                results[i].ShouldBe(i * 2);
            }
        }

        [Fact]
        public void Execute_Compiled_Exception_Contains_Position_Info()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 1)
                set(b, 8/0)
                set(c, 3)
            }";
            
            var compiled = engine.Compile(script);
            var result = engine.Execute(compiled, new Dictionary<string, object?>());

            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldContain("Divide by zero");
            result.ErrorLine.ShouldBeGreaterThan(0);
            result.ErrorColumn.ShouldBeGreaterThan(0);
        }
    }
}
