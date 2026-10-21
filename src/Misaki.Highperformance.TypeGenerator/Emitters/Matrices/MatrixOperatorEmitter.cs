using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Matrices;

public static class MatrixOperatorEmitter
{
    public static void EmitOperators(CodeWriter writer, MatrixType mat)
    {
        using (writer.Region("Operators"))
        {
            // 1. Equality operators
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static bool operator ==({mat.Name} lhs, {mat.Name} rhs) =>");
            writer.WriteLine($"    {string.Join(" && ", mat.ColumnNames.Select(c => $"global::Misaki.HighPerformance.Mathematics.math.all(lhs.{c} == rhs.{c})"))};");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static bool operator !=({mat.Name} lhs, {mat.Name} rhs) => !(lhs == rhs);");
            writer.WriteLine();

            // 2. Arithmetic operators
            if (mat.Element.Capabilities.HasFlag(TypeCapabilities.Arithmetic))
            {
                // Unary
                if (mat.Element.IsSigned)
                {
                    writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                    writer.WriteLine($"public static {mat.Name} operator -({mat.Name} m) =>");
                    writer.WriteLine($"    new({string.Join(", ", mat.ColumnNames.Select(c => $"-m.{c}"))});");
                    writer.WriteLine();

                    writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                    writer.WriteLine($"public static {mat.Name} operator +({mat.Name} m) => m;");
                    writer.WriteLine();
                }

                // Binary matrix + matrix, matrix - matrix
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {mat.Name} operator +({mat.Name} lhs, {mat.Name} rhs) =>");
                writer.WriteLine($"    new({string.Join(", ", mat.ColumnNames.Select(c => $"lhs.{c} + rhs.{c}"))});");
                writer.WriteLine();

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {mat.Name} operator -({mat.Name} lhs, {mat.Name} rhs) =>");
                writer.WriteLine($"    new({string.Join(", ", mat.ColumnNames.Select(c => $"lhs.{c} - rhs.{c}"))});");
                writer.WriteLine();

                // Scalar multiply & divide
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {mat.Name} operator *({mat.Name} lhs, {mat.Element.LogicalTypeName} rhs) =>");
                writer.WriteLine($"    new({string.Join(", ", mat.ColumnNames.Select(c => $"lhs.{c} * rhs"))});");
                writer.WriteLine();

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {mat.Name} operator *({mat.Element.LogicalTypeName} lhs, {mat.Name} rhs) =>");
                writer.WriteLine($"    new({string.Join(", ", mat.ColumnNames.Select(c => $"lhs * rhs.{c}"))});");
                writer.WriteLine();

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {mat.Name} operator /({mat.Name} lhs, {mat.Element.LogicalTypeName} rhs) =>");
                writer.WriteLine($"    new({string.Join(", ", mat.ColumnNames.Select(c => $"lhs.{c} / rhs"))});");
                writer.WriteLine();
            }
        }
    }
}
