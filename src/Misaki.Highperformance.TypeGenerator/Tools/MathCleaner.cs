using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Tools;

public static class MathCleaner
{
    private static readonly HashSet<string> s_generatedTypeNames = new(
        TypeRegistry.AllVectors.Select(v => v.Name)
            .Concat(TypeRegistry.AllMatrices.Select(m => m.Name))
    );

    private static readonly HashSet<string> s_generatedMethodNames =
    [
        "min", "max", "clamp", "abs", "sign",
        "sin", "cos", "tan", "asin", "acos", "atan", "atan2",
        "sinh", "cosh", "tanh",
        "sqrt", "rsqrt", "exp", "log", "pow",
        "floor", "ceil", "round", "trunc", "frac", "saturate", "lerp", "mad",
        "dot", "cross", "length", "lengthsq", "distance", "distancesq", "normalize", "reflect", "project",
        "mul", "transpose", "determinant", "inverse", "fastinverse",
        "cmin", "cmax", "csum", "any", "all",
        "shuffle", "select_shuffle_component"
    ];

    public static (int totalMethods, int strippedMethods, int keptMethods, string cleanedSource) CleanMathFile(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        var mathClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == "math");

        if (mathClass == null)
        {
            throw new InvalidOperationException("Could not find class 'math' in source.");
        }

        var totalMethods = 0;
        var strippedMethods = 0;
        var keptMethods = 0;

        var preservedMembers = new List<MemberDeclarationSyntax>();

        foreach (var member in mathClass.Members)
        {
            if (member is not MethodDeclarationSyntax method)
            {
                preservedMembers.Add(member);
                continue;
            }

            totalMethods++;
            var methodName = method.Identifier.Text;
            var paramTypes = method.ParameterList.Parameters
                .Select(p => p.Type?.ToString() ?? "")
                .ToList();
            var returnType = method.ReturnType.ToString();

            var touchesGeneratedType = paramTypes.Any(t => s_generatedTypeNames.Contains(t))
                                      || s_generatedTypeNames.Contains(returnType);

            var isGeneratedMethod = s_generatedMethodNames.Contains(methodName);

            // Also check for type constructor factories like `float4(...)`, `half2(...)`, etc.
            var isFactoryMethod = s_generatedTypeNames.Contains(methodName);

            if ((touchesGeneratedType && isGeneratedMethod) || isFactoryMethod)
            {
                strippedMethods++;
            }
            else
            {
                keptMethods++;
                preservedMembers.Add(member);
            }
        }

        var newMathClass = mathClass.WithMembers(SyntaxFactory.List(preservedMembers));
        var newRoot = root.ReplaceNode(mathClass, newMathClass);

        return (totalMethods, strippedMethods, keptMethods, newRoot.ToFullString());
    }
}
