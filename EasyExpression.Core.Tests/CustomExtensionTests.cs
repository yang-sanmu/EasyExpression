using System;
using System.Collections.Generic;
using EasyExpression.Core.Engine;
using EasyExpression.Core.Engine.Functions;
using EasyExpression.Core.Engine.Conversion;
using Shouldly;
using Xunit;

namespace EasyExpression
{
	public class CustomExtensionTests
	{
		private sealed class Concat3Function : IFunction
		{
			public string Name => "Concat3";
			public object? Invoke(object?[] args, InvocationContext ctx)
			{
				if (args.Length != 3) throw new ArgumentException("Concat3 expects 3 args");
				return (args[0]?.ToString() ?? string.Empty) + (args[1]?.ToString() ?? string.Empty) + (args[2]?.ToString() ?? string.Empty);
			}
		}

		private sealed class YesNoToBoolConverter : ITypeConverter
		{
			public Type InputType => typeof(string);
			public Type OutputType => typeof(bool);
			public bool TryConvert(object? value, out object? result)
			{
				result = null;
				if (value is string s)
				{
					if (string.Equals(s, "Y", StringComparison.OrdinalIgnoreCase)) { result = true; return true; }
					if (string.Equals(s, "N", StringComparison.OrdinalIgnoreCase)) { result = false; return true; }
				}
				return false;
			}
		}

		private sealed class OverrideToUpperFunction : IFunction
		{
			public string Name => "ToUpper";
			public object? Invoke(object?[] args, InvocationContext ctx)
			{
				if (args.Length != 1) throw new ArgumentException("ToUpper expects 1 arg");
				return $"OVERRIDDEN:{args[0]?.ToString()}";
			}
		}

		private sealed class OverrideStringToDecimalConverter : ITypeConverter
		{
			public Type InputType => typeof(string);
			public Type OutputType => typeof(decimal);
			public bool TryConvert(object? value, out object? result)
			{
				result = 42m; // 覆盖内置 string->decimal 转换
				return true;
			}
		}

		[Fact]
		public void Can_Register_And_Invoke_Custom_Function()
		{
			var factory = new DefaultExpressionEngineFactory();
			var e = factory.Create(contributors: new[] { new RegisterConcatContributor() });

			var script = @"
			{
				set(a, Concat3('x', 1, 'y'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe("x1y");
		}

		private sealed class RegisterConcatContributor : IEngineContributor
		{
			public void Configure(EngineServices services)
			{
				services.Functions.Register(new Concat3Function());
			}
		}

		[Fact]
		public void Can_Register_Custom_Type_Converter_For_Field_Type_Annotation()
		{
			var options = new ExpressionEngineOptions();
			var services = new EngineServices(options);
			services.Converters.Register(new YesNoToBoolConverter());
			var e = new ExpressionEngine(services);

			var inputs = new Dictionary<string, object?>
			{
				{"B1", "Y"},
				{"B2", "N"}
			};

			var script = @"
			{
				set(x, [B1:bool])
				set(y, [B2:bool])
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeFalse();
			res.Assignments["x"].ShouldBe(true);
			res.Assignments["y"].ShouldBe(false);
		}

		[Fact]
		public void Can_Override_BuiltIn_Function_By_Name()
		{
			var options = new ExpressionEngineOptions();
			var services = new EngineServices(options);
			services.Functions.Register(new OverrideToUpperFunction()); // 覆盖内置 ToUpper
			var e = new ExpressionEngine(services);

			var script = @"
			{
				set(a, ToUpper('a'))
			}";
			var res = e.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe("OVERRIDDEN:a");
		}

		[Fact]
		public void Can_Override_BuiltIn_StringToDecimal_Converter()
		{
			var options = new ExpressionEngineOptions();
			var services = new EngineServices(options);
			services.Converters.Register(new OverrideStringToDecimalConverter()); // 覆盖内置 string->decimal
			var e = new ExpressionEngine(services);

			var inputs = new Dictionary<string, object?> { {"A", "123"} };
			var script = @"
			{
				set(x, [A:decimal])
			}";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeFalse();
			res.Assignments["x"].ShouldBe(42m);
		}

		[Fact]
		public void Can_Use_Contributor_To_Register_Function_And_Converter()
		{
			var factory = new DefaultExpressionEngineFactory();
			var e = factory.Create(contributors: new[] { new DemoContributor() });

			var inputs = new Dictionary<string, object?> { {"B1", null}, {"S1", "ab"} };
			var script = @"
			{
				set(a, AddHours(ToDateTime('2024-01-01 00:00:00'), 1))
				set(b, Concat3('a','b','c'))
			}
			";
			var res = e.Execute(script, inputs);
			res.HasError.ShouldBeFalse();
			res.Assignments["b"].ShouldBe("abc");
		}

		private sealed class DemoContributor : IEngineContributor
		{
			public void Configure(EngineServices services)
			{
				services.Functions.Register(new Concat3Function());
			}
		}
	}
}



