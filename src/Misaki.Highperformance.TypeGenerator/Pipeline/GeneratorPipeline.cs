using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Pipeline;

public static class GeneratorPipeline
{
    public static IEnumerable<(string FileName, string Content)> GenerateAll(bool emitMath = true)
    {
        // 1. Generate Vectors
        foreach (var vec in TypeRegistry.AllVectors)
        {
            var code = VectorGenerator.Generate(vec, emitMath);
            yield return ($"{vec.Name}.g.cs", code);
        }

        // 2. Generate Matrices
        foreach (var mat in TypeRegistry.AllMatrices)
        {
            var code = MatrixGenerator.Generate(mat, emitMath);
            yield return ($"{mat.Name}.g.cs", code);
        }
    }

    public static int GenerateToDirectory(string outputDirectory, bool emitMath = true)
    {
        Directory.CreateDirectory(outputDirectory);
        var count = 0;

        foreach (var (fileName, content) in GenerateAll(emitMath))
        {
            var filePath = Path.Combine(outputDirectory, fileName);
            File.WriteAllText(filePath, content);
            count++;
        }

        return count;
    }
}
