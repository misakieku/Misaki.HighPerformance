using Misaki.HighPerformance.HPC.Generator.IR;
using System;
using System.Text;

namespace Misaki.HighPerformance.HPC.Generator.Backend
{
    /// <summary>
    /// Emits C# source code targeting AVX2 (256-bit vectors) with the bundled
    /// AVX2 extensions: FMA3, F16C, and BMI1/2.
    ///
    /// <para>
    /// Vector type: <c>Vector256&lt;T&gt;</c><br/>
    /// Intrinsic classes used: <c>Avx</c>, <c>Avx2</c>, <c>Fma</c>, <c>F16C</c>,
    /// <c>Bmi1</c>, <c>Bmi2</c>, <c>Vector256</c>.
    /// </para>
    ///
    /// <para>
    /// Math functions with no built-in AVX2 intrinsic (Sin, Cos, Asin, etc.) are
    /// delegated to the <c>AVX2Utility</c> class that is generated separately by
    /// <c>AVX2UtilityGenerator</c> via <c>IVectorAPIContext</c>.
    /// </para>
    /// </summary>
    internal sealed class AVX2Backend : IHPCBackend
    {
        // ── IHPCBackend ───────────────────────────────────────────────────────

        public string Name => "AVX2";

        public string[] RequiredUsings => new[]
        {
            "using System.Runtime.CompilerServices;",
            "using System.Runtime.Intrinsics;",
            "using System.Runtime.Intrinsics.X86;",
        };

        public string EmitMethod(HPCMethodIR method)
        {
            var sb = new StringBuilder();
            var indent = "        ";

            // ── Signature ──────────────────────────────────────────────────────
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.Append($"{indent}public static ");
            sb.Append(EmitReturnType(method.ReturnType));
            sb.Append(' ');
            sb.Append($"{method.OriginalName}_{Name}");
            sb.Append('(');
            sb.Append(EmitParameterList(method));
            sb.AppendLine(")");

            // ── Body ───────────────────────────────────────────────────────────
            sb.AppendLine($"{indent}{{");
            foreach (var stmt in method.Body)
                EmitStatement(sb, stmt, indent + "    ");
            sb.AppendLine($"{indent}}}");

            return sb.ToString();
        }

        // ── Statement emission ────────────────────────────────────────────────

        private void EmitStatement(StringBuilder sb, HPCStmt stmt, string indent)
        {
            switch (stmt)
            {
                case HPCVarDecl decl:
                    sb.AppendLine($"{indent}var {decl.Name} = {EmitExpr(decl.Initializer)};");
                    break;

                case HPCAssignment assign:
                    sb.AppendLine($"{indent}{assign.Target} = {EmitExpr(assign.Value)};");
                    break;

                case HPCExprStmt expr:
                    sb.AppendLine($"{indent}{EmitExpr(expr.Expression)};");
                    break;

                case HPCReturn ret:
                    if (ret.Value is null)
                        sb.AppendLine($"{indent}return;");
                    else
                        sb.AppendLine($"{indent}return {EmitExpr(ret.Value)};");
                    break;

                case HPCIf ifStmt:
                    EmitIf(sb, ifStmt, indent);
                    break;

                case HPCForLoop forLoop:
                    EmitForLoop(sb, forLoop, indent);
                    break;
            }
        }

        private void EmitIf(StringBuilder sb, HPCIf node, string indent)
        {
            sb.AppendLine($"{indent}if ({EmitExpr(node.Condition)})");
            sb.AppendLine($"{indent}{{");
            foreach (var s in node.ThenBody)
                EmitStatement(sb, s, indent + "    ");
            sb.AppendLine($"{indent}}}");

            if (node.ElseBody is { Length: > 0 })
            {
                sb.AppendLine($"{indent}else");
                sb.AppendLine($"{indent}{{");
                foreach (var s in node.ElseBody)
                    EmitStatement(sb, s, indent + "    ");
                sb.AppendLine($"{indent}}}");
            }
        }

