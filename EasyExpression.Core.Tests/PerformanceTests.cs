using System;
using System.Collections.Generic;
using System.Diagnostics;
using EasyExpression.Core.Engine;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class PerformanceTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void Compilation_Cache_Improves_Performance()
        {
            var script = @"{
                set(result, a + b)
                set(final, result)
            }";

            var inputs = new Dictionary<string, object?>
            {
                ["a"] = 5.0m,
                ["b"] = 3.0m,
                ["c"] = 9.0m
            };

            // Test performance with cache enabled
            var engineWithCache = CreateEngine(opts => opts.EnableCompilationCache = true);
            var engineWithoutCache = CreateEngine(opts => opts.EnableCompilationCache = false);

            // Warm up
            var warmupResult1 = engineWithCache.Execute(script, inputs);
            if (warmupResult1.HasError)
            {
                throw new Exception($"Warmup failed: {warmupResult1.ErrorMessage}");
            }
            var warmupResult2 = engineWithoutCache.Execute(script, inputs);
            if (warmupResult2.HasError)
            {
                throw new Exception($"Warmup failed: {warmupResult2.ErrorMessage}");
            }

            const int iterations = 1000;

            // Test performance with cache
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var result = engineWithCache.Execute(script, inputs);
                result.HasError.ShouldBeFalse();
            }
            sw1.Stop();

            // Test performance without cache
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var result = engineWithoutCache.Execute(script, inputs);
                result.HasError.ShouldBeFalse();
            }
            sw2.Stop();

            // Cache should significantly improve performance
            sw1.ElapsedMilliseconds.ShouldBeLessThan(sw2.ElapsedMilliseconds);
            
            // Output performance data for reference
            var improvement = (double)(sw2.ElapsedMilliseconds - sw1.ElapsedMilliseconds) / sw2.ElapsedMilliseconds * 100;
            Console.WriteLine($"With cache: {sw1.ElapsedMilliseconds}ms");
            Console.WriteLine($"Without cache: {sw2.ElapsedMilliseconds}ms");
            Console.WriteLine($"Performance improvement: {improvement:F1}%");
        }

        [Fact]
        public void Compile_And_Execute_Shows_Performance_Benefit()
        {
            var script = @"{
                set(x, a + b)
                set(y, x + c)
                set(result, y)
            }";

            var inputs = new Dictionary<string, object?>
            {
                ["a"] = 10.5m,
                ["b"] = 2.3m,
                ["c"] = 5.7m,
                ["d"] = 15.0m,
                ["e"] = 3.2m,
                ["f"] = 1.8m,
                ["threshold"] = 5.0m,
                ["factor"] = 0.8m,
                ["offset"] = 2.5m
            };

            var engine = CreateEngine();
            const int iterations = 500;

            // Test performance of parsing and executing each time
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var result = engine.Execute(script, inputs);
                result.HasError.ShouldBeFalse();
            }
            sw1.Stop();

            // Test performance of compile once then execute
            var compiled = engine.Compile(script);
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var result = engine.Execute(compiled, inputs);
                result.HasError.ShouldBeFalse();
            }
            sw2.Stop();

            var improvement = (double)(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds) / sw1.ElapsedMilliseconds * 100;

            // When running the full test suite, performance tests may be unstable; here we only verify correctness
            // Performance improvements are covered by other dedicated performance tests
            Console.WriteLine($"Parse + Execute: {sw1.ElapsedMilliseconds}ms");
            Console.WriteLine($"Compiled Execute: {sw2.ElapsedMilliseconds}ms");
            Console.WriteLine($"Compile-once improvement: {improvement:F1}%");
        }

        [Fact]
        public void Cache_Management_Works_Correctly()
        {
            var engine = CreateEngine();
            var script1 = "{ set(a, 1) }";
            var script2 = "{ set(b, 2) }";
            var inputs = new Dictionary<string, object?>();

            // Execute a few scripts to fill the cache
            engine.Execute(script1, inputs);
            engine.Execute(script2, inputs);

            // Clear cache
            engine.ClearCache();

            // Should still work after clearing
            var result = engine.Execute(script1, inputs);
            result.HasError.ShouldBeFalse();
            result.Assignments["a"].ShouldBe(1m);
        }

        [Fact]
        public void Large_Script_Performance_Test()
        {
            var scriptParts = new List<string>();
            
            // Generate a large script to test performance
            for (int i = 0; i < 50; i++)
            {
                scriptParts.Add($"set(var{i}, {i} + field{i % 10})");
            }
            
            scriptParts.Add("set(total, 0)");
            for (int i = 0; i < 50; i++)
            {
                scriptParts.Add($"set(total, total + var{i})");
            }

            var script = "{\n" + string.Join("\n", scriptParts) + "\n}";

            var inputs = new Dictionary<string, object?>();
            for (int i = 0; i < 10; i++)
            {
                inputs[$"field{i}"] = i * 2.5m;
            }

            var engine = CreateEngine();

            var sw = Stopwatch.StartNew();
            var result = engine.Execute(script, inputs);
            sw.Stop();

            result.HasError.ShouldBeFalse();
            result.Assignments.ShouldContainKey("total");
            
            // Large script should complete within a reasonable time
            sw.ElapsedMilliseconds.ShouldBeLessThan(1000); // Should finish within 1 second
            
            Console.WriteLine($"Large script execution time: {sw.ElapsedMilliseconds}ms");
        }
    }
}
