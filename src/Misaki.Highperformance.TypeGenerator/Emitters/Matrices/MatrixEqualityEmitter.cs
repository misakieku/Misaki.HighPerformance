using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Matrices;

public static class MatrixEqualityEmitter
{
    public static void EmitEquality(CodeWriter writer, MatrixType mat)
    {
        using (writer.Region("Equality & Formatting"))
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public override readonly bool Equals(object? obj) => obj is {mat.Name} other && Equals(other);");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public readonly bool Equals({mat.Name} other) => this == other;");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public override readonly int GetHashCode() => global::System.HashCode.Combine({string.Join(", ", mat.ColumnNames.Select(c => $"this.{c}"))});");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            var colPrints = string.Join(", ", mat.ColumnNames.Select(c => $"{c}: {{this.{c}}}"));
            writer.WriteLine($"public override readonly string ToString() => $\"{mat.Name}({colPrints})\";");
            writer.WriteLine();
        }
    }
}
