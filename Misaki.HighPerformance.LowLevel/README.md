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
var opts = new AllocationManagerInitOpts
{
    ArenaCapacity = 1024 * 1024,
    StackCapacity = 1024 * 1024,
    FreeListConcurrencyLevel = 1
};

AllocationManager.Initialize(opts);

var arr = new UnsafeArray<int>(10, Allocator.Persistent);

// Use the array

arr.Dispose();

AllocationManager.Dispose();
```

You can enable debug features for leak detection and use-after-free checks by defining `MHP_ENABLE_SAFETY_CHECKS` in your project. And define `MHP_ENABLE_STACKTRACE` to enable additional debug features such as tracking allocations and providing detailed error messages.
> Which means if you disable the safety checks, the library will not perform any safety checks and provide the maximum performance, and it will be your responsibility to ensure correct usage to avoid memory leaks and undefined behavior.
> Even `IUnsafeCollection.IsCreated` will only check if the internal pointer is non-null, without verifying the actual validity of the memory.

You can also define `MHP_ENABLE_MIMALLOC` to use mimalloc as the underlying allocator instead of the default C allocator.
> Using mimalloc requires to install the `TerraFX.Interop.Mimalloc` package.

## Package reference

```bash
dotnet add package Misaki.HighPerformance.LowLevel
```

## Notes

This project targets `net10.0`, enables unsafe code, and is packaged as content files for downstream consumption.
