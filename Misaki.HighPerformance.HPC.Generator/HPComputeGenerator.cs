using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Misaki.HighPerformance.HPC.Generator.Analysis;
using Misaki.HighPerformance.HPC.Generator.Backend;
using Misaki.HighPerformance.HPC.Generator.Optimization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Misaki.HighPerformance.HPC.Generator
{
    internal class HPComputeMethodInfo
    {
        public MethodDeclarationSyntax MethodDeclaration { get; set; } = null!;
        public IMethodSymbol MethodSymbol { get; set; } = null!;
        public SemanticModel SemanticModel { get; set; } = null!;
        public TargetInstructionSet InstructionSet { get; set; }
        public FloatPrecision Precision { get; set; }
        public MathMode Mode { get; set; }
    }

    [Generator]
    public class HPComputeGenerator : IIncrementalGenerator
    {
        // ── Backends (one singleton per ISA, stateless between methods) ───────

        private static readonly AVX2Backend s_avx2Backend = new();

        // ── Optimization passes ───────────────────────────────────────────────

        /// <summary>
        /// Returns the ordered list of optimisation passes for the given method.
        /// Passes are cheap objects; creating them per-method is intentional so
        /// future stateful passes (e.g. CSE) can be added without concurrency issues.
        /// </summary>
        private static IEnumerable<IHPCOptimizationPass> GetPasses(HPComputeMethodInfo info)
        {
            // FMA fusion only makes sense on ISAs that have it
            if (info.InstructionSet.HasFlag(TargetInstructionSet.AVX2))
                yield return new FMAFusionPass();
        }

        // ── IIncrementalGenerator ─────────────────────────────────────────────

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var methodDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Misaki.HighPerformance.HPC.HPComputeAttribute",
                    static (n, _) => n is MethodDeclarationSyntax,
                    static (ctx, _) =>
                    {
                        var attribute = ctx.Attributes.FirstOrDefault(
                            a => a.AttributeClass?.ToDisplayString() ==
                                 "Misaki.HighPerformance.HPC.HPComputeAttribute");

                        if (attribute is null || ctx.TargetSymbol is not IMethodSymbol methodSymbol)
                            return null;

                        return new HPComputeMethodInfo
                        {
                            MethodDeclaration = (MethodDeclarationSyntax)ctx.TargetNode,
                            MethodSymbol      = methodSymbol,
                            SemanticModel     = ctx.SemanticModel,
                            InstructionSet    = (TargetInstructionSet)attribute.ConstructorArguments[0].Value!,
                            Precision         = (FloatPrecision)attribute.ConstructorArguments[1].Value!,
                            Mode              = (MathMode)attribute.ConstructorArguments[2].Value!,
                        };
                    })
                .Collect();

            context.RegisterSourceOutput(methodDeclarations, GenerateHPCMethods);
        }

        // ── Core pipeline ─────────────────────────────────────────────────────

        private static void GenerateHPCMethods(
            SourceProductionContext context,
            ImmutableArray<HPComputeMethodInfo?> array)
        {
            if (array.IsEmpty) return;

            foreach (var info in array)
            {
                if (info is null) continue;

                try
                {
                    GenerateSingleMethod(context, info);
                }
                catch (Exception ex)
                {
                    // Surface analyzer errors as Roslyn diagnostics so the user
                    // sees them in the IDE rather than a silent empty output.
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "HPC0001",
                            title: "HPC code generation failed",
                            messageFormat: "Failed to generate HPC variant for '{0}': {1}",
                            category: "HPCGenerator",
                            defaultSeverity: DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        info.MethodDeclaration.GetLocation(),
                        info.MethodDeclaration.Identifier.Text,
                        ex.Message));
                }
            }
        }

        private static void GenerateSingleMethod(
            SourceProductionContext context,
            HPComputeMethodInfo info)
        {
            // ── Phase 1: Analyse ──────────────────────────────────────────────
            var analyzer = new HPCAnalyzer(info.SemanticModel);
            var ir = analyzer.Analyze(info.MethodDeclaration, info);

            // ── Phase 2: Optimise ─────────────────────────────────────────────
            var optimizedIR = ir;
            foreach (var pass in GetPasses(info))
                optimizedIR = pass.Transform(optimizedIR);

            // ── Phase 3: Emit per-target ──────────────────────────────────────
            foreach (var (backend, isa) in GetBackends(info.InstructionSet))
            {
                var methodSource = backend.EmitMethod(optimizedIR);
                var fullSource   = WrapSource(methodSource, optimizedIR, backend);

                context.AddSource(
                    hintName: $"{ir.ContainingTypeName}_{ir.OriginalName}_{backend.Name}.g.cs",
                    source:   fullSource);
            }
        }

        // ── Backend selection ─────────────────────────────────────────────────

        private static IEnumerable<(IHPCBackend backend, TargetInstructionSet isa)>
            GetBackends(TargetInstructionSet instructionSet)
        {
            if (instructionSet.HasFlag(TargetInstructionSet.AVX2))
                yield return (s_avx2Backend, TargetInstructionSet.AVX2);

            // Future: SSE4, AVX512, NEON — add here without touching anything else
        }

        // ── Source wrapping ───────────────────────────────────────────────────

        private static string WrapSource(
            string methodBody,
            IR.HPCMethodIR ir,
            IHPCBackend backend)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");

            foreach (var u in backend.RequiredUsings)
                sb.AppendLine(u);

            sb.AppendLine("using Misaki.HighPerformance.HPC;");
            sb.AppendLine();

            sb.AppendLine($"namespace {ir.ContainingNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    partial class {ir.ContainingTypeName}");
            sb.AppendLine("    {");
            sb.AppendLine(methodBody);
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
