using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Vectors;

public static class VectorOperatorEmitter
{
    public static void EmitOperators(CodeWriter writer, VectorType vec)
    {
        using (writer.Region("Operators"))
        {
            var comps = vec.Components;
            var boolType = vec.BoolVectorName;

            // 1. Relational operators (==, !=, <, <=, >, >=)
            EmitRelationalOperators(writer, vec, comps, boolType);

            // 2. Unary operators (-, +, ~, !)
            EmitUnaryOperators(writer, vec, comps);

            // 3. Binary arithmetic operators (+, -, *, /, %)
            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Arithmetic))
            {
                EmitArithmeticOperators(writer, vec, comps);
            }

            // 4. Bitwise operators (&, |, ^)
            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Bitwise))
            {
                EmitBitwiseOperators(writer, vec, comps);
            }

            // 5. Shift operators (<<, >>)
            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Shift))
            {
                EmitShiftOperators(writer, vec, comps);
            }
        }
    }

    private static void EmitRelationalOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps, string boolType)
    {
        var isSimd = vec.Storage != StorageLayout.Sequential;
        var bits = vec.SimdVectorBits;
        var vecClass = $"global::System.Runtime.Intrinsics.Vector{bits}";

        // == and !=
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {boolType} operator ==({vec.Name} lhs, {vec.Name} rhs) =>");
        if (isSimd && vec.Dimension == 4)
        {
            if (vec.Element.Kind == ScalarKind.Double)
            {
                writer.WriteLine($"    global::Misaki.HighPerformance.Mathematics.math.PackVector256(global::System.Runtime.Intrinsics.Vector256.AsUInt32({vecClass}.Equals(lhs.__v, rhs.__v))).Asbool4();");
            }
            else if (vec.Element.Kind == ScalarKind.UInt || vec.Element.IsBool)
            {
                writer.WriteLine($"    {vecClass}.Equals(lhs.__v, rhs.__v).Asbool4();");
            }
            else
            {
                writer.WriteLine($"    {vecClass}.AsUInt32({vecClass}.Equals(lhs.__v, rhs.__v)).Asbool4();");
            }
        }
        else
        {
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} == rhs.{c}"))});");
        }
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {boolType} operator !=({vec.Name} lhs, {vec.Name} rhs) =>");
        if (isSimd && vec.Dimension == 4)
        {
            if (vec.Element.Kind == ScalarKind.Double)
            {
                writer.WriteLine($"    (~global::Misaki.HighPerformance.Mathematics.math.PackVector256(global::System.Runtime.Intrinsics.Vector256.AsUInt32({vecClass}.Equals(lhs.__v, rhs.__v)))).Asbool4();");
            }
            else if (vec.Element.Kind == ScalarKind.UInt || vec.Element.IsBool)
            {
                writer.WriteLine($"    (~{vecClass}.Equals(lhs.__v, rhs.__v)).Asbool4();");
            }
            else
            {
                writer.WriteLine($"    (~{vecClass}.AsUInt32({vecClass}.Equals(lhs.__v, rhs.__v))).Asbool4();");
            }
        }
        else
        {
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} != rhs.{c}"))});");
        }
        writer.WriteLine();

        if (vec.Element.IsBool)
            return;

        // <, <=, >, >=
        string[] relOps = ["<", "<=", ">", ">="];
        string[] simdMethods = ["LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual"];
        for (var i = 0; i < relOps.Length; i++)
        {
            var op = relOps[i];
            var simdMethod = simdMethods[i];

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {boolType} operator {op}({vec.Name} lhs, {vec.Name} rhs) =>");
            if (isSimd && vec.Dimension == 4)
            {
                if (vec.Element.Kind == ScalarKind.Double)
                {
                    writer.WriteLine($"    global::Misaki.HighPerformance.Mathematics.math.PackVector256(global::System.Runtime.Intrinsics.Vector256.AsUInt32({vecClass}.{simdMethod}(lhs.__v, rhs.__v))).Asbool4();");
                }
                else if (vec.Element.Kind == ScalarKind.UInt)
                {
                    writer.WriteLine($"    {vecClass}.{simdMethod}(lhs.__v, rhs.__v).Asbool4();");
                }
                else
                {
                    writer.WriteLine($"    {vecClass}.AsUInt32({vecClass}.{simdMethod}(lhs.__v, rhs.__v)).Asbool4();");
                }
            }
            else
            {
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs.{c}"))});");
            }
            writer.WriteLine();
        }
    }

    private static void EmitUnaryOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var isSimd = vec.Storage != StorageLayout.Sequential;

        if (vec.Element.IsSigned)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator -({vec.Name} value) =>");
            if (isSimd)
            {
                writer.WriteLine($"    (-value.__v).As{vec.Name}();");
            }
            else
            {
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"-value.{c}"))});");
            }
            writer.WriteLine();
        }

        if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Arithmetic))
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator +({vec.Name} value) => value;");
            writer.WriteLine();
        }

        if (vec.Element.IsInteger)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator ~({vec.Name} value) =>");
            if (isSimd)
            {
                writer.WriteLine($"    (~value.__v).As{vec.Name}();");
            }
            else
            {
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"~value.{c}"))});");
            }
            writer.WriteLine();
        }

        if (vec.Element.IsBool)
        {
            if (vec.Storage == StorageLayout.SimdUnion128)
            {
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator !({vec.Name} value) => (~value.__v).As{vec.Name}();");
                writer.WriteLine();

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator ~({vec.Name} value) => (~value.__v).As{vec.Name}();");
                writer.WriteLine();
            }
            else
            {
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator !({vec.Name} value) =>");
                writer.WriteLine($"    new {vec.Name} {{ {string.Join(", ", comps.Select(c => $"{c} = ~value.{c}"))} }};");
                writer.WriteLine();

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator ~({vec.Name} value) =>");
                writer.WriteLine($"    new {vec.Name} {{ {string.Join(", ", comps.Select(c => $"{c} = ~value.{c}"))} }};");
                writer.WriteLine();
            }
        }
    }

    private static void EmitArithmeticOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var isSimd = vec.Storage != StorageLayout.Sequential;
        var bits = vec.SimdVectorBits;
        var vecClass = $"global::System.Runtime.Intrinsics.Vector{bits}";
        string[] ops = ["+", "-", "*", "/", "%"];

        foreach (var op in ops)
        {
            var canSimd = isSimd && (op != "%") && (op != "/" || vec.Element.IsFloatingPoint);

            // Vector op Vector
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} lhs, {vec.Name} rhs) =>");
            if (canSimd)
            {
                writer.WriteLine($"    (lhs.__v {op} rhs.__v).As{vec.Name}();");
            }
            else
            {
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs.{c}"))});");
            }
            writer.WriteLine();

            // Vector op Scalar
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} lhs, {vec.Element.LogicalTypeName} rhs) =>");
            if (canSimd)
            {
                writer.WriteLine($"    (lhs.__v {op} {vecClass}.Create(rhs)).As{vec.Name}();");
            }
            else
            {
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs"))});");
            }
            writer.WriteLine();

            // Scalar op Vector
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Element.LogicalTypeName} lhs, {vec.Name} rhs) =>");
            if (canSimd)
            {
                writer.WriteLine($"    ({vecClass}.Create(lhs) {op} rhs.__v).As{vec.Name}();");
            }
            else
            {
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs {op} rhs.{c}"))});");
            }
            writer.WriteLine();
        }
    }

    private static void EmitBitwiseOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var isSimd = vec.Storage != StorageLayout.Sequential;
        var bits = vec.SimdVectorBits;
        var vecClass = $"global::System.Runtime.Intrinsics.Vector{bits}";
        string[] ops = ["&", "|", "^"];

        foreach (var op in ops)
        {
            // Vector op Vector
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} lhs, {vec.Name} rhs)");
            using (writer.Scope(""))
            {
                if (vec.Element.IsBool)
                {
                    if (isSimd)
                    {
                        writer.WriteLine($"return (lhs.__v {op} rhs.__v).As{vec.Name}();");
                    }
                    else
                    {
                        var args = string.Join(", ", comps.Select(c => $"{c} = lhs.{c} {op} rhs.{c}"));
                        writer.WriteLine($"return new {vec.Name} {{ {args} }};");
                    }
                }
                else
                {
                    if (isSimd)
                    {
                        writer.WriteLine($"return (lhs.__v {op} rhs.__v).As{vec.Name}();");
                    }
                    else
                    {
                        writer.WriteLine($"return new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs.{c}"))});");
                    }
                }
            }
            writer.WriteLine();

            if (!vec.Element.IsBool)
            {
                // Vector op Scalar
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} lhs, {vec.Element.LogicalTypeName} rhs) =>");
                if (isSimd)
                {
                    writer.WriteLine($"    (lhs.__v {op} {vecClass}.Create(rhs)).As{vec.Name}();");
                }
                else
                {
                    writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs"))});");
                }
                writer.WriteLine();

                // Scalar op Vector
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator {op}({vec.Element.LogicalTypeName} lhs, {vec.Name} rhs) =>");
                if (isSimd)
                {
                    writer.WriteLine($"    ({vecClass}.Create(lhs) {op} rhs.__v).As{vec.Name}();");
                }
                else
                {
                    writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs {op} rhs.{c}"))});");
                }
                writer.WriteLine();
            }
        }
    }

    private static void EmitShiftOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var isSimd = vec.Storage != StorageLayout.Sequential;
        string[] ops = ["<<", ">>"];
        foreach (var op in ops)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} x, int n) =>");
            if (isSimd)
            {
                writer.WriteLine($"    (x.__v {op} n).As{vec.Name}();");
            }
            else
            {
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"x.{c} {op} n"))});");
            }
            writer.WriteLine();
        }
    }
}
