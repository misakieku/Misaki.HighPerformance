using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Vectors;

public static class VectorMathEmitter
{
    private static readonly string[] s_compPool = ["x", "y", "z", "w"];

    public static void EmitMathMethods(CodeWriter writer, VectorType vec)
    {
        writer.WriteLine("#pragma warning disable CS8981");
        using (writer.Scope("public static partial class math"))
        {
            var comps = vec.Components;

            // 1. Math factory methods
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            var compParams = string.Join(", ", comps.Select(c => $"{vec.Element.LogicalTypeName} {c}"));
            writer.WriteLine($"public static {vec.Name} {vec.Name}({compParams}) => new({string.Join(", ", comps)});");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} {vec.Name}({vec.Element.LogicalTypeName} value) => new(value);");
            writer.WriteLine();

            var otherScalars = TypeRegistry.AllScalars.Where(s => s.Kind != vec.Element.Kind);
            foreach (var other in otherScalars)
            {
                var otherVecName = $"{other.Name}{vec.Dimension}";
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} {vec.Name}({otherVecName} value) => new(value);");
                writer.WriteLine();

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {vec.Name} {vec.Name}({other.LogicalTypeName} value) => new(value);");
                writer.WriteLine();
            }

            // Subvector math factories (e.g. float4(float3, float))
            EmitSubvectorFactories(writer, vec);

            // 2. Logic functions (for bool vectors)
            if (vec.Element.IsBool)
            {
                EmitLogicFunctions(writer, vec, comps);
                EmitShuffleMethods(writer, vec);
                writer.WriteLine("#pragma warning restore CS8981");
                return;
            }

            // 3. Min, Max, Clamp, Abs, Sign
            EmitBasicMathFunctions(writer, vec, comps);