        private void EmitForLoop(StringBuilder sb, HPCForLoop node, string indent)
        {
            var init = $"var {node.Iterator.Name} = {EmitExpr(node.Iterator.Initializer)}";
            var cond = EmitExpr(node.Condition);
            var incr = EmitExpr(node.Increment);
            sb.AppendLine($"{indent}for ({init}; {cond}; {incr})");
            sb.AppendLine($"{indent}{{");
            foreach (var s in node.Body)
                EmitStatement(sb, s, indent + "    ");
            sb.AppendLine($"{indent}}}");
        }

        // ── Expression emission ───────────────────────────────────────────────

        private string EmitExpr(HPCExpr expr) => expr switch
        {
            HPCVarRef v             => v.Name,
            HPCLiteral l            => EmitLiteral(l),
            HPCBinaryOp b           => EmitBinary(b),
            HPCUnaryOp u            => EmitUnary(u),
            HPCIntrinsicCall c      => EmitIntrinsic(c),
            HPCPropertyAccess p     => EmitPropertyAccess(p),
            HPCPassThroughCall pt   => pt.MethodName,  // verbatim
            _ => throw new NotSupportedException($"Unknown IR expr: {expr.GetType().Name}")
        };

        private string EmitLiteral(HPCLiteral lit)
        {
            // Scalar literal → broadcast to all lanes
            return $"Vector256.Create({lit.Value}{LiteralSuffix(lit.Type)})";
        }

        private string EmitBinary(HPCBinaryOp node)
        {
            var l = EmitExpr(node.Left);
            var r = EmitExpr(node.Right);

            return node.Kind switch
            {
                // Floating-point arithmetic uses Avx / Avx2
                HPCBinaryKind.Add      when node.Type.IsFloatingPoint => $"Avx.Add({l}, {r})",
                HPCBinaryKind.Subtract when node.Type.IsFloatingPoint => $"Avx.Subtract({l}, {r})",
                HPCBinaryKind.Multiply when node.Type.IsFloatingPoint => $"Avx.Multiply({l}, {r})",
                HPCBinaryKind.Divide   when node.Type.IsFloatingPoint => $"Avx.Divide({l}, {r})",

                // Integer arithmetic uses Avx2
                HPCBinaryKind.Add      => $"Avx2.Add({l}, {r})",
                HPCBinaryKind.Subtract => $"Avx2.Subtract({l}, {r})",
                HPCBinaryKind.Multiply => $"Avx2.MultiplyLow({l}, {r})",  // 32-bit int multiply

                // Bitwise
                HPCBinaryKind.BitwiseAnd => $"Avx2.And({l}, {r})",
                HPCBinaryKind.BitwiseOr  => $"Avx2.Or({l}, {r})",
                HPCBinaryKind.BitwiseXor => $"Avx2.Xor({l}, {r})",
                HPCBinaryKind.ShiftLeft  => $"Avx2.ShiftLeftLogical({l}, {r})",
                HPCBinaryKind.ShiftRight => $"Avx2.ShiftRightLogical({l}, {r})",

                // Comparisons — emit as AVX compare returning a mask vector
                HPCBinaryKind.Equal              when node.Type.IsFloatingPoint
                    => $"Avx.Compare({l}, {r}, FloatComparisonMode.OrderedEqualNonSignaling)",
                HPCBinaryKind.NotEqual           when node.Type.IsFloatingPoint
                    => $"Avx.Compare({l}, {r}, FloatComparisonMode.OrderedNotEqualNonSignaling)",
                HPCBinaryKind.LessThan           when node.Type.IsFloatingPoint
                    => $"Avx.Compare({l}, {r}, FloatComparisonMode.OrderedLessThanNonSignaling)",
                HPCBinaryKind.LessThanOrEqual    when node.Type.IsFloatingPoint
                    => $"Avx.Compare({l}, {r}, FloatComparisonMode.OrderedLessThanOrEqualNonSignaling)",
                HPCBinaryKind.GreaterThan        when node.Type.IsFloatingPoint
                    => $"Avx.Compare({l}, {r}, FloatComparisonMode.OrderedGreaterThanNonSignaling)",
                HPCBinaryKind.GreaterThanOrEqual when node.Type.IsFloatingPoint
                    => $"Avx.Compare({l}, {r}, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling)",

                // Integer comparisons
                HPCBinaryKind.Equal           => $"Avx2.CompareEqual({l}, {r})",
                HPCBinaryKind.GreaterThan     => $"Avx2.CompareGreaterThan({l}, {r})",
                HPCBinaryKind.LessThan        => $"Avx2.CompareGreaterThan({r}, {l})",  // reversed
                HPCBinaryKind.GreaterThanOrEqual => $"Avx2.Or(Avx2.CompareGreaterThan({l}, {r}), Avx2.CompareEqual({l}, {r}))",
                HPCBinaryKind.LessThanOrEqual    => $"Avx2.Or(Avx2.CompareGreaterThan({r}, {l}), Avx2.CompareEqual({l}, {r}))",
                HPCBinaryKind.NotEqual           => $"Avx2.Xor(Avx2.CompareEqual({l}, {r}), Vector256<{CsTypeName(node.Type)}>.AllBitsSet)",

                HPCBinaryKind.Modulo => throw new NotSupportedException("Modulo has no AVX2 intrinsic; consider reformulating."),

                _ => throw new NotSupportedException($"Binary kind {node.Kind} not supported in AVX2 backend")
            };
        }

