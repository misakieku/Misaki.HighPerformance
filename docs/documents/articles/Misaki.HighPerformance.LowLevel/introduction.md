# Introduction

The low-level library provides unsafe collections, allocators, and memory-management primitives for high-performance C#. It gives you explicit control over allocation, layout, and ownership so you can build systems that run without GC interference.

## Why a dedicated low-level library?

Standard .NET memory management wasn't designed for allocation-heavy game and simulation workloads:

- `NativeMemory.Alloc` and `Marshal.AllocHGlobal` provide raw allocation but no collection types, no lifetime tracking, and no safety checks.
- The BCL collections (`List<T>`, `Dictionary<K,V>`) allocate on the managed heap, producing GC pressure in tight loops.
- `Span<T>` and `Memory<T>` avoid allocations but don't own their memory and can't manage lifetimes across asynchronous boundaries.

This library solves these problems with pluggable allocators, unsafe collections that wrap raw pointers, and a safety check system that can be compiled away in release builds.

## Feature highlights

| Feature | Description |
|---|---|
| Pluggable allocators | Every allocation passes through an `AllocationHandle` — choose the right allocator per use case |
| Built-in allocators | `Temp`, `FreeList`, `Persistent`, `TLSF` — or use `MemoryPool` with heap-based `Arena` and `Stack` |
| Unsafe collections | Arrays, lists, queues, stacks, hash maps, hash sets, sparse sets, slot maps, chunked lists |
| Parallel-aware types | Lock-free queue and concurrent hash map with parallel reader/writer views |
| Fixed-size text | Stack-only `FixedString` (UTF-16) and `FixedText` (UTF-8) for zero-allocation string operations |
| Compile-time safety | `MHP_ENABLE_SAFETY_CHECKS` enables bounds checking, use-after-free detection, and leak tracking — compiled away in release |
| Custom allocators | Implement your own allocator by populating an `AllocationHandle` with function pointers |
| `MemoryPool<TAllocator>` | Scope allocators to a method or algorithm without touching the global state |
| Struct semantics | All collections are structs with no managed handles — no GC overhead, pass by `ref` for mutation |

## Basic usage

```csharp
using Misaki.HighPerformance.LowLevel.Buffer;

AllocationManager.Initialize();

var array = new UnsafeArray<int>(10, AllocationHandle.Persistent);

array[0] = 42;
Console.WriteLine(array[0]); // Output: 42

array.Dispose();
AllocationManager.Dispose();
```

## Who this is for

- Custom game engine developers who need allocation control without GC pauses
- Systems programmers building runtime components, job schedulers, or custom allocators
- .NET developers who have hit performance limits with managed collections in hot paths

## Requirements

- .NET 10.0 or later
- `unsafe` code enabled (`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`)

## Install

```bash
dotnet add package Misaki.HighPerformance.LowLevel
```

## Additional resources

- [Architecture overview](architecture-overview.md) — understand the allocation model and design philosophy
- [Allocators](allocators.md) — learn about each built-in allocator and how to create custom ones
- [Collection types](collection-types.md) — explore all available data structures
