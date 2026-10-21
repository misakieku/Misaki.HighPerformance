using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Emitters.Matrices;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Pipeline;

public static class MatrixGenerator
{
    public static string Generate(MatrixType mat, bool emitMath = true)
    {
        var writer = new CodeWriter();

        FileHeaderEmitter.EmitHeader(writer);

        writer.WriteLine("namespace Misaki.HighPerformance.Mathematics;");
        writer.WriteLine();

        writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]");
        using (writer.Scope($"public partial struct {mat.Name} : global::System.IEquatable<{mat.Name}>"))
        {
            MatrixLayoutEmitter.EmitFieldsAndIndexers(writer, mat);
            MatrixConstructorEmitter.EmitConstructors(writer, mat);
            MatrixOperatorEmitter.EmitOperators(writer, mat);
            MatrixEqualityEmitter.EmitEquality(writer, mat);
        }

        if (emitMath)
        {
            writer.WriteLine();
            MatrixMathEmitter.EmitMathMethods(writer, mat);
        }

        return writer.ToString();
    }
}