            // 4. Trigonometric & Hyperbolic
            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Trig))
            {
                EmitTrigFunctions(writer, vec, comps);
            }

            // 5. Exponential & Logarithmic
            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Exponential))
            {
                EmitExponentialFunctions(writer, vec, comps);
            }

            // 6. Rounding, Saturate, Lerp, Mad
            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.Rounding))
            {
                EmitRoundingAndInterpolation(writer, vec, comps);
            }

            // 7. Linear Algebra & Reductions
            if (vec.Element.Capabilities.HasFlag(TypeCapabilities.LinearAlgebra))
            {
                EmitLinearAlgebra(writer, vec, comps);
            }

            // 8. Shuffle methods
            EmitShuffleMethods(writer, vec);
        }
        writer.WriteLine("#pragma warning restore CS8981");
    }

    private static void EmitLogicFunctions(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        if (vec.Storage == StorageLayout.SimdUnion128)
        {
            // any(bool4) using vector intrinsic
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static bool any({vec.Name} x) =>");
            writer.WriteLine("    global::System.Runtime.Intrinsics.Vector128.ExtractMostSignificantBits(x.__v) != 0;");
            writer.WriteLine();

            // all(bool4) using vector intrinsic
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static bool all({vec.Name} x) =>");
            writer.WriteLine("    global::System.Runtime.Intrinsics.Vector128.ExtractMostSignificantBits(x.__v) == 0b1111;");
            writer.WriteLine();
        }
        else
        {
            // any(boolN)
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static bool any({vec.Name} x) =>");
            writer.WriteLine($"    ({string.Join(" | ", comps.Select(c => $"x.{c}"))}) != 0;");
            writer.WriteLine();

            // all(boolN)
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static bool all({vec.Name} x) =>");
            writer.WriteLine($"    ({string.Join(" & ", comps.Select(c => $"x.{c}"))}) != 0;");
            writer.WriteLine();
        }
    }

    private static void EmitBasicMathFunctions(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var math = vec.Element.MathClass;

        // min
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} min({vec.Name} x, {vec.Name} y) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Min(x.{c}, y.{c})"))});");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} min({vec.Name} x, {vec.Element.LogicalTypeName} y) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Min(x.{c}, y)"))});");
        writer.WriteLine();

        // max
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} max({vec.Name} x, {vec.Name} y) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Max(x.{c}, y.{c})"))});");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} max({vec.Name} x, {vec.Element.LogicalTypeName} y) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Max(x.{c}, y)"))});");
        writer.WriteLine();

        // clamp
        var clampMethod = vec.Element.Kind == ScalarKind.Half ? "global::System.Half.Clamp" : "global::System.Math.Clamp";
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} clamp({vec.Name} x, {vec.Name} min, {vec.Name} max) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{clampMethod}(x.{c}, min.{c}, max.{c})"))});");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} clamp({vec.Name} x, {vec.Element.LogicalTypeName} min, {vec.Element.LogicalTypeName} max) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{clampMethod}(x.{c}, min, max)"))});");
        writer.WriteLine();

        // abs
        if (vec.Element.IsSigned)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} abs({vec.Name} x) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Abs(x.{c})"))});");
            writer.WriteLine();

            // sign
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} sign({vec.Name} x) =>");
            string SignExpr(string c) => vec.Element.Kind switch
            {
                ScalarKind.Half => $"(global::System.Half)(x.{c} > (global::System.Half)0.0f ? 1.0f : (x.{c} < (global::System.Half)0.0f ? -1.0f : 0.0f))",
                _ => $"({vec.Element.LogicalTypeName}){math}.Sign(x.{c})"
            };
            writer.WriteLine($"    new({string.Join(", ", comps.Select(SignExpr))});");
            writer.WriteLine();
        }
    }

    private static void EmitTrigFunctions(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var math = vec.Element.MathClass;
        string[] trigOps = ["Sin", "Cos", "Tan", "Asin", "Acos", "Atan"];

        foreach (var op in trigOps)
        {
            var methodName = op.ToLowerInvariant();
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} {methodName}({vec.Name} x) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.{op}(x.{c})"))});");
            writer.WriteLine();
        }

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} atan2({vec.Name} y, {vec.Name} x) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Atan2(y.{c}, x.{c})"))});");
        writer.WriteLine();
    }

    private static void EmitExponentialFunctions(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var math = vec.Element.MathClass;

        // sqrt
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} sqrt({vec.Name} x) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Sqrt(x.{c})"))});");
        writer.WriteLine();

        // rsqrt
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} rsqrt({vec.Name} x) =>");
        writer.WriteLine($"    {vec.Element.OneLiteral} / sqrt(x);");
        writer.WriteLine();

        // exp, log
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} exp({vec.Name} x) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Exp(x.{c})"))});");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} log({vec.Name} x) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Log(x.{c})"))});");
        writer.WriteLine();

        // pow
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} pow({vec.Name} x, {vec.Name} y) =>");
        writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.Pow(x.{c}, y.{c})"))});");
        writer.WriteLine();
    }

    private static void EmitRoundingAndInterpolation(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        var math = vec.Element.MathClass;

        // floor, ceil, round, trunc
        string[] roundOps = ["Floor", "Ceiling", "Round", "Truncate"];
        string[] methodNames = ["floor", "ceil", "round", "trunc"];

        for (var i = 0; i < roundOps.Length; i++)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} {methodNames[i]}({vec.Name} x) =>");
            writer.WriteLine($"    new({string.Join(", ", comps.Select(c => $"{math}.{roundOps[i]}(x.{c})"))});");
            writer.WriteLine();
        }

        // frac
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} frac({vec.Name} x) => x - floor(x);");
        writer.WriteLine();

        // saturate
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} saturate({vec.Name} x) => clamp(x, {vec.Element.ZeroLiteral}, {vec.Element.OneLiteral});");
        writer.WriteLine();

        // lerp
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} lerp({vec.Name} x, {vec.Name} y, {vec.Name} s) => x + s * (y - x);");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} lerp({vec.Name} x, {vec.Name} y, {vec.Element.LogicalTypeName} s) => x + s * (y - x);");
        writer.WriteLine();

        // mad
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Name} mad({vec.Name} a, {vec.Name} b, {vec.Name} c) => a * b + c;");
        writer.WriteLine();
    }

    private static void EmitLinearAlgebra(CodeWriter writer, VectorType vec, IReadOnlyList<string> comps)
    {
        // cmin, cmax, csum
        var math = vec.Element.MathClass;
        string cminExpr = $"x.{comps[0]}";
        string cmaxExpr = $"x.{comps[0]}";
        string csumExpr = $"x.{comps[0]}";

        for (var i = 1; i < comps.Count; i++)
        {
            cminExpr = $"{math}.Min({cminExpr}, x.{comps[i]})";
            cmaxExpr = $"{math}.Max({cmaxExpr}, x.{comps[i]})";
            csumExpr = $"{csumExpr} + x.{comps[i]}";
        }

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Element.LogicalTypeName} cmin({vec.Name} x) => {cminExpr};");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Element.LogicalTypeName} cmax({vec.Name} x) => {cmaxExpr};");
        writer.WriteLine();

        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Element.LogicalTypeName} csum({vec.Name} x) => {csumExpr};");
        writer.WriteLine();

        // dot
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Element.LogicalTypeName} dot({vec.Name} x, {vec.Name} y) =>");
        writer.WriteLine($"    {string.Join(" + ", comps.Select(c => $"x.{c} * y.{c}"))};");
        writer.WriteLine();

        // lengthsq, length, distance, distancesq, normalize
        if (vec.Element.IsFloatingPoint)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Element.LogicalTypeName} lengthsq({vec.Name} x) => dot(x, x);");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Element.LogicalTypeName} length({vec.Name} x) => {math}.Sqrt(dot(x, x));");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Element.LogicalTypeName} distancesq({vec.Name} x, {vec.Name} y) => lengthsq(y - x);");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Element.LogicalTypeName} distance({vec.Name} x, {vec.Name} y) => length(y - x);");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} normalize({vec.Name} x) => x * ({vec.Element.OneLiteral} / {math}.Sqrt(dot(x, x)));");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} reflect({vec.Name} i, {vec.Name} n) => i - ({vec.Element.FormatLiteral(2.0)} * n * dot(i, n));");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} project({vec.Name} v, {vec.Name} onNormal) => onNormal * (dot(v, onNormal) / dot(onNormal, onNormal));");
            writer.WriteLine();
        }

        // 3D vector specific: cross
        if (vec.Dimension == 3)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} cross({vec.Name} x, {vec.Name} y) =>");
            writer.WriteLine($"    new(x.y * y.z - x.z * y.y, x.z * y.x - x.x * y.z, x.x * y.y - x.y * y.x);");
            writer.WriteLine();
        }
    }

    private static void EmitShuffleMethods(CodeWriter writer, VectorType vec)
    {
        var comps = vec.Components;

        // Scalar shuffle
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {vec.Element.LogicalTypeName} shuffle({vec.Name} left, {vec.Name} right, ShuffleComponent x) =>");
        writer.WriteLine($"    select_shuffle_component(left, right, x);");
        writer.WriteLine();

        // Vector shuffles of length 2..vec.Dimension
        for (var dim = 2; dim <= vec.Dimension; dim++)
        {
            var targetType = $"{vec.Element.Name}{dim}";
            var shuffleParams = string.Join(", ", s_compPool.Take(dim).Select(c => $"ShuffleComponent {c}"));
            var shuffleArgs = string.Join(", ", s_compPool.Take(dim).Select(c => $"select_shuffle_component(left, right, {c})"));

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {targetType} shuffle({vec.Name} left, {vec.Name} right, {shuffleParams}) =>");
            writer.WriteLine($"    new({shuffleArgs});");
            writer.WriteLine();
        }

        // Helper: select_shuffle_component
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"internal static {vec.Element.LogicalTypeName} select_shuffle_component({vec.Name} a, {vec.Name} b, ShuffleComponent component) =>");
        using (writer.Scope("component switch"))
        {
            for (var i = 0; i < comps.Count; i++)
            {
                var upper = comps[i].ToUpperInvariant();
                var leftVal = vec.Element.EmitUnpackToLogical($"a.{comps[i]}");
                var rightVal = vec.Element.EmitUnpackToLogical($"b.{comps[i]}");
                writer.WriteLine($"ShuffleComponent.Left{upper} => {leftVal},");
                writer.WriteLine($"ShuffleComponent.Right{upper} => {rightVal},");
            }
            writer.WriteLine("_ => throw new global::System.ArgumentException(\"Invalid shuffle component: \" + component)");
        }
        writer.WriteLine(";");
        writer.WriteLine();
    }

    private static void EmitSubvectorFactories(CodeWriter writer, VectorType vec)
    {
        if (vec.Dimension < 2) return;

        var partitions = VectorConstructorEmitter.GetPartitions(vec.Dimension);
        var seenSignatures = new HashSet<string>();

        foreach (var partition in partitions)
        {
            if (partition.All(p => p == 1) || partition.Count == 1) continue;

            var paramList = new List<string>();
            var paramNames = new List<string>();

            var offset = 0;
            for (var i = 0; i < partition.Count; i++)
            {
                var size = partition[i];
                var paramComps = string.Concat(vec.Components.Skip(offset).Take(size));
                var paramName = paramComps;
                paramNames.Add(paramName);

                if (size == 1)
                {
                    paramList.Add($"{vec.Element.LogicalTypeName} {paramName}");
                }
                else
                {
                    var subVecName = $"{vec.Element.Name}{size}";
                    paramList.Add($"{subVecName} {paramName}");
                }
                offset += size;
            }

            var signature = string.Join(", ", paramList);
            if (!seenSignatures.Add(signature)) continue;

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {vec.Name} {vec.Name}({signature}) => new({string.Join(", ", paramNames)});");
            writer.WriteLine();
        }
    }
}
