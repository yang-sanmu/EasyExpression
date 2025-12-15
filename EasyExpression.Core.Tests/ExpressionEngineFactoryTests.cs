using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using EasyExpression.Core.Engine.Functions;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class ExpressionEngineFactoryTests
    {
        [Fact]
        public void DefaultFactory_Creates_Engine_With_Default_Options()
        {
            var factory = new DefaultExpressionEngineFactory();
            var engine = factory.Create();

            engine.ShouldNotBeNull();
            engine.Services.ShouldNotBeNull();
            engine.Services.Options.ShouldNotBeNull();
            
            // 检查默认选项值
            engine.Services.Options.MaxDepth.ShouldBe(64);
            engine.Services.Options.MaxNodes.ShouldBe(2000);
            engine.Services.Options.TimeoutMilliseconds.ShouldBe(2000);
            engine.Services.Options.StringComparison.ShouldBe(StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Factory_Can_Configure_Options()
        {
            var factory = new DefaultExpressionEngineFactory();
            var engine = factory.Create(options =>
            {
                options.MaxDepth = 32;
                options.MaxNodes = 1000;
                options.TimeoutMilliseconds = 5000;
                options.StringComparison = StringComparison.Ordinal;
            });

            engine.Services.Options.MaxDepth.ShouldBe(32);
            engine.Services.Options.MaxNodes.ShouldBe(1000);
            engine.Services.Options.TimeoutMilliseconds.ShouldBe(5000);
            engine.Services.Options.StringComparison.ShouldBe(StringComparison.Ordinal);
        }

        [Fact]
        public void Factory_Can_Add_Contributors()
        {
            var factory = new DefaultExpressionEngineFactory();
            var contributor = new TestContributor();
            var engine = factory.Create(contributors: new[] { contributor });

            // 验证自定义函数已注册
            var script = @"{ set(a, TestFunction()) }";
            var res = engine.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe("test");
        }

        [Fact]
        public void Factory_Can_Configure_Options_And_Add_Contributors()
        {
            var factory = new DefaultExpressionEngineFactory();
            var contributor = new TestContributor();
            var engine = factory.Create(
                options => options.MaxDepth = 16,
                contributors: new[] { contributor });

            engine.Services.Options.MaxDepth.ShouldBe(16);
            
            var script = @"{ set(a, TestFunction()) }";
            var res = engine.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe("test");
        }

        [Fact]
        public void Factory_With_Null_ConfigureOptions_Should_Work()
        {
            var factory = new DefaultExpressionEngineFactory();
            var engine = factory.Create(configureOptions: null);

            engine.ShouldNotBeNull();
            engine.Services.Options.ShouldNotBeNull();
        }

        [Fact]
        public void Factory_With_Null_Contributors_Should_Work()
        {
            var factory = new DefaultExpressionEngineFactory();
            var engine = factory.Create(contributors: null);

            engine.ShouldNotBeNull();
            engine.Services.Options.ShouldNotBeNull();
        }

        [Fact]
        public void Factory_With_Multiple_Contributors()
        {
            var factory = new DefaultExpressionEngineFactory();
            var contributors = new IEngineContributor[]
            {
                new TestContributor(),
                new AnotherTestContributor()
            };
            
            var engine = factory.Create(contributors: contributors);

            var script = @"
            {
                set(a, TestFunction())
                set(b, AnotherFunction())
            }";
            var res = engine.Execute(script, new Dictionary<string, object?>());
            res.HasError.ShouldBeFalse();
            res.Assignments["a"].ShouldBe("test");
            res.Assignments["b"].ShouldBe("another");
        }

        private sealed class TestContributor : IEngineContributor
        {
            public void Configure(EngineServices services)
            {
                services.Functions.Register(new TestFunction());
            }
        }

        private sealed class AnotherTestContributor : IEngineContributor
        {
            public void Configure(EngineServices services)
            {
                services.Functions.Register(new AnotherFunction());
            }
        }

        private sealed class TestFunction : IFunction
        {
            public string Name => "TestFunction";
            public object? Invoke(object?[] args, InvocationContext ctx)
            {
                return "test";
            }
        }

        private sealed class AnotherFunction : IFunction
        {
            public string Name => "AnotherFunction";
            public object? Invoke(object?[] args, InvocationContext ctx)
            {
                return "another";
            }
        }
    }
}
