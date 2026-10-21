using Misaki.Highperformance.TypeGenerator.Pipeline;
using Misaki.Highperformance.TypeGenerator.Tools;

if (args.Length > 0 && args[0] == "--clean-math" && args.Length > 1)
{
    var mathFilePath = args[1];
    Console.WriteLine($"[CleanMath] Analyzing: {mathFilePath}");

    if (!File.Exists(mathFilePath))
    {
        Console.Error.WriteLine($"Error: File not found at {mathFilePath}");
        return 1;
    }

    var original = File.ReadAllText(mathFilePath);
    var (total, stripped, kept, cleaned) = MathCleaner.CleanMathFile(original);

    Console.WriteLine($"[CleanMath] Total methods analyzed: {total}");
    Console.WriteLine($"[CleanMath] Generated vector/matrix methods stripped: {stripped}");
    Console.WriteLine($"[CleanMath] Core scalar/utility methods kept: {kept}");

    var backupPath = mathFilePath + ".bak";
    File.Copy(mathFilePath, backupPath, overwrite: true);
    Console.WriteLine($"[CleanMath] Backup written to: {backupPath}");

    File.WriteAllText(mathFilePath, cleaned);
    Console.WriteLine($"[CleanMath] Cleaned math file saved: {mathFilePath}");
    return 0;
}

if (args.Length > 0 && args[0] == "--dry-clean-math" && args.Length > 1)
{
    var mathFilePath = args[1];
    Console.WriteLine($"[DryRun] Analyzing: {mathFilePath}");

    if (!File.Exists(mathFilePath))
    {
        Console.Error.WriteLine($"Error: File not found at {mathFilePath}");
        return 1;
    }

    var original = File.ReadAllText(mathFilePath);
    var (total, stripped, kept, _) = MathCleaner.CleanMathFile(original);

    Console.WriteLine($"[DryRun] Total methods analyzed: {total}");
    Console.WriteLine($"[DryRun] Generated vector/matrix methods stripped: {stripped}");
    Console.WriteLine($"[DryRun] Core scalar/utility methods kept: {kept}");
    return 0;
}

var emitMath = !args.Contains("--types-only");
var outputIdx = Array.IndexOf(args, "--output");

if (outputIdx >= 0 && outputIdx + 1 < args.Length)
{
    var targetDir = args[outputIdx + 1];
    Console.WriteLine($"Generating math types (emitMath={emitMath}) to: {targetDir}");
    var count = GeneratorPipeline.GenerateToDirectory(targetDir, emitMath);
    Console.WriteLine($"Successfully generated {count} files.");
    return 0;
}

Console.WriteLine("Running math type generator in verification mode...");
var generatedFiles = GeneratorPipeline.GenerateAll(emitMath).ToList();
Console.WriteLine($"Total files generated: {generatedFiles.Count}");

var half4 = generatedFiles.FirstOrDefault(f => f.FileName == "half4.g.cs");
if (half4 != default)
{
    Console.WriteLine($"[VERIFY] half4.g.cs length: {half4.Content.Length} chars");
}

var bool4 = generatedFiles.FirstOrDefault(f => f.FileName == "bool4.g.cs");
if (bool4 != default)
{
    Console.WriteLine($"[VERIFY] bool4.g.cs length: {bool4.Content.Length} chars");
}

var float4x4 = generatedFiles.FirstOrDefault(f => f.FileName == "float4x4.g.cs");
if (float4x4 != default)
{
    Console.WriteLine($"[VERIFY] float4x4.g.cs length: {float4x4.Content.Length} chars");
}

Console.WriteLine("Generator verification completed successfully.");
return 0;
