using Microsoft.CodeAnalysis;
using Misaki.HighPerformance.HPC.Generator.APIContext;

namespace Misaki.HighPerformance.HPC.Generator
{
    /// <summary>
    /// Generates the <c>AVX2Utility</c> static class containing polynomial
    /// approximations for transcendental functions (Sin, Cos, SinCos, etc.)
    /// that have no built-in AVX2 hardware intrinsic.
    ///
    /// <para>These methods are called by the <c>AVX2Backend</c> emitter when it
    /// encounters <see cref="IR.HPCUnaryKind.Sin"/>, <see cref="IR.HPCUnaryKind.Cos"/>,
    /// and similar IR nodes.</para>
    /// </summary>
    [Generator]
    public class AVX2UtilityGenerator : IIncrementalGenerator
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
}
