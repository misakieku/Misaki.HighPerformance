# Misaki.HighPerformance.Mathematics.SPMD

SPMD-oriented math abstractions built on top of the mathematics layer.

This package is intended for code that wants to express vectorized work in a way that is portable across lane widths and easier to reason about than raw intrinsics alone.

## What it includes

- SPMD lane interfaces
- scalar and wide lane abstractions
- vector template helpers
- shuffle table generation support
- job-oriented SPMD helpers

## Highlights

- abstracts lane width through a common interface
- supports sequence creation, load/store, and compress-store style workflows
- built for vectorized algorithms and data-parallel execution
- useful when you need explicit lane semantics rather than ad hoc SIMD code

## Main types

- `ISPMDLane`
- `ISPMDLane<TSelf, TNumber>`
- `ScalerLane`
- `WideLane`
- `IJobSPMD`
- `Vector{T}Helper`

## Example

```csharp
public struct Vector2LerpJob : IJobSPMD<float>
{
    public float2[] arrayA;
    public float2[] arrayB;
    public float[] results;

    public readonly void Execute<TFloat>(TFloat indices, TFloat mask, ref readonly JobExecutionContext ctx)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        TFloat gatherIndices = indices * 2;
        Vector2<TFloat, float> a = MathV.MaskGatherVector2<TFloat, float>(ref arrayA[0].x, gatherIndices, mask, 4);
        Vector2<TFloat, float> b = MathV.MaskGatherVector2<TFloat, float>(ref arrayB[0].x, gatherIndices, mask, 4);

        TFloat t = TFloat.Create(0.5f);
        Vector2<TFloat, float> lerped = MathV.Lerp(a, b, t);
        TFloat len = TFloat.Sqrt(MathV.LengthSquared(lerped));

        len.MaskStore(ref results[(int)indices[0]], mask);
    }
}
```

You can visit `GGXMipGenerationBenchmark.cs` for a more complete example of how to use the SPMD abstractions in a real algorithm.

## Package reference

```bash
dotnet add package Misaki.HighPerformance.Mathematics.SPMD
```

## Notes

This project targets `net10.0` and depends on the mathematics project for shared numeric concepts.

You can enable `MHP_FASTMATH` to allow the use of faster math intrinsics where appropriate, but be aware that this may lead to less precise results in some cases.