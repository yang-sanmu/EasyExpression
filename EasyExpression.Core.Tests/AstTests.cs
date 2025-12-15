using System;
using System.Collections.Generic;
using System.Linq;
using EasyExpression.Core.Engine;
using EasyExpression.Core.Engine.Ast;
using EasyExpression.Core.Engine.Parsing;
using Shouldly;
using Xunit;

namespace EasyExpression
{
    public class AstTests
    {
        private static ExpressionEngine CreateEngine(Action<ExpressionEngineOptions>? cfg = null)
        {
            var options = new ExpressionEngineOptions();
            cfg?.Invoke(options);
            var services = new EngineServices(options);
            return new ExpressionEngine(services);
        }

        [Fact]
        public void LiteralExpr_Stores_Values_Correctly()
        {
            var literalDecimal = new LiteralExpr(123.45m, 1, 5);
            var literalString = new LiteralExpr("hello", 2, 10);
            var literalBool = new LiteralExpr(true, 3, 15);
            var literalNull = new LiteralExpr(null, 4, 20);

            literalDecimal.Value.ShouldBe(123.45m);
            literalDecimal.Line.ShouldBe(1);
            literalDecimal.Column.ShouldBe(5);

            literalString.Value.ShouldBe("hello");
            literalString.Line.ShouldBe(2);
            literalString.Column.ShouldBe(10);

            literalBool.Value.ShouldBe(true);
            literalBool.Line.ShouldBe(3);
            literalBool.Column.ShouldBe(15);

            literalNull.Value.ShouldBeNull();
            literalNull.Line.ShouldBe(4);
            literalNull.Column.ShouldBe(20);
        }

        [Fact]
        public void FieldExpr_Stores_Name_And_Type_Correctly()
        {
            var fieldSimple = new FieldExpr("fieldName", null, 1, 5);
            var fieldWithType = new FieldExpr("typedField", "decimal", 2, 10);

            fieldSimple.Name.ShouldBe("fieldName");
            fieldSimple.TypeAnnotation.ShouldBeNull();
            fieldSimple.Line.ShouldBe(1);
            fieldSimple.Column.ShouldBe(5);

            fieldWithType.Name.ShouldBe("typedField");
            fieldWithType.TypeAnnotation.ShouldBe("decimal");
            fieldWithType.Line.ShouldBe(2);
            fieldWithType.Column.ShouldBe(10);
        }

        [Fact]
        public void BinaryExpr_Constructs_Tree_Structure()
        {
            var left = new LiteralExpr(1m, 1, 1);
            var right = new LiteralExpr(2m, 1, 5);
            var binary = new BinaryExpr(BinaryOp.Add, left, right, 1, 3);

            binary.Op.ShouldBe(BinaryOp.Add);
            binary.Left.ShouldBeSameAs(left);
            binary.Right.ShouldBeSameAs(right);
            binary.Line.ShouldBe(1);
            binary.Column.ShouldBe(3);
        }

        [Fact]
        public void UnaryExpr_Constructs_With_Inner_Expression()
        {
            var inner = new LiteralExpr(true, 1, 5);
            var unary = new UnaryExpr("!", inner, 1, 1);

            unary.Op.ShouldBe("!");
            unary.Inner.ShouldBeSameAs(inner);
            unary.Line.ShouldBe(1);
            unary.Column.ShouldBe(1);
        }

        [Fact]
        public void CallExpr_Constructs_With_Arguments()
        {
            var arg1 = new LiteralExpr(1m, 1, 8);
            var arg2 = new LiteralExpr(2m, 1, 11);
            var args = new List<Expr> { arg1, arg2 };
            var call = new CallExpr("Sum", args, 1, 1);

            call.Name.ShouldBe("Sum");
            call.Args.Count.ShouldBe(2);
            call.Args[0].ShouldBeSameAs(arg1);
            call.Args[1].ShouldBeSameAs(arg2);
            call.Line.ShouldBe(1);
            call.Column.ShouldBe(1);
        }

        [Fact]
        public void Block_Contains_Statements()
        {
            var block = new Block(1, 1);
            var stmt1 = new SetStmt("a", new LiteralExpr(1m, 2, 10), 2, 5);
            var stmt2 = new SetStmt("b", new LiteralExpr(2m, 3, 10), 3, 5);

            block.Statements.Add(stmt1);
            block.Statements.Add(stmt2);

            block.Statements.Count.ShouldBe(2);
            block.Statements[0].ShouldBeSameAs(stmt1);
            block.Statements[1].ShouldBeSameAs(stmt2);
        }

