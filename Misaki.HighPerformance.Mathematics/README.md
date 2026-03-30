# Misaki.HighPerformance.Mathematics

Math helpers, geometry primitives, numeric types, and SIMD-friendly utilities for performance-sensitive C# code.

This package focuses on fast scalar and vector math while keeping the API surface practical for game, simulation, rendering, and systems work.

## What it includes

- common math constants and helpers
- custom numeric types
- quaternion and random helpers
- geometry primitives
- SIMD-oriented vector utilities
- generated vector extensions and codegen-backed math support

## Highlights

- constants exposed for float and double workflows
- `System.Runtime.Intrinsics`-based helper code
- geometry types for bounds and spatial calculations
- designed to support vectorized and low-overhead numerical code

## Main types

- `math`
- `quaternion`
- `random`
- `svd`
- `float`
- `double`
- `int`
- `uint`
- `bool`
- `AABB`
- `OBB`
- `Plane`
- `SphereBounds`

## Example

```csharp
using Misaki.HighPerformance.Mathematics;

float radians = math.radians(90f);
float degrees = math.degrees(radians);
float tau = math.TAU;

float3 a = new float3(1f, 2f, 3f);
float3 b = new float3(4f, 5f, 6f);
float3 c = math.cross(a, b);
```

## Package reference

```bash
dotnet add package Misaki.HighPerformance.Mathematics
```

## Notes

This project targets `net10.0`, enables unsafe code, and uses generated source for parts of its vector API surface.