        private string EmitUnary(HPCUnaryOp node)
        {
            var op = EmitExpr(node.Operand);
            return node.Kind switch
            {
                HPCUnaryKind.Negate    when node.Type.IsFloatingPoint
                    => $"Avx.Subtract(Vector256<{CsTypeName(node.Type)}>.Zero, {op})",
                HPCUnaryKind.Negate
                    => $"Avx2.Subtract(Vector256<{CsTypeName(node.Type)}>.Zero, {op})",
                HPCUnaryKind.BitwiseNot => $"Avx2.Xor({op}, Vector256<{CsTypeName(node.Type)}>.AllBitsSet)",

                // Math — delegate to Vector256 helpers (available in .NET 7+)
                HPCUnaryKind.Abs   when node.Type.IsFloatingPoint => $"Vector256.Abs({op})",
                HPCUnaryKind.Sqrt  when node.Type.IsFloatingPoint => $"Avx.Sqrt({op})",
                HPCUnaryKind.Floor when node.Type.IsFloatingPoint => $"Avx.Floor({op})",
                HPCUnaryKind.Ceil  when node.Type.IsFloatingPoint => $"Avx.Ceiling({op})",
                HPCUnaryKind.Round when node.Type.IsFloatingPoint
                    => $"Avx.RoundToNearestInteger({op})",
                HPCUnaryKind.Trunc when node.Type.IsFloatingPoint
                    => $"Avx.RoundToZero({op})",

                // Reciprocal / Rsqrt (float only; approximate 14-bit variants)
                HPCUnaryKind.Rcp   when node.Type.ElementTypeName == "float"
                    => $"Avx.Reciprocal({op})",
                HPCUnaryKind.Rsqrt when node.Type.ElementTypeName == "float"
                    => $"Avx.ReciprocalSqrt({op})",

                // Transcendentals — routed to AVX2Utility (generated by UtilityTemplate)
                HPCUnaryKind.Sin  => $"AVX2Utility.Sin_{CsTypeNameCap(node.Type)}_{MathModeSuffix()}({op})",
                HPCUnaryKind.Cos  => $"AVX2Utility.Cos_{CsTypeNameCap(node.Type)}_{MathModeSuffix()}({op})",
                HPCUnaryKind.Asin => $"AVX2Utility.Asin({op})",
                HPCUnaryKind.Atan => $"AVX2Utility.Atan({op})",
                HPCUnaryKind.Log  => $"AVX2Utility.Log_{CsTypeNameCap(node.Type)}_{MathModeSuffix()}({op})",
                HPCUnaryKind.Exp  => $"AVX2Utility.Exp_{CsTypeNameCap(node.Type)}_{MathModeSuffix()}({op})",

                // Frac = x - Floor(x)
                HPCUnaryKind.Frac when node.Type.IsFloatingPoint
                    => $"Avx.Subtract({op}, Avx.Floor({op}))",

                // Sign: extract and normalise sign bit
                HPCUnaryKind.Sign when node.Type.ElementTypeName == "float"
                    => $"Avx.And(Avx.CompareEqual({op}, Vector256<float>.Zero) == Vector256<float>.Zero ? Vector256.Create(1.0f) : Vector256<float>.Zero, Avx.Or(Avx.And({op}, Vector256.Create(-0.0f)), Vector256.Create(1.0f)))",

                // Saturate: clamp to [0,1]
                HPCUnaryKind.Saturate when node.Type.IsFloatingPoint
                    => $"Avx.Max(Avx.Min({op}, Vector256.Create(1.0{LiteralSuffix(node.Type)})), Vector256<{CsTypeName(node.Type)}>.Zero)",

                _ => throw new NotSupportedException($"Unary kind {node.Kind} not supported in AVX2 backend (type: {node.Type})")
            };
        }

