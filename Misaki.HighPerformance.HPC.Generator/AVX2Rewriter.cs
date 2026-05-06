using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Misaki.HighPerformance.HPC.Generator.VectorAPI;
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

                var sinFloat_standard = UtilityTemplate.SinFloat_Standard(api);
                var sinFloat_fast = UtilityTemplate.SinFloat_Fast(api);

                var source = @$"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Misaki.HighPerformance.HPC
{{
    public static class AVX2Utility
    {{
        [MethodImpl(MethodImplOptions.NoInlining)]
{sinFloat_standard.GetFullCode("        ")}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
{sinFloat_fast.GetFullCode("        ")}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Asin(Vector256<float> value)
        {{
            // asin(value) = pi/2 - acos(value)

            var piOver2 = Vector256.Create(MathF.PI / 2.0f);
            return Avx2.Subtract(piOver2, Acos(value));
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Acos(Vector256<float> value)
        {{
            // 0 <= value <= 1 : acos(value) = sqrt(1 - value) * (c0 + c1*value + c2*value^2 + c3*value^3)
            // value < 0 : acos(value) = pi - acos(-value)

            var x = Vector256.Abs(value);

            var c0 = Vector256.Create(1.5707288f); // pi/2
            var c1 = Vector256.Create(-0.2121144f);
            var c2 = Vector256.Create(0.0742610f);
            var c3 = Vector256.Create(-0.0187293f);

            var term1 = Fma.MultiplyAdd(x, c3, c2);
            var term2 = Fma.MultiplyAdd(x, term1, c1);
            var poly = Fma.MultiplyAdd(x, term2, c0);

            var sqrtTerm = Avx2.Sqrt(Avx2.Subtract(Vector256<float>.One, x));
            var result = Avx2.Multiply(poly, sqrtTerm);

            var pi = Vector256.Create(MathF.PI);
            var isNegative = Avx2.CompareLessThan(value, Vector256<float>.Zero);

            return Avx2.BlendVariable(pi, Avx2.Subtract(pi, result), isNegative);
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> Atan2(Vector256<float> y, Vector256<float> x)
        {{
            var absX = Vector256.Abs(x);
            var absY = Vector256.Abs(y);

            // 1. Determine the ratio (input to Atan)
            // If |value| > |y|, we are in the ""shallow"" region, ratio = y/value
            // If |y| > |value|, we are in the ""steep"" region, ratio = value/y (and we transform result)
            var yGtX = Avx2.CompareGreaterThan(absY, absX);

            // Select numerator and denominator to ensure ratio is always in [-1, 1]
            var num = Avx2.BlendVariable(absX, absY, yGtX);
            var den = Avx2.BlendVariable(absY, absX, yGtX);

            var t = Avx2.Multiply(num, Avx2.Reciprocal(den)); // t is now in [0, 1]
            var t2 = Avx2.Multiply(t, t);

            // 2. Polynomial Approximation (Odd function: value * (c1 + c2*value^2))
            var c1 = Vector256.Create(0.97239411f);
            var c2 = Vector256.Create(-0.19194795f);

            // (c1 + c2 * t2)
            var poly = Fma.MultiplyAdd(c2, t2, c1);

            // result = Avx2.Multiply(t, poly)
            var result = Avx2.Multiply(t, poly);

            // 3. Reconstruct the angle
            // If we swapped value/y (yGtX), the identity is: atan(value/y) = PI/2 - atan(y/value)
            var halfPi = Vector256.Create(1.570796327f);
            result = Avx2.BlendVariable(halfPi - result, result, yGtX);

            // 4. Adjust for Quadrants (Signs)
            // If value < 0, we are in quadrants 2 or 3, so we need to add PI
            var pi = Vector256.Create(3.141592654f);
            var xLtZero = Avx2.CompareLessThan(x, Vector256<float>.Zero);
            result = Avx2.BlendVariable(pi - result, result, xLtZero);

            // If y < 0, the result should be negative (standard atan2 convention)
            // NOTE: This sign flip strategy depends on exact polynomial range mapping, 
            // but typically just copy the sign of Y to the result.
            var yLtZero = Avx2.CompareLessThan(y, Vector256<float>.Zero);
            // If original Y was negative, negate the result
            // (This works because our ratio logic effectively computed atan(|y|/|value|) above)
            var negativeResult = Avx2.Subtract(Vector256<float>.Zero, result);
            return Avx2.BlendVariable(negativeResult, result, yLtZero);
        }}
    }}
}}";

                ctx.AddSource("AVX2Utility.g.cs", source);
            });
        }
    }

    internal class AVX2Rewriter : HPCRewriter
    {
        public override string Name => "AVX2";

        public override string GetNesessaryUsing()
        {
            return "using System.Runtime.Intrinsics;\nusing System.Runtime.Intrinsics.X86;";
        }

        protected override MathExpression RewriteMathExpression(SIMDInstruction instruction, bool isFloatingPoint)
        {
            switch (instruction)
            {
                case SIMDInstruction.Add:
                    break;
                case SIMDInstruction.Subtract:
                    break;
                case SIMDInstruction.Multiply:
                    break;
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

        protected override void RewriteMathArguments(SIMDInstruction instruction, Span<ArgumentSyntax> originalArgs)
        {
            return;
        }
    }
}
