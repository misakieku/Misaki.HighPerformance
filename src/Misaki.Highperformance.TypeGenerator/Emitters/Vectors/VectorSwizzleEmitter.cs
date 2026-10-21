using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Vectors;

public static class VectorSwizzleEmitter
{
    private static readonly string[] s_compPool = ["x", "y", "z", "w"];

    public static void EmitSwizzles(CodeWriter writer, VectorType vec)
    {
        using (writer.Region("Swizzle Properties"))
        {
            var validComponents = vec.Components.ToArray();
            var swizzles = GenerateSwizzles(validComponents, 4);

            foreach (var (prop, canSet) in swizzles)
            {
                var targetStruct = $"{vec.Element.Name}{prop.Length}";
                var getArgs = string.Join(", ", prop.Select(c => $"this.{c}"));

                writer.WriteLine("[global::System.Text.Json.Serialization.JsonIgnore]");

                if (canSet)
                {
                    writer.WriteLine($"public {targetStruct} {prop}");
                    using (writer.Scope(""))
                    {
                        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                        writer.WriteLine($"get => new({getArgs});");
                        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);

                        var assignments = string.Join(" ", prop.Select((c, i) => $"this.{c} = value.{s_compPool[i]};"));
                        writer.WriteLine($"set {{ {assignments} }}");
                    }
                }
                else
                {
                    writer.WriteLine($"public readonly {targetStruct} {prop}");
                    using (writer.Scope(""))
                    {
                        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                        writer.WriteLine($"get => new({getArgs});");
                    }
                }

                writer.WriteLine();
            }
        }
    }

    private static IEnumerable<(string property, bool canSet)> GenerateSwizzles(string[] pool, int maxLen)
    {
        IEnumerable<(string property, bool canSet)> Recurse(string prefix, int depth)
        {
            if (depth == 0)
            {
                yield return (prefix, prefix.Distinct().Count() == prefix.Length);
            }
            else
            {
                foreach (var c in pool)
                {
                    foreach (var s in Recurse(prefix + c, depth - 1))
                    {
                        yield return s;
                    }
                }
            }
        }

        return Enumerable.Range(2, maxLen - 1).SelectMany(len => Recurse(string.Empty, len));
    }
}
