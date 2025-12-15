using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EasyExpression.Core.Engine.Ast;
using EasyExpression.Core.Engine.Functions;
using EasyExpression.Core.Engine.Runtime;

namespace EasyExpression.Core.Engine
{
    public sealed class Evaluator
    {
        private readonly EngineServices _services;
        private int _visitCount;
        private Stopwatch? _stopwatch;

        public Evaluator(EngineServices services)
        {
            _services = services;
        }

        public ExecutionResult Execute(Block block, Dictionary<string, object?> input)
        {
            var ctx = new ExecutionContext(input, _services.Options);
            var result = new ExecutionResult();
            _visitCount = 0;
            // Unified timer: use ExecutionResult.Stopwatch as the single timing source
            _stopwatch = result.Stopwatch;
            try
            {
                ExecuteBlock(block, ctx, result, allowReturnLocal: false);
            }
            catch (ExpressionException ex)
            {
                result.HasError = true;
                result.ErrorMessage = ex.Message;
                result.ErrorLine = ex.Line;
                result.ErrorColumn = ex.Column;
                result.ErrorCode = ex.ErrorCode;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                result.Stopwatch.Stop();
                result.Elapsed = result.Stopwatch.Elapsed;
            }
            return result;
        }

        private enum FlowSignal
        { None, Return, ReturnLocal }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FlowSignal ExecuteBlock(Block block, ExecutionContext ctx, ExecutionResult result, bool allowReturnLocal)
        {
            foreach (var stmt in block.Statements)
            {
                var signal = ExecuteStatement(stmt, ctx, result, allowReturnLocal);
                if (signal != FlowSignal.None) return signal;
            }
            return FlowSignal.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FlowSignal ExecuteStatement(Stmt stmt, ExecutionContext ctx, ExecutionResult result, bool allowReturnLocal)
        {
            CheckLimits(stmt.Line, stmt.Column, depth: 0);
            result.EndLine = stmt.Line;
            result.EndColumn = stmt.Column;
            switch (stmt)
            {
                case SetStmt s:
                    {
                        var value = EvaluateExpr(s.Value, ctx);
                        object? finalVal = value;
                        if (!string.IsNullOrEmpty(s.TypeAnnotation))
                        {
                            var targetType = ResolveType(s.TypeAnnotation, value);
                            if (!_services.Converters.TryConvert(value, targetType, out var converted))
                                throw new ExpressionRuntimeException($"Cannot convert value '{value}' to {s.TypeAnnotation}", ExpressionErrorCode.ConversionError, s.Line, s.Column);
                            finalVal = converted;
                        }
                        var outVal = ApplyOutputRounding(finalVal);
                        ctx.MutableFields[s.FieldName] = outVal;
                        result.Assignments[s.FieldName] = outVal;
                        return FlowSignal.None;
                    }
                case MsgStmt m:
                    {
                        var level = ParseLevel(m.Level);
                        result.Messages.Add(new Runtime.ExecutionMessage(level, m.Text, m.Line, m.Column));
                        return FlowSignal.None;
                    }
                case ReturnStmt r:
                    {
                        if (r.Kind == ReturnKind.ReturnLocal)
                            return allowReturnLocal ? FlowSignal.ReturnLocal : FlowSignal.Return;
                        return FlowSignal.Return;
                    }
                case AssertStmt a:
                    {
                        var cond = EvaluateExpr(a.Condition, ctx);
                        if (cond is not bool b) throw new ExpressionRuntimeException("assert condition must be boolean", ExpressionErrorCode.TypeMismatch, a.Line, a.Column);
                        if (!b)
                        {
                            var level = ParseLevel(a.MsgLevel);
                            result.Messages.Add(new Runtime.ExecutionMessage(level, a.Message, a.Line, a.Column));
                            if (!string.Equals(a.Action, "none", StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.Equals(a.Action, "return_local", StringComparison.OrdinalIgnoreCase))
                                    return allowReturnLocal ? FlowSignal.ReturnLocal : FlowSignal.Return;
                                if (string.Equals(a.Action, "return", StringComparison.OrdinalIgnoreCase))
                                    return FlowSignal.Return;
                                throw new ExpressionRuntimeException($"Unknown assert action: {a.Action}", ExpressionErrorCode.UnknownOperator, a.Line, a.Column);
                            }
                        }
                        return FlowSignal.None;
                    }
                case IfStmt ifs:
                    {
                        var cond = EvaluateExpr(ifs.Condition, ctx);
                        if (cond is not bool b) throw new ExpressionRuntimeException("if condition must be boolean", ExpressionErrorCode.TypeMismatch, ifs.Line, ifs.Column);
                        if (b)
                            return ExecuteBlock(ifs.Then, ctx, result, allowReturnLocal);
                        foreach (var (c, body) in ifs.ElseIfs)
                        {
                            var cb = EvaluateExpr(c, ctx);
                            if (cb is not bool b2) throw new ExpressionRuntimeException("elseif condition must be boolean", ExpressionErrorCode.TypeMismatch, ifs.Line, ifs.Column);
                            if (b2) return ExecuteBlock(body, ctx, result, allowReturnLocal);
                        }
                        if (ifs.Else != null) return ExecuteBlock(ifs.Else, ctx, result, allowReturnLocal);
                        return FlowSignal.None;
                    }
                case LocalStmt l:
                    {
                        var sig = ExecuteBlock(l.Body, ctx, result, allowReturnLocal: true);
                        if (sig == FlowSignal.ReturnLocal) return FlowSignal.None; // Swallow ReturnLocal inside local
                        return sig;
                    }
            }
            throw new ExpressionRuntimeException($"Unknown statement: {stmt.GetType().Name}", ExpressionErrorCode.UnknownOperator, stmt.Line, stmt.Column);
        }

        private MessageLevel ParseLevel(string? level)
        {
            if (string.IsNullOrEmpty(level)) return MessageLevel.Info;
            if (string.Equals(level, "info", StringComparison.OrdinalIgnoreCase)) return MessageLevel.Info;
            if (string.Equals(level, "warn", StringComparison.OrdinalIgnoreCase)) return MessageLevel.Warn;
            if (string.Equals(level, "error", StringComparison.OrdinalIgnoreCase)) return MessageLevel.Error;
            return MessageLevel.Info;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object? EvaluateExpr(Expr expr, ExecutionContext ctx, int depth = 0)
        {
            CheckLimits(expr.Line, expr.Column, depth);
            switch (expr)
            {
                case LiteralExpr l:
                    return l.Value;

                case FieldExpr f:
                    {
                        // Field-name validation: prefer custom validator; otherwise follow Strict rules
                        var validator = _services.Options.FieldNameValidator;
                        if (validator != null)
                        {
                            if (!validator(f.Name)) throw new ExpressionParseException($"Invalid field name: {f.Name}", ExpressionErrorCode.InvalidFieldName, f.Line, f.Column);
                        }
                        else if (_services.Options.StrictFieldNameValidation && !IsValidFieldName(f.Name))
                        {
                            throw new ExpressionParseException($"Invalid field name: {f.Name}", ExpressionErrorCode.InvalidFieldName, f.Line, f.Column);
                        }
                        if (!ctx.MutableFields.TryGetValue(f.Name, out var value))
                            throw new ExpressionRuntimeException($"Unknown field: {f.Name}", ExpressionErrorCode.UnknownField, f.Line, f.Column);
                        //if (f.TypeAnnotation == null || string.Equals(f.TypeAnnotation, "string", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    // Default field type is string: prefer converters to keep behavior consistent (e.g., Options.DateTimeFormat)
                        //    if (_services.Converters.TryConvert(value, typeof(string), out var s))
                        //        return s;
                        //    return value?.ToString() ?? string.Empty;
                        //}
                        var target = ResolveType(f.TypeAnnotation, value);
                        // Special behavior for null based on the target type
                        if (value == null)
                        {
                            if (target == typeof(decimal) && _services.Options.TreatNullDecimalAsZero)
                                return 0m;
                            if (target == typeof(bool) && _services.Options.TreatNullBoolAsFalse)
                                return false;
                            if (target == typeof(DateTime) && _services.Options.NullDateTimeDefault.HasValue)
                                return _services.Options.NullDateTimeDefault.Value;
                        }
                        if (!_services.Converters.TryConvert(value, target, out var conv))
                            throw new ExpressionRuntimeException($"Cannot convert field {f.Name} value '{value}' to {f.TypeAnnotation}", ExpressionErrorCode.ConversionError, f.Line, f.Column);
                        return conv;
                    }
                case UnaryExpr u:
                    {
                        var v = EvaluateExpr(u.Inner, ctx, depth + 1);
                        if (u.Op == "-")
                        {
                            var d = ToDecimal(v);
                            return -d;
                        }
                        if (u.Op == "!")
                        {
                            var b = ToBool(v);
                            return !b;
                        }
                        throw new ExpressionRuntimeException($"Unsupported unary op: {u.Op}", ExpressionErrorCode.UnknownOperator, u.Line, u.Column);
                    }
                case BinaryExpr b:
                    return EvalBinary(b, ctx, depth + 1);

                case CallExpr c:
                    return EvalCall(c, ctx, depth + 1);
            }
            throw new ExpressionRuntimeException($"Unknown expression: {expr.GetType().Name}", ExpressionErrorCode.UnknownOperator, expr.Line, expr.Column);
        }

        private object? EvalCall(CallExpr c, ExecutionContext ctx, int depth)
        {
            if (c.Name == "__now__")
            {
                return _services.Options.NowUseLocalTime ? DateTime.Now : DateTime.UtcNow;
            }
            IFunction func;
            try
            {
                func = _services.Functions.Resolve(c.Name);
            }
            catch (ArgumentException ex)
            {
                // Function not registered
                throw new ExpressionRuntimeException(ex.Message, ExpressionErrorCode.UnknownFunction, c.Line, c.Column, ex);
            }

            var args = new object?[c.Args.Count];
            for (int i = 0; i < args.Length; i++) args[i] = EvaluateExpr(c.Args[i], ctx, depth + 1);
            try
            {
                return func.Invoke(args, new InvocationContext(_services.Options, _services.Converters, ctx.InputFields));
            }
            catch (ExpressionException)
            {
                // Propagate semantic exceptions thrown by the engine
                throw;
            }
            catch (Exception ex)
            {
                // Function resolved, but invocation failed (arg count/type/business validation, etc.)
                throw new ExpressionRuntimeException(ex.Message, ExpressionErrorCode.InvalidFunctionArguments, c.Line, c.Column, ex);
            }
        }

        private object EvalBinary(BinaryExpr b, ExecutionContext ctx, int depth)
        {
            // Short-circuit logic: evaluate left first, evaluate right only if needed
            if (b.Op == BinaryOp.And)
            {
                var leftVal = EvaluateExpr(b.Left, ctx, depth + 1);
                var leftBool = ToBool(leftVal);
                if (!leftBool) return false; // Short-circuit
                var rightVal = EvaluateExpr(b.Right, ctx, depth + 1);
                return leftBool && ToBool(rightVal);
            }
            if (b.Op == BinaryOp.Or)
            {
                var leftVal = EvaluateExpr(b.Left, ctx, depth + 1);
                var leftBool = ToBool(leftVal);
                if (leftBool) return true; // Short-circuit
                var rightVal = EvaluateExpr(b.Right, ctx, depth + 1);
                return ToBool(rightVal);
            }

            var l = EvaluateExpr(b.Left, ctx, depth + 1);
            var r = EvaluateExpr(b.Right, ctx, depth + 1);
            switch (b.Op)
            {
                case BinaryOp.Add:
                    {
                        if (l is string || r is string)
                        {
                            if (_services.Options.StringConcat == StringConcatMode.PreferNumericIfParsable)
                            {
                                if (TryToDecimal(l, out var dl) && TryToDecimal(r, out var dr))
                                {
                                    return dl + dr;
                                }
                            }
                            string ls, rs;
                            if (!_services.Converters.TryConvert(l, typeof(string), out var lstrObj))
                                ls = l?.ToString() ?? string.Empty;
                            else
                                ls = lstrObj?.ToString() ?? string.Empty;

                            if (!_services.Converters.TryConvert(r, typeof(string), out var rstrObj))
                                rs = r?.ToString() ?? string.Empty;
                            else
                                rs = rstrObj?.ToString() ?? string.Empty;

                            return string.Concat(ls, rs);
                        }
                        return ToDecimal(l) + ToDecimal(r);
                    }
                case BinaryOp.Sub: return ToDecimal(l) - ToDecimal(r);
                case BinaryOp.Mul: return ToDecimal(l) * ToDecimal(r);
                case BinaryOp.Div:
                    {
                        var denom = ToDecimal(r);
                        if (denom == 0m) throw new ExpressionRuntimeException("Divide by zero", ExpressionErrorCode.DivideByZero, b.Line, b.Column);
                        return ToDecimal(l) / denom;
                    }
                case BinaryOp.Mod:
                    {
                        var denom = ToDecimal(r);
                        if (denom == 0m) throw new ExpressionRuntimeException("Modulo by zero", ExpressionErrorCode.ModuloByZero, b.Line, b.Column);
                        return ToDecimal(l) % denom;
                    }
                case BinaryOp.Gt:
                case BinaryOp.Lt:
                case BinaryOp.Ge:
                case BinaryOp.Le:
                    return CompareRelational(b.Op, l, r, b.Line, b.Column);

                case BinaryOp.Eq:
                case BinaryOp.Ne:
                    return CompareEquality(b.Op, l, r, b.Line, b.Column);
            }
            throw new ExpressionRuntimeException($"Unsupported binary op: {b.Op}", ExpressionErrorCode.UnknownOperator, b.Line, b.Column);
        }

        private bool CompareEquality(BinaryOp op, object? l, object? r, int line, int column)
        {
            // If either side is a string, decide comparison behavior based on the selected strategy
            if (l is string || r is string)
            {
                // MixedNumericOnly strategy:
                // - Only when one side is a number and the other side is a string, try numeric comparison.
                // - If both sides are strings, always compare as strings.
                // - For other mismatches, fall back to string comparison.
                if (_services.Options.EqualityCoercion == EqualityCoercionMode.MixedNumericOnly)
                {
                    if (l is string && r is string)
                    {
                        var ls0 = l?.ToString() ?? string.Empty;
                        var rs0 = r?.ToString() ?? string.Empty;
                        var eq0 = string.Equals(ls0, rs0, _services.Options.StringComparison);
                        return op == BinaryOp.Eq ? eq0 : !eq0;
                    }
                    if (l is string && IsStrictNumberType(r))
                    {
                        if (TryToDecimal(l, out var ld0))
                        {
                            var rd0 = ToDecimal(r);
                            var eqn0 = ld0 == rd0; return op == BinaryOp.Eq ? eqn0 : !eqn0;
                        }
                    }
                    if (r is string && IsStrictNumberType(l))
                    {
                        if (TryToDecimal(r, out var rd1))
                        {
                            var ld1 = ToDecimal(l);
                            var eqn1 = ld1 == rd1; return op == BinaryOp.Eq ? eqn1 : !eqn1;
                        }
                    }
                    var ls1 = l?.ToString() ?? string.Empty;
                    var rs1 = r?.ToString() ?? string.Empty;
                    var eq1 = string.Equals(ls1, rs1, _services.Options.StringComparison);
                    return op == BinaryOp.Eq ? eq1 : !eq1;
                }

                if (_services.Options.EqualityCoercion == EqualityCoercionMode.NumberFriendly || _services.Options.EqualityCoercion == EqualityCoercionMode.Permissive)
                {
                    if (TryToDecimal(l, out var dl) && TryToDecimal(r, out var dr))
                    {
                        var eqn = dl == dr; return op == BinaryOp.Eq ? eqn : !eqn;
                    }
                }
                var ls = l?.ToString() ?? string.Empty;
                var rs = r?.ToString() ?? string.Empty;
                var eq = string.Equals(ls, rs, _services.Options.StringComparison);
                return op == BinaryOp.Eq ? eq : !eq;
            }
            if (l is bool lb && r is bool rb)
            {
                var eq = lb == rb; return op == BinaryOp.Eq ? eq : !eq;
            }
            if (IsNumberLike(l) && IsNumberLike(r))
            {
                var eq = ToDecimal(l) == ToDecimal(r); return op == BinaryOp.Eq ? eq : !eq;
            }
            if (l is DateTime ld && r is DateTime rd)
            {
                var eq = ld == rd; return op == BinaryOp.Eq ? eq : !eq;
            }
            // Type mismatch: in Permissive mode, fall back to string comparison; otherwise throw
            if (_services.Options.EqualityCoercion == EqualityCoercionMode.Permissive || _services.Options.EqualityCoercion == EqualityCoercionMode.MixedNumericOnly)
            {
                var ls2 = l?.ToString() ?? string.Empty;
                var rs2 = r?.ToString() ?? string.Empty;
                var eq2 = string.Equals(ls2, rs2, _services.Options.StringComparison);
                return op == BinaryOp.Eq ? eq2 : !eq2;
            }
            throw new ExpressionRuntimeException("== != type mismatch", ExpressionErrorCode.TypeMismatch, line, column);
        }

        private bool CompareRelational(BinaryOp op, object? l, object? r, int line, int column)
        {
            // Categorization: strict runtime numeric types and DateTime types (do not treat strings as numbers/datetimes)
            bool lIsNum = IsStrictNumberType(l);
            bool rIsNum = IsStrictNumberType(r);
            bool lIsDt = l is DateTime;
            bool rIsDt = r is DateTime;

            // 4. One side is datetime and the other is number: error out
            if ((lIsDt && rIsNum) || (rIsDt && lIsNum))
                throw new ExpressionRuntimeException(
                    "> < >= <= cannot compare datetime with number", ExpressionErrorCode.TypeMismatch, line, column);

            // Both are numbers: compare as decimal
            if (lIsNum && rIsNum)
            {
                var ld = ToDecimal(l);
                var rd = ToDecimal(r);
                return op switch
                {
                    BinaryOp.Gt => ld > rd,
                    BinaryOp.Ge => ld >= rd,
                    BinaryOp.Lt => ld < rd,
                    BinaryOp.Le => ld <= rd,
                    _ => throw new InvalidOperationException()
                };
            }

            // Both are datetimes: compare as DateTime
            if (lIsDt && rIsDt)
            {
                var ld2 = (DateTime)l!;
                var rd2 = (DateTime)r!;
                return op switch
                {
                    BinaryOp.Gt => ld2 > rd2,
                    BinaryOp.Ge => ld2 >= rd2,
                    BinaryOp.Lt => ld2 < rd2,
                    BinaryOp.Le => ld2 <= rd2,
                    _ => throw new InvalidOperationException()
                };
            }

            // 3. One side is datetime, and the other is neither number nor datetime: treat as datetime comparison (try convert the other side to DateTime)
            if (lIsDt && !rIsNum && !rIsDt)
            {
                var ld3 = (DateTime)l!;
                if (!_services.Converters.TryConvert(r, typeof(DateTime), out var rConv) || rConv is not DateTime rdt)
                    throw new ExpressionRuntimeException($"Cannot convert value '{r}' to datetime", ExpressionErrorCode.ConversionError, line, column);
                return op switch
                {
                    BinaryOp.Gt => ld3 > rdt,
                    BinaryOp.Ge => ld3 >= rdt,
                    BinaryOp.Lt => ld3 < rdt,
                    BinaryOp.Le => ld3 <= rdt,
                    _ => throw new InvalidOperationException()
                };
            }
            if (rIsDt && !lIsNum && !lIsDt)
            {
                var rd3 = (DateTime)r!;
                if (!_services.Converters.TryConvert(l, typeof(DateTime), out var lConv) || lConv is not DateTime ldt)
                    throw new ExpressionRuntimeException($"Cannot convert value '{l}' to datetime", ExpressionErrorCode.ConversionError, line, column);
                return op switch
                {
                    BinaryOp.Gt => ldt > rd3,
                    BinaryOp.Ge => ldt >= rd3,
                    BinaryOp.Lt => ldt < rd3,
                    BinaryOp.Le => ldt <= rd3,
                    _ => throw new InvalidOperationException()
                };
            }

            // 2. One side is number, and the other is neither number nor datetime: treat as numeric comparison (convert the other side to decimal)
            if (lIsNum && !rIsNum && !rIsDt)
            {
                var ld = ToDecimal(l);
                if (!_services.Converters.TryConvert(r, typeof(decimal), out var rConv) || rConv is not decimal rd)
                    throw new ExpressionRuntimeException($"Cannot convert value '{r}' to decimal", ExpressionErrorCode.ConversionError, line, column);
                return op switch
                {
                    BinaryOp.Gt => ld > rd,
                    BinaryOp.Ge => ld >= rd,
                    BinaryOp.Lt => ld < rd,
                    BinaryOp.Le => ld <= rd,
                    _ => throw new InvalidOperationException()
                };
            }
            if (rIsNum && !lIsNum && !lIsDt)
            {
                var rd = ToDecimal(r);
                if (!_services.Converters.TryConvert(l, typeof(decimal), out var lConv) || lConv is not decimal ld)
                    throw new ExpressionRuntimeException($"Cannot convert value '{l}' to decimal", ExpressionErrorCode.ConversionError, line, column);
                return op switch
                {
                    BinaryOp.Gt => ld > rd,
                    BinaryOp.Ge => ld >= rd,
                    BinaryOp.Lt => ld < rd,
                    BinaryOp.Le => ld <= rd,
                    _ => throw new InvalidOperationException()
                };
            }

            // 1. Neither side is number/datetime: treat as numeric comparison (try convert both sides to decimal)
            if (!lIsNum && !rIsNum && !lIsDt && !rIsDt)
            {
                if (!_services.Converters.TryConvert(l, typeof(decimal), out var lConv) || lConv is not decimal ldec)
                    throw new ExpressionRuntimeException($"Cannot convert value '{l}' to decimal", ExpressionErrorCode.ConversionError, line, column);
                if (!_services.Converters.TryConvert(r, typeof(decimal), out var rConv) || rConv is not decimal rdec)
                    throw new ExpressionRuntimeException($"Cannot convert value '{r}' to decimal", ExpressionErrorCode.ConversionError, line, column);
                return op switch
                {
                    BinaryOp.Gt => ldec > rdec,
                    BinaryOp.Ge => ldec >= rdec,
                    BinaryOp.Lt => ldec < rdec,
                    BinaryOp.Le => ldec <= rdec,
                    _ => throw new InvalidOperationException()
                };
            }

            // Other combinations are not supported
            throw new ExpressionRuntimeException(
                "> < >= <= only for numbers or datetimes", ExpressionErrorCode.TypeMismatch, line, column);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsStrictNumberType(object? v)
        {
            return v is decimal || v is int || v is long || v is double || v is float || v is byte || v is sbyte || v is short || v is ushort || v is uint || v is ulong;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNumberLike(object? v)
        {
            return v is decimal || v is int || v is long || v is double || v is float || (v is string s && decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _));
        }

        private bool TryToDecimal(object? v, out decimal d)
        {
            try
            {
                d = ToDecimal(v);
                return true;
            }
            catch
            {
                d = 0m;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal ToDecimal(object? v)
        {
            if (v == null) throw new ExpressionRuntimeException("Null cannot convert to decimal", ExpressionErrorCode.NullReference);
            if (v is decimal d) return d;
            if (v is int i) return i;
            if (v is long l) return l;
            if (v is double db) return Convert.ToDecimal(db);
            if (v is float f) return Convert.ToDecimal(f);
            if (v is string s)
            {
                if (decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var rd)) return rd;
                throw new ExpressionRuntimeException($"Cannot parse decimal: {s}", ExpressionErrorCode.ConversionError);
            }
            throw new ExpressionRuntimeException($"Unsupported number type: {v.GetType().FullName}", ExpressionErrorCode.ConversionError);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ToBool(object? v)
        {
            if (v is bool b) return b;
            throw new ExpressionRuntimeException("Expected boolean in logical operation", ExpressionErrorCode.TypeMismatch);
        }

        private static Type ResolveType(string? type,object? currentValue)
        {
            if (string.IsNullOrEmpty(type))
            {
                if(currentValue != null) return currentValue.GetType();
                else return typeof(string); // Default to string
            }
            switch (type!.ToLowerInvariant())
            {
                case "string": return typeof(string);
                case "decimal": return typeof(decimal);
                case "bool": return typeof(bool);
                case "datetime": return typeof(DateTime);
                default: throw new ExpressionRuntimeException($"Unknown type annotation: {type}", ExpressionErrorCode.TypeMismatch);
            }
        }

        private static bool IsValidFieldName(string name)
        {
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == ' ')) return false;
            }
            return true;
        }

        private object? ApplyOutputRounding(object? value)
        {
            if (value is decimal d)
            {
                return Math.Round(d, _services.Options.RoundingDigits, _services.Options.MidpointRounding);
            }
            return value;
        }

        private void CheckLimits(int line, int column, int depth)
        {
            // Visit count
            _visitCount++;
            if (_visitCount > _services.Options.MaxNodeVisits)
                throw new ExpressionLimitException("Max node visits exceeded", ExpressionErrorCode.MaxVisitsExceeded, line, column);

            // Depth
            if (depth > _services.Options.MaxDepth)
                throw new ExpressionLimitException("Max depth exceeded", ExpressionErrorCode.MaxDepthExceeded, line, column);

            // Timeout
            if (_services.Options.TimeoutMilliseconds > 0 && _stopwatch != null)
            {
                if (_stopwatch.ElapsedMilliseconds > _services.Options.TimeoutMilliseconds)
                    throw new ExpressionLimitException("Execution timeout", ExpressionErrorCode.ExecutionTimeout, line, column);
            }
        }
    }
}