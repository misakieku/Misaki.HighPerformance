using Misaki.HighPerformance.HPC.Generator.IR;

namespace Misaki.HighPerformance.HPC.Generator.Optimization
{
    /// <summary>
    /// A single optimization pass that transforms an <see cref="HPCMethodIR"/>
    /// into another <see cref="HPCMethodIR"/>. Passes must be pure functions —
    /// they must not mutate the input and should return the original object
    /// unchanged when nothing was modified (enabling cheap "no-change" detection
    /// in the pipeline).
    /// </summary>
    internal interface IHPCOptimizationPass
    {
        /// <summary>Human-readable name for diagnostics.</summary>
        string Name { get; }

        /// <summary>Transforms the IR. Returns the same object if unchanged.</summary>
        HPCMethodIR Transform(HPCMethodIR method);
    }
}