        private string EmitIntrinsic(HPCIntrinsicCall node)
        {
            var args = node.Args;
            string A(int i) => EmitExpr(args[i]);

            return node.Intrinsic switch
            {
                // FMA — uses the FMA extension bundled with AVX2
                HPCIntrinsic.MultiplyAdd      => $"Fma.MultiplyAdd({A(0)}, {A(1)}, {A(2)})",
                HPCIntrinsic.MultiplySubtract => $"Fma.MultiplySubtract({A(0)}, {A(1)}, {A(2)})",

                // Math
                HPCIntrinsic.Atan2 => $"AVX2Utility.Atan2({A(0)}, {A(1)})",
                HPCIntrinsic.Pow   => $"AVX2Utility.Pow({A(0)}, {A(1)})",
                HPCIntrinsic.SinCos => $"AVX2Utility.SinCos_{CsTypeNameCap(node.Type)}_{MathModeSuffix()}({A(0)}, out {A(1)}, out {A(2)})",

                // Compound math
                HPCIntrinsic.Lerp  => $"Fma.MultiplyAdd(Avx.Subtract({A(1)}, {A(0)}), {A(2)}, {A(0)})",
                HPCIntrinsic.Min   when node.Type.IsFloatingPoint => $"Avx.Min({A(0)}, {A(1)})",
                HPCIntrinsic.Max   when node.Type.IsFloatingPoint => $"Avx.Max({A(0)}, {A(1)})",
                HPCIntrinsic.Min   => $"Avx2.Min({A(0)}, {A(1)})",
                HPCIntrinsic.Max   => $"Avx2.Max({A(0)}, {A(1)})",
                HPCIntrinsic.Clamp when node.Type.IsFloatingPoint => $"Avx.Max(Avx.Min({A(0)}, {A(2)}), {A(1)})",
                HPCIntrinsic.Clamp => $"Avx2.Max(Avx2.Min({A(0)}, {A(2)}), {A(1)})",

                // Conditional select via bitwise blend
                HPCIntrinsic.Select when node.Type.IsFloatingPoint
                    => $"Avx.BlendVariable({A(2)}, {A(1)}, {A(0)})",
                HPCIntrinsic.Select
                    => $"Avx2.BlendVariable({A(2)}.As<{CsTypeName(node.Type)}, byte>(), {A(1)}.As<{CsTypeName(node.Type)}, byte>(), {A(0)}.As<{CsTypeName(node.Type)}, byte>()).As<byte, {CsTypeName(node.Type)}>()",

                // CopySign: compose exponent+mantissa from A(0) and sign from A(1)
                HPCIntrinsic.CopySign when node.Type.ElementTypeName == "float"
                    => $"Avx.Or(Avx.And({A(0)}, Vector256.Create(0x7FFFFFFFu).AsSingle()), Avx.And({A(1)}, Vector256.Create(0x80000000u).AsSingle()))",

                // Horizontal reductions
                HPCIntrinsic.ReduceAdd => $"Vector256.Sum({A(0)})",
                HPCIntrinsic.ReduceMax => $"Vector256.Max({A(0)})",
                HPCIntrinsic.ReduceMin => $"Vector256.Min({A(0)})",

                // Memory — pointer-based (emitted as ref-to-pointer pattern)
                HPCIntrinsic.Load     => $"Vector256.LoadUnsafe(ref {A(0)})",
                HPCIntrinsic.MaskLoad => $"Avx2.MaskLoad(ref {A(0)}, {A(1)}.AsInt32())",
                HPCIntrinsic.Store    => $"Vector256.StoreUnsafe({A(0)}, ref {A(1)})",
                HPCIntrinsic.MaskStore => $"Avx2.MaskStore(ref {A(1)}, {A(2)}.AsInt32(), {A(0)})",

                HPCIntrinsic.Gather     => $"Avx2.GatherVector256(ref {A(0)}, {A(1)}, {A(2)})",
                HPCIntrinsic.MaskGather => $"Avx2.GatherMaskVector256({A(0)}, ref {A(1)}, {A(2)}, {A(3)}, {A(4)})",

                HPCIntrinsic.CompressStore
                    => $"/* CompressStore requires AVX-512VBMI2; use scalar fallback */ {A(0)}.CompressStore(ref {A(1)}, {A(2)})",

                // Conversions
                HPCIntrinsic.Cast    => $"{A(0)}.As<{CsTypeName(args[0].Type)}, {CsTypeName(node.Type)}>()",
                HPCIntrinsic.BitCast => $"{A(0)}.As<{CsTypeName(args[0].Type)}, {CsTypeName(node.Type)}>()",

                _ => throw new NotSupportedException($"Intrinsic {node.Intrinsic} not implemented in AVX2 backend")
            };
        }

