# Misaki.HighPerformance

A collection of high-performance C# libraries focused on low allocation, low overhead, and systems-level control.

The solution is organized around a few core goals:

- fast collections and reusable memory primitives
- unsafe and low-level building blocks when performance matters
- SIMD/SPMD-friendly mathematics
- a zero-allocation job system
- STB-based image loading and translation helpers
- analyzers and code fixes that help avoid accidental performance regressions

Most runtime projects target **.NET 10** and enable **unsafe** code where needed. Analyzer packages target **.NET Standard 2.0**.

## Packages

Each major package has its own README for project-specific guidance.

| Project | Purpose | Package |
| --- | --- | --- |
| [`Misaki.HighPerformance`](Misaki.HighPerformance/README.md) | Core collection utilities, pools, slot maps, sparse sets, and shared helpers. | [![Nuget Version](https://img.shields.io/nuget/v/Misaki.HighPerformance)](https://www.nuget.org/packages/Misaki.HighPerformance)
| [`Misaki.HighPerformance.LowLevel`](Misaki.HighPerformance.LowLevel/README.md) | Unsafe collections, allocators, arenas, fixed strings, memory utilities, and other low-level primitives. | [![Nuget Version](https://img.shields.io/nuget/v/Misaki.HighPerformance.LowLevel)](https://www.nuget.org/packages/Misaki.HighPerformance.LowLevel)
| [`Misaki.HighPerformance.Mathematics`](Misaki.HighPerformance.Mathematics/README.md) | Math helpers, geometry types, SIMD-friendly helpers, numeric constants, and vector operations. | [![Nuget Version](https://img.shields.io/nuget/v/Misaki.HighPerformance.Mathematics)](https://www.nuget.org/packages/Misaki.HighPerformance.Mathematics)
| [`Misaki.HighPerformance.Mathematics.SPMD`](Misaki.HighPerformance.Mathematics.SPMD/README.md) | SPMD-oriented math helpers, lane abstractions, and vector template infrastructure. | [![Nuget Version](https://img.shields.io/nuget/v/Misaki.HighPerformance.Mathematics.SPMD)](https://www.nuget.org/packages/Misaki.HighPerformance.Mathematics.SPMD)
| [`Misaki.HighPerformance.Jobs`](Misaki.HighPerformance.Jobs/README.md) | Job scheduling, worker threads, handles, dependency management, and temporary job allocation. | [![Nuget Version](https://img.shields.io/nuget/v/Misaki.HighPerformance.Jobs)](https://www.nuget.org/packages/Misaki.HighPerformance.Jobs)

## Highlights

### Low-level collections and memory management

The low-level layer includes building blocks such as:

- `UnsafeArray<T>`
- `UnsafeList<T>`
- `UnsafeQueue<T>`
- `UnsafeStack<T>`
- `UnsafeHashMap<TKey, TValue>`
- `UnsafeHashSet<T>`
- `UnsafeSparseSet<T>`
- `UnsafeSlotMap<T>`
- `MemoryPool`
- `VirtualArena`
- `DynamicArena`
- `AllocationManager`
- `FreeList`
- `FixedString` and `FixedText`

These types are designed for scenarios where allocation patterns, data locality, and explicit lifetime management matter.

### Mathematics for performance-sensitive code

The mathematics layer provides:

- `math` constants and helpers
- custom numeric types and vector extensions
- geometry primitives such as `AABB`, `OBB`, `Plane`, and `SphereBounds`
- SIMD-friendly operations through `System.Runtime.Intrinsics`

### Job system

The job system is designed for work scheduling with minimal overhead and dependency tracking.
It supports:

- single jobs
- parallel-for style jobs
- parallel jobs with batching
- job dependencies
- worker threads
- temporary frame-based allocation

### Image loading

The image package wraps STB-based decoding workflows and includes support for image metadata, animated frames, and runtime statistics.

## Getting started

### Install the packages

```bash
dotnet add package Misaki.HighPerformance
dotnet add package Misaki.HighPerformance.LowLevel
dotnet add package Misaki.HighPerformance.Mathematics
dotnet add package Misaki.HighPerformance.Jobs
dotnet add package Misaki.HighPerformance.Image
```

### Reference only what you need

The solution is split into smaller packages so you can bring in only the pieces required by your application.
For example:

- use `Misaki.HighPerformance` for lightweight collection utilities
- add `Misaki.HighPerformance.LowLevel` when you need unsafe memory primitives
- use `Misaki.HighPerformance.Mathematics` for fast numeric and geometry work
- use `Misaki.HighPerformance.Jobs` for background work scheduling
- use `Misaki.HighPerformance.Image` for image decoding

## Quick examples

### Dynamic array

```csharp
using Misaki.HighPerformance.Collections;

var values = new DynamicArray<int>();
values.Add(10);
values.Add(20);

Span<int> span = values.AsSpan();
```

### Math helpers

```csharp
using Misaki.HighPerformance.Mathematics;

float angle = math.radians(90f);
float tau = math.TAU;
```

### Low-level memory usage

The low-level project is intended for advanced scenarios where explicit allocation and lifetime control are required.
Prefer the safer higher-level projects when they already meet your needs.

## Building the solution

Open the solution in Visual Studio or build from the command line:

```bash
dotnet build
```

Some projects use unsafe code and source generation. Make sure you are building with a compatible .NET SDK and that your environment allows those features.

## Design goals

- keep allocations under control
- favor cache-friendly data structures
- expose low-level primitives without unnecessary abstraction
- support high-performance math and job execution paths
- provide analyzers to help keep code fast

## Repository notes

- Most runtime projects target `net10.0`
- Analyzer projects target `netstandard2.0`
- Several projects enable packaging on build
- Unsafe code is used intentionally in the low-level, math, job, and image layers

## License

See the repository license if one is provided.
