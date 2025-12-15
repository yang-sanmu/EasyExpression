using System.Collections.Generic;
using EasyExpression.Core.Engine;
using Shouldly;
using EasyExpression.Core.Engine.Runtime;
using Xunit;

namespace EasyExpression
{
	public class ControlFlowTests
	{
		private static ExpressionEngine CreateEngine()
		{
			var services = new EngineServices(new ExpressionEngineOptions());
			return new ExpressionEngine(services);
		}

        [Fact]
        public void If_Formula_And_Return()
        {
            var eng = CreateEngine();
            var script = @"
			{
				if(1+1==2){
					set(a, 1)
				}
			}";
            var res = eng.Execute(script, new Dictionary<string, object?>());
            res.Assignments["a"].ShouldBe(1m);
        }

        [Fact]
        public void If_Not_Boolean_Exception()
        {
            var eng = CreateEngine();
            var script = @"
			{
				if(1+1){
					set(a, 1)
				}
			}";
            var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeTrue();
        }

        [Fact]
		public void If_ElseIf_Else_And_Return()
		{
			var eng = CreateEngine();
			var script = @"
			{
				if(true){
					set(a, 1)
					return
				} elseif(true){
					set(a, 2)
				} else {
					set(a, 3)
				}
				set(b, 9)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(1m);
			// No more statements should execute after return
			res.Assignments.ContainsKey("b").ShouldBeFalse();
		}

		[Fact]
		public void Validate_Only_Should_Return_Syntax_Diagnostics()
		{
			var services = new EngineServices(new ExpressionEngineOptions());
			var eng = new ExpressionEngine(services);
			var vr = eng.Validate("{ set(a, 1 + ) }");
			vr.Success.ShouldBeFalse();
			vr.ErrorMessage.ShouldNotBeNull();
		}

		[Fact]
		public void Parser_Should_Ignore_Comments()
		{
			var eng = new ExpressionEngine(new EngineServices(new ExpressionEngineOptions()));
			var script = @"
			{
				// set(a, 0)
				set(a, 1) /* comment */
			}
			";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.HasError.ShouldBeFalse();
			res.Assignments["a"].ShouldBe(1m);
		}

		[Fact]
		public void Local_And_ReturnLocal()
		{
			var eng = CreateEngine();
			var script = @"
			{
				local{
					set(a,1)
					return_local
					set(a,2)
				}
				set(b,9)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(1m);
			res.Assignments["b"].ShouldBe(9m);
		}

		[Fact]
		public void Assert_With_Message_And_Action()
		{
			var eng = CreateEngine();
			var script = @"
			{
				assert(false, 'return', 'X','warn')
				set(a,1)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Messages.Count.ShouldBe(1);
			res.Messages[0].Text.ShouldBe("X");
			res.Messages[0].Level.ShouldBe(MessageLevel.Warn);
			res.Assignments.ContainsKey("a").ShouldBeFalse();
		}

		[Fact]
		public void Msg_Statement_Produces_Messages()
		{
			var eng = CreateEngine();
			var script = @"
			{
				msg('Hello')
				msg('Warn', 'warn')
				set(a,1)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Messages.Count.ShouldBe(2);
			res.Messages[0].Text.ShouldBe("Hello");
			res.Messages[0].Level.ShouldBe(MessageLevel.Info);
			res.Messages[1].Text.ShouldBe("Warn");
			res.Messages[1].Level.ShouldBe(MessageLevel.Warn);
			res.Assignments["a"].ShouldBe(1m);
		}

		[Fact]
		public void If_With_Nested_If()
		{
			var eng = CreateEngine();
			var script = @"
			{
				if(true){
					if(false){
						set(a, 1)
					} else {
						set(a, 2)
					}
				} else {
					set(a, 3)
				}
				set(b, 9)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(2m);
			res.Assignments["b"].ShouldBe(9m);
		}

		[Fact]
		public void Local_With_Nested_If_And_ReturnLocal()
		{
			var eng = CreateEngine();
			var script = @"
			{
				local{
					if(true){
						set(a, 1)
						return_local
					}
					set(a, 2)
				}
				set(b, 9)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(1m);
			res.Assignments["b"].ShouldBe(9m);
		}

		[Fact]
		public void If_With_Nested_Local_And_ReturnLocal()
		{
			var eng = CreateEngine();
			var script = @"
			{
				if(true){
					local{
						set(a, 1)
						return_local
					}
					set(a, 2)
				} else {
					set(a, 3)
				}
				set(b, 9)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(2m);
			res.Assignments["b"].ShouldBe(9m);
		}

		[Fact]
		public void Local_Nested_Local_With_ReturnLocal_In_Inner()
		{
			var eng = CreateEngine();
			var script = @"
			{
				local{
					set(p, 1)
					local{
						set(a, 10)
						return_local
						set(a, 11)
					}
					set(q, 2)
				}
				set(r, 3)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(10m);
			res.Assignments["p"].ShouldBe(1m);
			res.Assignments["q"].ShouldBe(2m);
			res.Assignments["r"].ShouldBe(3m);
		}

		[Fact]
		public void ReturnLocal_Outside_Local_Equals_Return()
		{
			var eng = CreateEngine();
			var script = @"
			{
				set(a, 1)
				return_local
				set(b, 2)
			}";
			var res = eng.Execute(script, new Dictionary<string, object?>());
			res.Assignments["a"].ShouldBe(1m);
			res.Assignments.ContainsKey("b").ShouldBeFalse();
		}
	}
}


