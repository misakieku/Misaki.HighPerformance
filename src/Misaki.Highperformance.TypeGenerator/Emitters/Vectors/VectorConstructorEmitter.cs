using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Vectors;

public static class VectorConstructorEmitter
{
    public static void EmitConstructors(CodeWriter writer, VectorType vec)
    {
        using (writer.Region("Constructors & Conversions"))
        {
            var comps = vec.Components;

            // 1. Component-wise constructor
            var compParams = string.Join(", ", comps.Select(c => $"{vec.Element.LogicalTypeName} {c}"));
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {vec.Name}({compParams})");
            using (writer.Scope(""))
            {
                foreach (var c in comps)
                {
                    writer.WriteLine($"this.{c} = {vec.Element.EmitPackFromLogical(c)};");
                }
            }
            writer.WriteLine();

            // 1b. Internal storage constructor (for raw masks in bool vectors)
            if (vec.Element.IsBool)
            {
                var maskParams = string.Join(", ", comps.Select(c => $"{vec.Element.StorageTypeName} {c}"));
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"internal {vec.Name}({maskParams})");
                using (writer.Scope(""))
                {
                    foreach (var c in comps)
                    {
                        writer.WriteLine($"this.{c} = {c};");
                    }
                }
                writer.WriteLine();
            }

            // 2. Splat constructor
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {vec.Name}({vec.Element.LogicalTypeName} value)");
            using (writer.Scope(""))
            {
                var packed = vec.Element.EmitPackFromLogical("value");
                foreach (var c in comps)
                {
                    writer.WriteLine($"this.{c} = {packed};");
                }
            }
            writer.WriteLine();

            // 3. Sub-vector partition constructors
            EmitSubvectorConstructors(writer, vec);

            // 4. Span constructor
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public unsafe {vec.Name}(global::System.ReadOnlySpan<{vec.Element.StorageTypeName}> values)");
            using (writer.Scope(""))
            {
                writer.WriteLine($"if (values.Length < {vec.Dimension})");
                writer.WriteLine($"    throw new global::System.ArgumentOutOfRangeException(nameof(values), \"Expected at least {vec.Dimension} elements.\");");
                writer.WriteLine($"fixed ({vec.Element.StorageTypeName}* ptr = values)");
                writer.WriteLine("{");
                writer.WriteLine($"    this = *(({vec.Name}*)ptr);");
                writer.WriteLine("}");
            }
            writer.WriteLine();

            // 5. Implicit scalar-to-vector conversion
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static implicit operator {vec.Name}({vec.Element.LogicalTypeName} value) => new(value);");
            writer.WriteLine();

            // 6. Conversions between other vectors of the same dimension
            EmitCrossVectorConversions(writer, vec);
        }
    }

    private static void EmitSubvectorConstructors(CodeWriter writer, VectorType vec)
    {
        if (vec.Dimension < 2) return;

        var partitions = GetPartitions(vec.Dimension);
        var seenSignatures = new HashSet<string>();

        foreach (var partition in partitions)
        {
            // Only care about multi-component partitions that have at least one vector parameter
            if (partition.All(p => p == 1) || partition.Count == 1) continue;

            var paramList = new List<string>();
            var assignments = new List<(string comp, string valueExpr)>();

            var offset = 0;
            for (var i = 0; i < partition.Count; i++)
            {
                var size = partition[i];
                var paramComps = string.Concat(vec.Components.Skip(offset).Take(size));
                var paramName = paramComps;

                if (size == 1)
                {
                    paramList.Add($"{vec.Element.LogicalTypeName} {paramName}");
                    assignments.Add((vec.Components[offset], vec.Element.EmitPackFromLogical(paramName)));
                }
                else
                {
                    var subVecName = $"{vec.Element.Name}{size}";
                    paramList.Add($"{subVecName} {paramName}");
                    for (var j = 0; j < size; j++)
                    {
                        var srcComp = vec.Components[j];
                        var dstComp = vec.Components[offset + j];
                        assignments.Add((dstComp, $"{paramName}.{srcComp}"));
                    }
                }
                offset += size;
            }

            var signature = string.Join(", ", paramList);
            if (!seenSignatures.Add(signature)) continue;

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {vec.Name}({signature})");
            using (writer.Scope(""))
            {
                foreach (var (comp, valueExpr) in assignments)
                {
                    writer.WriteLine($"this.{comp} = {valueExpr};");
                }
            }
            writer.WriteLine();
        }
    }

    private static void EmitCrossVectorConversions(CodeWriter writer, VectorType vec)
    {
        var otherScalars = TypeRegistry.AllScalars.Where(s => s.Kind != vec.Element.Kind);

        foreach (var other in otherScalars)
        {
            var otherVecName = $"{other.Name}{vec.Dimension}";

            string Expr(string comp)
            {
                if (vec.Element.IsBool)
                {
                    // Numeric -> Bool
                    return $"v.{comp} != {other.ZeroLiteral}";
                }
                if (other.IsBool)
                {
                    // Bool -> Numeric
                    return $"v.{comp} != 0 ? {vec.Element.OneLiteral} : {vec.Element.ZeroLiteral}";
                }
                // Numeric -> Numeric
                return $"({vec.Element.LogicalTypeName})v.{comp}";
            }

            var args = string.Join(", ", vec.Components.Select(Expr));

            // Cross-vector constructor
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {vec.Name}({otherVecName} v) : this({args}) {{ }}");
            writer.WriteLine();

            // Cross-vector explicit conversion operator
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static explicit operator {vec.Name}({otherVecName} v) => new(v);");
            writer.WriteLine();

            // Cross-scalar splat constructor
            string ScalarExpr(string p)
            {
                if (vec.Element.IsBool) return $"{p} != {other.ZeroLiteral}";
                if (other.IsBool) return $"{p} ? {vec.Element.OneLiteral} : {vec.Element.ZeroLiteral}";
                return $"({vec.Element.LogicalTypeName}){p}";
            }

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {vec.Name}({other.LogicalTypeName} v) : this({ScalarExpr("v")}) {{ }}");
            writer.WriteLine();

            // Cross-scalar explicit conversion operator
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static explicit operator {vec.Name}({other.LogicalTypeName} v) => new(v);");
            writer.WriteLine();
        }
    }

    internal static List<List<int>> GetPartitions(int target)
    {
        var result = new List<List<int>>();

        void Recurse(List<int> current, int sum)
        {
            if (sum == target)
            {
                result.Add(new List<int>(current));
                return;
            }

            for (var i = 1; i <= target; i++)
            {
                if (sum + i <= target)
                {
                    current.Add(i);
                    Recurse(current, sum + i);
                    current.RemoveAt(current.Count - 1);
                }
            }
        }

        Recurse(new List<int>(), 0);
        return result;
    }
}
