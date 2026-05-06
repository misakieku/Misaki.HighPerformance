using System;

namespace Misaki.HighPerformance.HPC.Generator.IR
{
    // ─────────────────────────────────────────────────────────────────────────
    // Type system
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A resolved primitive type inside the HPC IR (always the element scalar type).
    /// E.g. "float", "double", "int". Never a vector type name — the IR is
    /// element-type–centric; the backend decides the concrete vector width.
    /// </summary>
    internal sealed record HPCType(string ElementTypeName, bool IsFloatingPoint)
    {
        // Convenience singletons for the most common cases
        public static readonly HPCType Float  = new("float",  IsFloatingPoint: true);
        public static readonly HPCType Double = new("double", IsFloatingPoint: true);
        public static readonly HPCType Int    = new("int",    IsFloatingPoint: false);
        public static readonly HPCType UInt   = new("uint",   IsFloatingPoint: false);
        public static readonly HPCType Long   = new("long",   IsFloatingPoint: false);

        public static HPCType FromElementName(string name) => name switch
        {
            "float"  or "Single" => Float,
            "double" or "Double" => Double,
            "int"    or "Int32"  => Int,
            "uint"   or "UInt32" => UInt,
            "long"   or "Int64"  => Long,
            _ => new(name, IsFloatingPoint: false)
        };

        public override string ToString() => ElementTypeName;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Expression nodes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Base type for all HPC IR expression nodes.</summary>
    internal abstract record HPCExpr(HPCType Type);

    /// <summary>A reference to a local variable or parameter by name.</summary>
    internal sealed record HPCVarRef(string Name, HPCType Type) : HPCExpr(Type)
    {
        public override string ToString() => Name;
    }

    /// <summary>A scalar literal value broadcast to all lanes.</summary>
    internal sealed record HPCLiteral(string Value, HPCType Type) : HPCExpr(Type)
    {
        public override string ToString() => Value;
    }

    // ── Binary ops ───────────────────────────────────────────────────────────

    internal enum HPCBinaryKind
    {
        Add, Subtract, Multiply, Divide, Modulo,
        BitwiseAnd, BitwiseOr, BitwiseXor, ShiftLeft, ShiftRight,
        Equal, NotEqual,
        LessThan, LessThanOrEqual,
        GreaterThan, GreaterThanOrEqual,
    }

    internal sealed record HPCBinaryOp(
        HPCBinaryKind Kind,
        HPCExpr Left,
        HPCExpr Right,
        HPCType Type) : HPCExpr(Type);

    // ── Unary ops ────────────────────────────────────────────────────────────

    internal enum HPCUnaryKind
    {
        Negate, BitwiseNot,
        // Math functions that map to a single ISPMDLane static method
        Abs, Sqrt, Floor, Ceil, Round, Trunc, Frac, Sign, Saturate,
        Rcp, Rsqrt,
        Sin, Cos, Tan, Asin, Acos, Atan,
        Exp, Exp2, Log, Log2,
    }

    internal sealed record HPCUnaryOp(
        HPCUnaryKind Kind,
        HPCExpr Operand,
        HPCType Type) : HPCExpr(Type);

    // ── Intrinsic calls (multi-argument) ────────────────────────────────────

    internal enum HPCIntrinsic
    {
        // Arithmetic
        MultiplyAdd,            // a * b + c (FMA)
        MultiplySubtract,       // a * b - c

        // Math
        Atan2, Pow,
        SinCos,                 // out sin, out cos simultaneously
        Lerp, Min, Max, Clamp, Select, CopySign,

        // Reduction
        ReduceAdd, ReduceMax, ReduceMin,

        // Memory
        Load, Store,
        MaskLoad, MaskStore,
        Gather, MaskGather,
        Scatter, MaskScatter,
        CompressStore,

        // Conversion
        Cast, BitCast,
    }

    internal sealed record HPCIntrinsicCall(
        HPCIntrinsic Intrinsic,
        HPCExpr[] Args,
        HPCType Type) : HPCExpr(Type);

    /// <summary>
    /// A method call that the HPC pipeline does not recognise as an intrinsic —
    /// emitted verbatim so user code can still call arbitrary helpers.
    /// </summary>
    internal sealed record HPCPassThroughCall(
        string MethodName,
        HPCExpr[] Args,
        HPCType Type) : HPCExpr(Type);

    /// <summary>Member-property access, e.g. <c>lane.LaneWidth</c> → <c>Count</c>.</summary>
    internal sealed record HPCPropertyAccess(
        HPCExpr Target,
        string PropertyName,
        HPCType Type) : HPCExpr(Type);

    // ─────────────────────────────────────────────────────────────────────────
    // Statement nodes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Base type for all HPC IR statement nodes.</summary>
    internal abstract record HPCStmt;

    /// <summary>Local variable declaration with mandatory initialiser.</summary>
    internal sealed record HPCVarDecl(
        string Name,
        HPCType Type,
        HPCExpr Initializer) : HPCStmt;

    /// <summary>Assignment to an existing variable (including out-parameters).</summary>
    internal sealed record HPCAssignment(string Target, HPCExpr Value) : HPCStmt;

    /// <summary>Expression evaluated purely for its side-effect (e.g. Store calls).</summary>
    internal sealed record HPCExprStmt(HPCExpr Expression) : HPCStmt;

    /// <summary>Return statement.</summary>
    internal sealed record HPCReturn(HPCExpr? Value) : HPCStmt;

    /// <summary>
    /// Conditional branch. In a vectorised context the backend may lower this
    /// to a predicated Select or a masked execution block.
    /// </summary>
    internal sealed record HPCIf(
        HPCExpr Condition,
        HPCStmt[] ThenBody,
        HPCStmt[]? ElseBody) : HPCStmt;

    /// <summary>Simple counted for-loop.</summary>
    internal sealed record HPCForLoop(
        HPCVarDecl Iterator,
        HPCExpr Condition,
        HPCExpr Increment,
        HPCStmt[] Body) : HPCStmt;

    // ─────────────────────────────────────────────────────────────────────────
    // Method-level IR
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed record HPCParameter(
        string Name,
        HPCType Type,
        bool IsOut,
        bool IsRef);

    /// <summary>
    /// The complete IR representation of a single <c>[HPCompute]</c>-annotated method,
    /// fully detached from Roslyn syntax after the analysis phase.
    /// </summary>
    internal sealed record HPCMethodIR
    {
        public required string Name { get; init; }
        public required HPCType ReturnType { get; init; }
        public required HPCParameter[] Parameters { get; init; }
        public required HPCStmt[] Body { get; init; }

        // Compilation metadata from the attribute
        public required TargetInstructionSet TargetISA { get; init; }
        public required FloatPrecision Precision { get; init; }
        public required MathMode Mode { get; init; }

        // Emission metadata
        public required string ContainingNamespace { get; init; }
        public required string ContainingTypeName { get; init; }
        public required string OriginalName { get; init; }
    }
}
