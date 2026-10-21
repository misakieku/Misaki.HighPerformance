using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Vectors;

public static class VectorLayoutEmitter
{
    public static void EmitFieldsAndIndexers(CodeWriter writer, VectorType vec)
    {
        using (writer.Region("Fields & Constants"))
        {
            var isExplicit = vec.Storage != StorageLayout.Sequential;

            if (isExplicit)
            {
                var bits = vec.SimdVectorBits;
                writer.WriteLine("[global::System.Text.Json.Serialization.JsonIgnore]");
                writer.WriteLine("[global::System.Runtime.InteropServices.FieldOffset(0)]");
                writer.WriteLine($"public global::System.Runtime.Intrinsics.Vector{bits}<{vec.Element.StorageTypeName}> __v;");
                writer.WriteLine();

                for (var i = 0; i < vec.Dimension; i++)
                {
                    writer.WriteLine("[global::System.Text.Json.Serialization.JsonInclude]");
                    writer.WriteLine($"[global::System.Runtime.InteropServices.FieldOffset({i * vec.Element.SizeInBytes})]");
                    writer.WriteLine($"public {vec.Element.StorageTypeName} {vec.Components[i]};");
                }
            }
            else
            {
                for (var i = 0; i < vec.Dimension; i++)
                {
                    writer.WriteLine("[global::System.Text.Json.Serialization.JsonInclude]");
                    writer.WriteLine($"public {vec.Element.StorageTypeName} {vec.Components[i]};");
                }
            }

            writer.WriteLine();

            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Arithmetic))
            {
                writer.WriteLine($"public static {vec.Name} zero => new({string.Join(", ", Enumerable.Repeat(vec.Element.ZeroLiteral, vec.Dimension))});");
                writer.WriteLine($"public static {vec.Name} one => new({string.Join(", ", Enumerable.Repeat(vec.Element.OneLiteral, vec.Dimension))});");

                for (var i = 0; i < vec.Dimension; i++)
                {
                    var unitArgs = Enumerable.Range(0, vec.Dimension)
                        .Select(j => j == i ? vec.Element.OneLiteral : vec.Element.ZeroLiteral);
                    writer.WriteLine($"public static {vec.Name} unit{vec.Components[i].ToUpperInvariant()} => new({string.Join(", ", unitArgs)});");
                }

                writer.WriteLine();
            }

            writer.WriteLine("[global::System.Diagnostics.CodeAnalysis.UnscopedRef]");
            writer.WriteLine($"public unsafe ref {vec.Element.StorageTypeName} this[int index]");
            using (writer.Scope(""))
            {
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"get => ref global::System.Runtime.CompilerServices.Unsafe.Add(ref {vec.Components[0]}, index);");
            }
        }
    }
}