        private static string EmitPropertyAccess(HPCPropertyAccess node) =>
            $"{node.Target}.{node.PropertyName}";

        // ── Signature helpers ─────────────────────────────────────────────────

        private static string EmitReturnType(HPCType type) =>
            type.ElementTypeName == "void"
                ? "void"
                : $"Vector256<{type.ElementTypeName}>";

        private static string EmitParameterList(HPCMethodIR method)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var p in method.Parameters)
            {
                var prefix = (p.IsOut ? "out " : "") + (p.IsRef ? "ref " : "");
                var typeName = p.Type.ElementTypeName == "void"
                    ? "void"
                    : $"Vector256<{p.Type.ElementTypeName}>";
                parts.Add($"{prefix}{typeName} {p.Name}");
            }
            return string.Join(", ", parts);
        }

        // ── Naming helpers ────────────────────────────────────────────────────

        private static string CsTypeName(HPCType t) => t.ElementTypeName;

        private static string CsTypeNameCap(HPCType t) => t.ElementTypeName switch
        {
            "float"  or "Single" => "Single",
            "double" or "Double" => "Double",
            _ => t.ElementTypeName
        };

        private static string LiteralSuffix(HPCType t) => t.ElementTypeName switch
        {
            "float"  or "Single" => "f",
            "double" or "Double" => "d",
            _ => ""
        };

        // ── Mode-aware suffix ─────────────────────────────────────────────────

        // The backend does not hold state for the current method's MathMode by default;
        // instead EmitMethod passes context through a field set per-call.
        private MathMode _currentMode = MathMode.Standard;

        public string EmitMethod(HPCMethodIR method, MathMode mode)
        {
            _currentMode = mode;
            return EmitMethod(method);
        }

        private string MathModeSuffix() => _currentMode == MathMode.Fast ? "Fast" : "Standard";
    }
}
