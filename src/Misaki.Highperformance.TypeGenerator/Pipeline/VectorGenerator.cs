using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Emitters.Vectors;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Pipeline;

public static class VectorGenerator
{
    public static string Generate(VectorType vec, bool emitMath = true)
    {
        var writer = new CodeWriter();

        FileHeaderEmitter.EmitHeader(writer);

        writer.WriteLine("namespace Misaki.HighPerformance.Mathematics;");
        writer.WriteLine();

        var structLayoutAttr = vec.Storage != StorageLayout.Sequential
            ? "[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]"
            : "[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]";

        writer.WriteLine(structLayoutAttr);
        using (writer.Scope($"public partial struct {vec.Name} : global::System.IEquatable<{vec.Name}>"))
        {
            VectorLayoutEmitter.EmitFieldsAndIndexers(writer, vec);
            VectorConstructorEmitter.EmitConstructors(writer, vec);
            VectorSwizzleEmitter.EmitSwizzles(writer, vec);
            VectorOperatorEmitter.EmitOperators(writer, vec);
            VectorEqualityEmitter.EmitEquality(writer, vec);
        }

        if (emitMath)
        {
            writer.WriteLine();
            VectorMathEmitter.EmitMathMethods(writer, vec);
        }

        writer.WriteLine();
        VectorInteropEmitter.EmitInterop(writer, vec);

        return writer.ToString();
    }
}
