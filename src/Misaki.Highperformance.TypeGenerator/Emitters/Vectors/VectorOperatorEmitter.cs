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
        // == and !=
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {boolType} operator ==({vec.Name} lhs, {vec.Name} rhs) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} == rhs.{c}"))});");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {boolType} operator !=({vec.Name} lhs, {vec.Name} rhs) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} != rhs.{c}"))});");
        writer.WriteLine();

        if (vec.Element.IsBool) return;

        // <, <=, >, >=
        string[] relOps = ["<", "<=", ">", ">="];
        foreach (var op in relOps)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {boolType} operator {op}({vec.Name} lhs, {vec.Name} rhs) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs.{c}"))});");
            writer.WriteLine();
        }
    }

    private static void EmitUnaryOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        if (vec.Element.IsSigned)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator -({vec.Name} value) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"-value.{c}"))});");
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
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"~value.{c}"))});");
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
        string[] ops = ["+", "-", "*", "/", "%"];

        foreach (var op in ops)
        {
            // Vector op Vector
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} lhs, {vec.Name} rhs) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs.{c}"))});");
            writer.WriteLine();

            // Vector op Scalar
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} lhs, {vec.Element.LogicalTypeName} rhs) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs"))});");
            writer.WriteLine();

            // Scalar op Vector
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Element.LogicalTypeName} lhs, {vec.Name} rhs) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs {op} rhs.{c}"))});");
            writer.WriteLine();
        }
    }

    private static void EmitBitwiseOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
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
                    if (vec.Storage == StorageLayout.SimdUnion128)
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
                    writer.WriteLine($"return new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs.{c}"))});");
                }
            }
            writer.WriteLine();

            if (!vec.Element.IsBool)
            {
                // Vector op Scalar
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} lhs, {vec.Element.LogicalTypeName} rhs) =>");
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs.{c} {op} rhs"))});");
                writer.WriteLine();

                // Scalar op Vector
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} operator {op}({vec.Element.LogicalTypeName} lhs, {vec.Name} rhs) =>");
                writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"lhs {op} rhs.{c}"))});");
                writer.WriteLine();
            }
        }
    }

    private static void EmitShiftOperators(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        string[] ops = ["<<", ">>"];
        foreach (var op in ops)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} operator {op}({vec.Name} x, int n) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"x.{c} {op} n"))});");
            writer.WriteLine();
        }
    }
}