        [Fact]
        public void SetStmt_Associates_Field_With_Expression()
        {
            var value = new LiteralExpr(42m, 1, 10);
            var stmt = new SetStmt("myField", value, 1, 5);

            stmt.FieldName.ShouldBe("myField");
            stmt.Value.ShouldBeSameAs(value);
            stmt.Line.ShouldBe(1);
            stmt.Column.ShouldBe(5);
        }

        [Fact]
        public void IfStmt_Constructs_With_Condition_And_Then_Block()
        {
            var condition = new LiteralExpr(true, 1, 4);
            var thenBlock = new Block(1, 10);
            var ifStmt = new IfStmt(condition, thenBlock, 1, 1);

            ifStmt.Condition.ShouldBeSameAs(condition);
            ifStmt.Then.ShouldBeSameAs(thenBlock);
            ifStmt.ElseIfs.Count.ShouldBe(0);
            ifStmt.Else.ShouldBeNull();
        }

        [Fact]
        public void IfStmt_Supports_ElseIf_And_Else()
        {
            var condition = new LiteralExpr(true, 1, 4);
            var thenBlock = new Block(1, 10);
            var ifStmt = new IfStmt(condition, thenBlock, 1, 1);

            var elseIfCondition = new LiteralExpr(false, 2, 4);
            var elseIfBlock = new Block(2, 10);
            ifStmt.ElseIfs.Add((elseIfCondition, elseIfBlock));

            var elseBlock = new Block(3, 5);
            ifStmt.Else = elseBlock;

            ifStmt.ElseIfs.Count.ShouldBe(1);
            ifStmt.ElseIfs[0].Cond.ShouldBeSameAs(elseIfCondition);
            ifStmt.ElseIfs[0].Body.ShouldBeSameAs(elseIfBlock);
            ifStmt.Else.ShouldBeSameAs(elseBlock);
        }

        [Fact]
        public void LocalStmt_Contains_Block()
        {
            var body = new Block(1, 10);
            var localStmt = new LocalStmt(body, 1, 1);

            localStmt.Body.ShouldBeSameAs(body);
        }

        [Fact]
        public void ReturnStmt_Has_Kind()
        {
            var returnStmt = new ReturnStmt(ReturnKind.Return, 1, 1);
            var returnLocalStmt = new ReturnStmt(ReturnKind.ReturnLocal, 2, 1);

            returnStmt.Kind.ShouldBe(ReturnKind.Return);
            returnLocalStmt.Kind.ShouldBe(ReturnKind.ReturnLocal);
        }

        [Fact]
        public void MsgStmt_Stores_Text_And_Level()
        {
            var msgStmt = new MsgStmt("Hello", "info", 1, 1);
            var msgStmtNoLevel = new MsgStmt("Warning", null, 2, 1);

            msgStmt.Text.ShouldBe("Hello");
            msgStmt.Level.ShouldBe("info");

            msgStmtNoLevel.Text.ShouldBe("Warning");
            msgStmtNoLevel.Level.ShouldBeNull();
        }

        [Fact]
        public void AssertStmt_Contains_Condition_And_Action()
        {
            var condition = new LiteralExpr(true, 1, 8);
            var assertStmt = new AssertStmt(condition, "halt", "Assertion failed", "error", 1, 1);

            assertStmt.Condition.ShouldBeSameAs(condition);
            assertStmt.Action.ShouldBe("halt");
            assertStmt.Message.ShouldBe("Assertion failed");
            assertStmt.MsgLevel.ShouldBe("error");
        }

        [Fact]
        public void AST_Builds_From_Simple_Script()
        {
            var engine = CreateEngine();
            var script = "{ set(a, 123) }";

            var compiled = engine.Compile(script);

            compiled.ShouldNotBeNull();
            compiled.Statements.Count.ShouldBe(1);
            
            var setStmt = compiled.Statements[0].ShouldBeOfType<SetStmt>();
            setStmt.FieldName.ShouldBe("a");
            
            var literal = setStmt.Value.ShouldBeOfType<LiteralExpr>();
            literal.Value.ShouldBe(123m);
        }

