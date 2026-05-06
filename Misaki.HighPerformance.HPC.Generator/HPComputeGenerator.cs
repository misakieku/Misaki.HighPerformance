using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Misaki.HighPerformance.HPC.Generator
{
    internal class HPComputeMethodInfo
    {
        public MethodDeclarationSyntax MethodDeclaration
        {
            get; set;
        } = null!;

        public IMethodSymbol MethodSymbol
        {
            get; set;
        } = null!;

        public SemanticModel SemanticModel
        {
            get; set;
        } = null!;

        public TargetInstructionSet InstructionSet
        {
            get; set;
        }

        public FloatPrecision Precision
        {
            get; set;
        }

        public MathMode Mode
        {
            get; set;
        }
    }

    [Generator]
    public class HPComputeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var methodDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Misaki.HighPerformance.HPC.HPComputeAttribute",
                    static (n, ct) => n is MethodDeclarationSyntax,
                    static (ctx, ct) =>
                    {
                        var attributes = ctx.Attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Misaki.HighPerformance.HPC.HPComputeAttribute");
                        if (attributes != null && ctx.TargetSymbol is IMethodSymbol methodSymbol)
                        {
                            return new HPComputeMethodInfo
                            {
                                MethodDeclaration = (MethodDeclarationSyntax)ctx.TargetNode,
                                MethodSymbol = methodSymbol,
                                SemanticModel = ctx.SemanticModel,
                                InstructionSet = (TargetInstructionSet)attributes.ConstructorArguments[0].Value!,
                                Precision = (FloatPrecision)attributes.ConstructorArguments[1].Value!,
                                Mode = (MathMode)attributes.ConstructorArguments[2].Value!,
                            };
                        }

                        return null;
                    })
                .Collect();

            context.RegisterSourceOutput(methodDeclarations, GenerateHPCMethod);
        }

        private void GenerateHPCMethod(SourceProductionContext context, ImmutableArray<HPComputeMethodInfo?> array)
        {
            if (array.IsEmpty)
            {
                return;
            }

            foreach (var info in array)
            {
                if (info == null)
                {
                    continue;
                }

                var rewriters = HPCRewriter.GetRewriter(info.InstructionSet, info.SemanticModel);

                foreach (var writer in rewriters)
                {
                    var rewrittenMethod = (MethodDeclarationSyntax)writer.Visit(info.MethodDeclaration);
                    var newMethod = rewrittenMethod
                        .WithIdentifier(SyntaxFactory.Identifier($"{info.MethodDeclaration.Identifier.Text}_{writer.Name}"));

                    var source = $@"
using Misaki.HighPerformance.HPC;
{writer.GetNesessaryUsing()}

namespace {info.MethodSymbol.ContainingNamespace.ToDisplayString()}
{{
    partial class {info.MethodSymbol.ContainingType.Name}
    {{
{newMethod.NormalizeWhitespace().ToFullString()}
    }}
}}";
                    context.AddSource($"{info.MethodSymbol.ContainingType.Name}_{info.MethodDeclaration.Identifier.Text}_{writer.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }
    }
}
