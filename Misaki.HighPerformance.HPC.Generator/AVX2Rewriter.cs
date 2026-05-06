using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Misaki.HighPerformance.HPC.Generator.APIContext;
using System;

namespace Misaki.HighPerformance.HPC.Generator
{
    [Generator]
    internal class AVX2UtilityGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static ctx =>
            {
                var api = new Avx2APIContext();

                var sinCosMethods = UtilityTemplate.GenerateSinCosUtilityMethods(api, "        ");

                var source = @$"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Misaki.HighPerformance.HPC
{{
    public static class AVX2Utility
    {{
{sinCosMethods}
    }}
}}";

                ctx.AddSource("AVX2Utility.g.cs", source);
            });
        }
    }

    internal class AVX2Rewriter : HPCRewriter
    {
        public AVX2Rewriter(SemanticModel semanticModel)
            : base(semanticModel)
        {
        }

        public override string Name => "AVX2";

        public override string GetNesessaryUsing()
        {
            return "using System.Runtime.Intrinsics;\nusing System.Runtime.Intrinsics.X86;";
        }

        protected override void RewriteMathArguments(SIMDInstruction instruction, Span<ArgumentSyntax> originalArgs)
        {
            throw new NotImplementedException();
        }

        protected override MathExpression RewriteMathExpression(SIMDInstruction instruction)
        {
            switch (instruction)
            {
                case SIMDInstruction.Add:
                    return new MathExpression
                    {
                        Expression = "Avx2",
                        Name = "Add"
                    };
                case SIMDInstruction.Subtract:
                    return new MathExpression
                    {
                        Expression = "Avx2",
                        Name = "Subtract"
                    };
                case SIMDInstruction.Multiply:
                    return new MathExpression
                    {
                        Expression = "Avx2",
                        Name = "Multiply"
                    };
                case SIMDInstruction.MultiplyAdd:
                    return new MathExpression
                    {
                        Expression = "Fma",
                        Name = "MultiplyAdd"
                    };
                case SIMDInstruction.Asin:
                    return new MathExpression
                    {
                        Expression = "AVX2Utility",
                        Name = "Asin"
                    };
                case SIMDInstruction.Atan2:
                    return new MathExpression
                    {
                        Expression = "AVX2Utility",
                        Name = "Atan2"
                    };
                default:
                    break;
            }

            return default;
        }
    }
}
