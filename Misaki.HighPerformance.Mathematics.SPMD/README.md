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

- `ISPMD`
- `ISPMD<TSelf, TNumber>`
- `ScalerLane`
- `WideLane`
- `IJobSPMD`
- `Vector{T}Helper`

## Example

```csharp
// Define an SPMD-friendly numeric lane type and use it to express data-parallel work.
// See the source templates for the current concrete lane implementations.
```

## Package reference

```bash
dotnet add package Misaki.HighPerformance.Mathematics.SPMD
```

## Notes

This project targets `net10.0` and depends on the mathematics project for shared numeric concepts.
