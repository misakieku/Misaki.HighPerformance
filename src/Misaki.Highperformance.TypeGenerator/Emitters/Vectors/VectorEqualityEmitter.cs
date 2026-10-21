using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Vectors;

public static class VectorEqualityEmitter
{
    public static void EmitEquality(CodeWriter writer, VectorType vec)
    {
        using (writer.Region("Equality & Formatting"))
        {
            var comps = vec.Components;

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public override readonly bool Equals(object? obj) => obj is {vec.Name} other && Equals(other);");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public readonly bool Equals({vec.Name} other) => global::Misaki.HighPerformance.Mathematics.math.all(this == other);");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public override readonly int GetHashCode() => global::System.HashCode.Combine({string.Join(", ", comps.Select(c => $"this.{c}"))});");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public override readonly string ToString() => $\"({string.Join(", ", comps.Select(c => $"{c}: {{this.{c}}}"))})\";");
            writer.WriteLine();
        }
    }
}
