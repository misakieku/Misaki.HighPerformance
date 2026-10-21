using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Vectors;

public static class VectorInteropEmitter
{
    public static void EmitInterop(CodeWriter writer, VectorType vec)
    {
        // Only for types that support SIMD (float, double, int, uint, and bool4)
        if (!vec.Element.HasHardwareSimd && !(vec.Element.IsBool && vec.Dimension == 4))
        {
            return;
        }

        writer.WriteLine("#pragma warning disable CS8981");
        using (writer.Scope("public static partial class VectorInterop"))
        {
            var bits = vec.Storage switch
            {
                StorageLayout.SimdUnion256 => 256,
                _ => 128
            };

            // 1. AsVector128 / AsVector256
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static global::System.Runtime.Intrinsics.Vector{bits}<{vec.Element.StorageTypeName}> AsVector{bits}(this {vec.Name} value)");
            using (writer.Scope(""))
            {
                if (vec.Storage != StorageLayout.Sequential)
                {
                    writer.WriteLine("return value.__v;");
                }
                else
                {
                    var padCount = (bits / 8) / vec.Element.SizeInBytes;
                    var args = string.Join(", ", Enumerable.Range(0, padCount).Select(i => i < vec.Dimension ? $"value.{vec.Components[i]}" : "default"));
                    writer.WriteLine($"return global::System.Runtime.Intrinsics.Vector{bits}.Create({args});");
                }
            }
            writer.WriteLine();

            // 2. As{vec.Name} on Vector128<T> / Vector256<T>
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} As{vec.Name}(this global::System.Runtime.Intrinsics.Vector{bits}<{vec.Element.StorageTypeName}> value)");
            using (writer.Scope(""))
            {
                if (vec.Storage != StorageLayout.Sequential)
                {
                    writer.WriteLine($"return global::System.Runtime.CompilerServices.Unsafe.BitCast<global::System.Runtime.Intrinsics.Vector{bits}<{vec.Element.StorageTypeName}>, {vec.Name}>(value);");
                }
                else
                {
                    writer.WriteLine($"ref var address = ref global::System.Runtime.CompilerServices.Unsafe.As<global::System.Runtime.Intrinsics.Vector{bits}<{vec.Element.StorageTypeName}>, byte>(ref value);");
                    writer.WriteLine($"return global::System.Runtime.CompilerServices.Unsafe.ReadUnaligned<{vec.Name}>(ref address);");
                }
            }
        }
        writer.WriteLine("#pragma warning restore CS8981");
    }
}
