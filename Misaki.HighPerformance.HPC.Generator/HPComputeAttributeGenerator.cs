using Microsoft.CodeAnalysis;
using System;

namespace Misaki.HighPerformance.HPC.Generator
{
    internal enum FloatPrecision
    {
        Standard = 0,
        High = 1,
        Low = 2,
    }

    internal enum MathMode
    {
        Standard = 0,
        Fast = 1,
    }

    [Flags]
    internal enum TargetInstructionSet
    {
        None = 0,
        SSE2 = 1 << 0,
        SSE4 = 1 << 1,
        AVX = 1 << 2,
        AVX2 = 1 << 3,
        AVX512 = 1 << 4,
    }

    [Generator]
    public class HPComputeAttributeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static ctx =>
            {
                var source = @$"
using System;

namespace Misaki.HighPerformance.HPC
{{
    public enum FloatPrecision
    {{
        /// <summary>
        /// Compute with an accuracy of 3.5 ULPs (Units in the Last Place). This is the default precision level for floating-point operations.
        /// </summary>
        Standard = {(int)FloatPrecision.Standard},
        /// <summary>
        /// Compute with an accuracy of 1 ULP. This level may use more aggressive optimizations that can lead to faster computations but with reduced precision.
        /// </summary>
        High = {(int)FloatPrecision.High},
        /// <summary>
        /// Compute with an accuracy that equals or lower than 3.5 ULPs. This level may use the most aggressive optimizations, potentially sacrificing precision for maximum performance.
        /// </summary>
        Low = {(int)FloatPrecision.Low},
    }}

    public enum MathMode
    {{
        /// <summary>
        /// Use the default math mode, which balances performance and accuracy. This mode may allow certain optimizations that can lead to faster computations while maintaining reasonable precision.
        /// </summary>
        Standard = {(int)MathMode.Standard},
        /// <summary>
        /// Use a fast math mode, which prioritizes performance over accuracy. This mode assumes there are no special cases (like NaNs or infinities) and may allow for more aggressive optimizations.
        /// </summary>
        Fast = {(int)MathMode.Fast},
    }}

    [Flags]
    public enum TargetInstructionSet
    {{
        None = {(int)TargetInstructionSet.None},
        /// <summary>
        /// Streaming SIMD Extensions 2.
        /// </summary>
        SSE2 = {(int)TargetInstructionSet.SSE2},
        /// <summary>
        /// Streaming SIMD Extensions 4.2.
        /// </summary>
        SSE4 = {(int)TargetInstructionSet.SSE4},
        /// <summary>
        /// Advanced Vector Extensions.
        /// </summary>
        AVX = {(int)TargetInstructionSet.AVX},
        /// <summary>
        /// Advanced Vector Extensions 2. Includes FMA, F16C and BMI1/2.
        /// </summary>
        AVX2 = {(int)TargetInstructionSet.AVX2},
        /// <summary>
        /// Advanced Vector Extensions 512.
        /// </summary>
        AVX512 = {(int)TargetInstructionSet.AVX512},
    }}

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class HPComputeAttribute : Attribute
    {{
        public HPComputeAttribute(TargetInstructionSet instructionSet, FloatPrecision precision = FloatPrecision.Standard, MathMode mode = MathMode.Standard)
        {{
        }}
    }}
}}";
                ctx.AddSource("HPComputeAttribute.g.cs", source);
            });
        }
    }
}
