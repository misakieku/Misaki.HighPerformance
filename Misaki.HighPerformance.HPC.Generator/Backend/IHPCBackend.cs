using Misaki.HighPerformance.HPC.Generator.IR;

namespace Misaki.HighPerformance.HPC.Generator.Backend
{
    /// <summary>
    /// Emits C# source code for one target instruction-set architecture from a
    /// fully analysed and optimised <see cref="HPCMethodIR"/>.
    /// </summary>
    internal interface IHPCBackend
    {
        /// <summary>Short identifier used in generated file/method names (e.g. "AVX2").</summary>
        string Name { get; }

        /// <summary>Using-directives the emitted code requires.</summary>
        string[] RequiredUsings { get; }

        /// <summary>
        /// Emits the full body of a specialised method and returns the C# source
        /// as a string (method signature + braces included).
        /// </summary>
        string EmitMethod(HPCMethodIR method);
    }
}
