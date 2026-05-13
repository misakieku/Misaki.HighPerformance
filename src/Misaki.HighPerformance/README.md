# Misaki.HighPerformance

Core collection utilities and shared helpers for high-performance C# code.

This package provides lightweight, allocation-conscious building blocks that are useful across the rest of the solution and in standalone projects.

## What it includes

- dynamic and reusable collection primitives
- slot maps and sparse sets
- object pooling helpers
- atomic counters
- collection utilities and shared result types

## Highlights

- designed for performance-sensitive code paths
- minimal abstraction over common data-structure patterns
- useful as a small runtime dependency for other packages in this solution

## Main types

- `DynamicArray<T>`
- `SlotMap<T>`
- `ConcurrentSlotMap<T>`
- `SparseSet<T>`
- `AtomicCounter`
- `ObjectPool<T>`
- `Result<T>`

## Example

```csharp
using Misaki.HighPerformance.Collections;

var values = new DynamicArray<int>();
values.Add(10);
values.Add(20);
values.Add(30);

ref int firstValue = ref values[0];

Span<int> span = values.AsSpan();
```

## Package reference

```bash
dotnet add package Misaki.HighPerformance
```

## Notes

This project targets `net10.0` and enables unsafe code where needed by the broader solution.