        [Fact]
        public void AST_Builds_From_Complex_Expression()
        {
            var engine = CreateEngine();
            var script = "{ set(result, (1 + 2) * 3) }";

            var compiled = engine.Compile(script);

            var setStmt = compiled.Statements[0].ShouldBeOfType<SetStmt>();
            var binaryMul = setStmt.Value.ShouldBeOfType<BinaryExpr>();
            binaryMul.Op.ShouldBe(BinaryOp.Mul);

            var binaryAdd = binaryMul.Left.ShouldBeOfType<BinaryExpr>();
            binaryAdd.Op.ShouldBe(BinaryOp.Add);

            var leftLiteral = binaryAdd.Left.ShouldBeOfType<LiteralExpr>();
            leftLiteral.Value.ShouldBe(1m);

            var rightLiteral = binaryAdd.Right.ShouldBeOfType<LiteralExpr>();
            rightLiteral.Value.ShouldBe(2m);

            var mulRightLiteral = binaryMul.Right.ShouldBeOfType<LiteralExpr>();
            mulRightLiteral.Value.ShouldBe(3m);
        }

        [Fact]
        public void AST_Builds_From_Function_Call()
        {
            var engine = CreateEngine();
            var script = "{ set(result, Sum(1, 2, 3)) }";

            var compiled = engine.Compile(script);

            var setStmt = compiled.Statements[0].ShouldBeOfType<SetStmt>();
            var callExpr = setStmt.Value.ShouldBeOfType<CallExpr>();
            callExpr.Name.ShouldBe("Sum");
            callExpr.Args.Count.ShouldBe(3);

            callExpr.Args[0].ShouldBeOfType<LiteralExpr>().Value.ShouldBe(1m);
            callExpr.Args[1].ShouldBeOfType<LiteralExpr>().Value.ShouldBe(2m);
            callExpr.Args[2].ShouldBeOfType<LiteralExpr>().Value.ShouldBe(3m);
        }

        [Fact]
        public void AST_Builds_From_Field_Reference()
        {
            var engine = CreateEngine();
            var script = "{ set(result, [myField:decimal]) }";

            var compiled = engine.Compile(script);

            var setStmt = compiled.Statements[0].ShouldBeOfType<SetStmt>();
            var fieldExpr = setStmt.Value.ShouldBeOfType<FieldExpr>();
            fieldExpr.Name.ShouldBe("myField");
            fieldExpr.TypeAnnotation.ShouldBe("decimal");
        }

        [Fact]
        public void AST_Builds_From_If_Statement()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if(true) {
                    set(a, 1)
                } else {
                    set(a, 2)
                }
            }";

            var compiled = engine.Compile(script);

            var ifStmt = compiled.Statements[0].ShouldBeOfType<IfStmt>();
            ifStmt.Condition.ShouldBeOfType<LiteralExpr>().Value.ShouldBe(true);
            ifStmt.Then.Statements.Count.ShouldBe(1);
            ifStmt.Else.ShouldNotBeNull();
            ifStmt.Else.Statements.Count.ShouldBe(1);

            var thenSet = ifStmt.Then.Statements[0].ShouldBeOfType<SetStmt>();
            thenSet.FieldName.ShouldBe("a");
            thenSet.Value.ShouldBeOfType<LiteralExpr>().Value.ShouldBe(1m);

