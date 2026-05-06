using Misaki.HighPerformance.HPC.Generator.IR;
using System.Linq;

namespace Misaki.HighPerformance.HPC.Generator.Optimization
{
    /// <summary>
    /// Detects <c>(a * b) + c</c> and <c>c + (a * b)</c> binary expression patterns
    /// in the IR and replaces them with <see cref="HPCIntrinsic.MultiplyAdd"/> calls,
    /// enabling the backend to emit a single FMA instruction instead of two.
    ///
    /// <para>This pass only runs when the target ISA supports FMA (e.g. AVX2 which
    /// includes FMA3 via the FMA extension, and AVX-512F). The pipeline checks
    /// <see cref="HPCMethodIR.TargetISA"/> before adding the pass.</para>
    /// </summary>
    internal sealed class FMAFusionPass : HPCNodeRewriter, IHPCOptimizationPass
    {
        public string Name => "FMA Fusion";

        public HPCMethodIR Transform(HPCMethodIR method)
        {
            var newBody = RewriteBody(method.Body);
            return ReferenceEquals(newBody, method.Body)
                ? method
                : method with { Body = newBody };
        }

        protected override HPCExpr RewriteBinaryOp(HPCBinaryOp node)
        {
            if (node.Kind == HPCBinaryKind.Add)
            {
                // (a * b) + c  →  FMA(a, b, c)
                if (node.Left is HPCBinaryOp { Kind: HPCBinaryKind.Multiply } mulLeft)
                {
                    return new HPCIntrinsicCall(
                        HPCIntrinsic.MultiplyAdd,
                        [RewriteExpr(mulLeft.Left), RewriteExpr(mulLeft.Right), RewriteExpr(node.Right)],
                        node.Type);
                }

                // c + (a * b)  →  FMA(a, b, c)
                if (node.Right is HPCBinaryOp { Kind: HPCBinaryKind.Multiply } mulRight)
                {
                    return new HPCIntrinsicCall(
                        HPCIntrinsic.MultiplyAdd,
                        [RewriteExpr(mulRight.Left), RewriteExpr(mulRight.Right), RewriteExpr(node.Left)],
                        node.Type);
                }
            }

            if (node.Kind == HPCBinaryKind.Subtract)
            {
                // (a * b) - c  →  FMA(a, b, -c) ... or MultiplySubtract if available
                if (node.Left is HPCBinaryOp { Kind: HPCBinaryKind.Multiply } mulLeft)
                {
                    return new HPCIntrinsicCall(
                        HPCIntrinsic.MultiplySubtract,
                        [RewriteExpr(mulLeft.Left), RewriteExpr(mulLeft.Right), RewriteExpr(node.Right)],
                        node.Type);
                }
            }

            return base.RewriteBinaryOp(node);
        }
    }
}
