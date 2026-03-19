# Misaki.HighPerformance.LowLevel

Unsafe collections, allocators, and memory-management primitives for high-performance C#.

This package is the lowest-level layer in the solution. It is intended for code that needs explicit control over allocation, layout, and ownership.

## What it includes

- unsafe arrays, lists, queues, stacks, hash maps, hash sets, sparse sets, and slot maps
- arenas and allocation helpers
- fixed-size text and string primitives
- memory and unsafe utilities
- pointer wrappers and function pointers
- low-level buffer and lifetime management types

## Highlights

- explicit allocation control
- cache-friendly and allocation-aware data structures
- APIs suited for systems programming, jobs, and custom runtime components
- designed to work well with unsafe and AOT-friendly code paths

## Main types

- `UnsafeArray<T>`
- `UnsafeList<T>`
- `UnsafeQueue<T>`
- `UnsafeStack<T>`
- `UnsafeHashMap<TKey, TValue>`
- `UnsafeHashSet<T>`
- `UnsafeSparseSet<T>`
- `UnsafeSlotMap<T>`
- `VirtualArena`
- `DynamicArena`
- `MemoryPool`
- `AllocationManager`
- `UnsafeUtility`
- `FixedString`
- `FixedText`

## Example

```csharp
// The low-level layer is meant for advanced ownership and allocation scenarios.
// Prefer the higher-level packages when they already satisfy your use case.
```

## Package reference

```bash
dotnet add package Misaki.HighPerformance.LowLevel
```

## Notes

This project targets `net10.0`, enables unsafe code, and is packaged as content files for downstream consumption.