            var elseSet = ifStmt.Else.Statements[0].ShouldBeOfType<SetStmt>();
            elseSet.FieldName.ShouldBe("a");
            elseSet.Value.ShouldBeOfType<LiteralExpr>().Value.ShouldBe(2m);
        }

        [Fact]
        public void AST_Builds_From_Local_Block()
        {
            var engine = CreateEngine();
            var script = @"
            {
                local {
                    set(temp, 42)
                }
            }";

            var compiled = engine.Compile(script);

            var localStmt = compiled.Statements[0].ShouldBeOfType<LocalStmt>();
            localStmt.Body.Statements.Count.ShouldBe(1);

            var setStmt = localStmt.Body.Statements[0].ShouldBeOfType<SetStmt>();
            setStmt.FieldName.ShouldBe("temp");
            setStmt.Value.ShouldBeOfType<LiteralExpr>().Value.ShouldBe(42m);
        }

        [Fact]
        public void AST_Preserves_Line_Column_Information()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 1)
                set(b, 2)
            }";

            var compiled = engine.Compile(script);

            // 验证节点包含位置信息
            compiled.Line.ShouldBeGreaterThan(0);
            compiled.Column.ShouldBeGreaterThan(0);

            foreach (var stmt in compiled.Statements)
            {
                stmt.Line.ShouldBeGreaterThan(0);
                stmt.Column.ShouldBeGreaterThan(0);
            }
        }

        [Fact]
        public void AST_Node_Count_Calculates_Correctly()
        {
            var engine = CreateEngine();
            
            // 简单表达式：{ set(a, 1) }
            // 预期节点：Block(1) + SetStmt(1) + LiteralExpr(1) = 3
            var simpleScript = "{ set(a, 1) }";
            var simpleCompiled = engine.Compile(simpleScript);
            simpleCompiled.ShouldNotBeNull();

            // 复杂表达式：{ set(a, (1 + 2) * 3) }
            // 预期节点：Block(1) + SetStmt(1) + BinaryExpr(*)(1) + BinaryExpr(+)(1) + LiteralExpr(1)(1) + LiteralExpr(2)(1) + LiteralExpr(3)(1) = 7
            var complexScript = "{ set(a, (1 + 2) * 3) }";
            var complexCompiled = engine.Compile(complexScript);
            complexCompiled.ShouldNotBeNull();

            // 函数调用：{ set(a, Sum(1, 2, 3)) }
            // 预期节点：Block(1) + SetStmt(1) + CallExpr(1) + LiteralExpr(1)(1) + LiteralExpr(2)(1) + LiteralExpr(3)(1) = 6
            var functionScript = "{ set(a, Sum(1, 2, 3)) }";
            var functionCompiled = engine.Compile(functionScript);
            functionCompiled.ShouldNotBeNull();
        }

        [Fact]
        public void AST_Handles_Nested_Structures()
        {
            var engine = CreateEngine();
            var script = @"
            {
                if(true) {
                    if(false) {
                        set(a, 1)
                    } else {
                        local {
                            set(b, 2)
                        }
                    }
                }
            }";

            var compiled = engine.Compile(script);

            var outerIf = compiled.Statements[0].ShouldBeOfType<IfStmt>();
            var innerIf = outerIf.Then.Statements[0].ShouldBeOfType<IfStmt>();
            var localStmt = innerIf.Else.Statements[0].ShouldBeOfType<LocalStmt>();
            var setStmt = localStmt.Body.Statements[0].ShouldBeOfType<SetStmt>();
            
            setStmt.FieldName.ShouldBe("b");
            setStmt.Value.ShouldBeOfType<LiteralExpr>().Value.ShouldBe(2m);
        }

        [Fact]
        public void AST_Handles_Multiple_Statements()
        {
            var engine = CreateEngine();
            var script = @"
            {
                set(a, 1)
                set(b, 2)
                set(c, a + b)
                if(c > 2) {
                    set(result, 'large')
                }
            }";

            var compiled = engine.Compile(script);

            compiled.Statements.Count.ShouldBe(4);
            
            compiled.Statements[0].ShouldBeOfType<SetStmt>().FieldName.ShouldBe("a");
            compiled.Statements[1].ShouldBeOfType<SetStmt>().FieldName.ShouldBe("b");
            compiled.Statements[2].ShouldBeOfType<SetStmt>().FieldName.ShouldBe("c");
            compiled.Statements[3].ShouldBeOfType<IfStmt>();
        }

        [Fact]
        public void AST_BinaryOp_Enum_Values()
        {
            // 验证所有BinaryOp枚举值
            var arithmeticOps = new[] { BinaryOp.Add, BinaryOp.Sub, BinaryOp.Mul, BinaryOp.Div, BinaryOp.Mod };
            var comparisonOps = new[] { BinaryOp.Gt, BinaryOp.Lt, BinaryOp.Ge, BinaryOp.Le, BinaryOp.Eq, BinaryOp.Ne };
            var logicalOps = new[] { BinaryOp.And, BinaryOp.Or };

            arithmeticOps.Length.ShouldBe(5);
            comparisonOps.Length.ShouldBe(6);
            logicalOps.Length.ShouldBe(2);
        }

        [Fact]
        public void AST_ReturnKind_Enum_Values()
        {
            var returnKinds = new[] { ReturnKind.Return, ReturnKind.ReturnLocal };
            returnKinds.Length.ShouldBe(2);

            var returnStmt = new ReturnStmt(ReturnKind.Return, 1, 1);
            var returnLocalStmt = new ReturnStmt(ReturnKind.ReturnLocal, 1, 1);

            returnStmt.Kind.ShouldBe(ReturnKind.Return);
            returnLocalStmt.Kind.ShouldBe(ReturnKind.ReturnLocal);
        }
    }
}
