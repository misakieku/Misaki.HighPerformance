# Architecture overview

The library is structured as a stack of explicit layers. Each layer has a single responsibility, and you can work at any level depending on how much control you need.

```
User Code
    |
Unsafe collections (UnsafeArray, UnsafeList, UnsafeHashMap, ...)
    |
AllocationHandle (function-pointer-based "interface")
    |
Allocators (Arena / FreeList / TLSF / Stack)
    |
OS memory (VirtualAlloc / malloc / mimalloc)
```

## Design philosophy

The library is built around four principles:

**Explicit over implicit.** Every allocation requires an `AllocationHandle`. There is no default allocator, no hidden malloc, and no GC fallback. If memory is allocated, you chose exactly where it came from.

**Unsafe-first.** All collections work with raw pointers internally. Safety checks are optional and compiled away in release. The library trusts you — and lets you prove you can be trusted.

**Struct-only collections.** No managed objects, no handles to GC-tracked state, no hidden heap allocations. A collection is just a pointer + metadata. Copy it, inline it, store it in unmanaged memory.

**Zero overhead by default.** Safety checks, stack traces, tracking — all opt-in via compile-time constants. Release builds produce the same code as hand-written pointer manipulation.

## AllocationHandle pattern

`AllocationHandle` is the central abstraction. It is a struct containing a state pointer and three function pointers:

```
AllocationHandle
  _state   : void*    — allocator-specific context
  _alloc   : delegate — allocate
  _realloc : delegate — reallocate
  _free    : delegate — free
```

Because it uses function pointers instead of virtual interfaces, an `AllocationHandle` call is a direct indirect call — no boxing, no vtable lookup, no GC pressure. Any combination of alloc/free/realloc functions can be composed into a handle, which means custom allocators are just a matter of filling in the struct.

Every collection stores its `AllocationHandle` and uses it for all internal memory operations.

## MemoryHandle tracking

`MemoryHandle` is a safety-only struct that pairs an allocation ID with a generation counter:

```csharp
public readonly struct MemoryHandle
{
    public readonly int ID;
    public readonly int Generation;
}
```

When `MHP_ENABLE_SAFETY_CHECKS` is defined, every allocation is registered in `AllocationManager`'s tracking database with its address and size. Operations like dispose verify the handle is still valid, catching double-free and use-after-free errors. The generation counter prevents handle reuse after an allocation is freed.

In release builds, `MemoryHandle` fields compile away to nothing.

## AllocationManager

`AllocationManager` serves two roles:

- **Registry.** It owns the global instances of the built-in allocators (`Temp`, `FreeList`, `Persistent`, `TLSF`). Calling `AllocationHandle.Persistent` returns a handle to the manager's internal TLSF allocator instance.

- **Safety database.** When safety checks are enabled, the manager tracks every live allocation. Diagnostics, snapshot inspection, and leak detection all go through this system.

The manager must be initialized before any allocation and disposed at shutdown:

```csharp
AllocationManager.Initialize();
// ... use collections ...
AllocationManager.Dispose();
```

## MemoryPool for scoped allocators

`MemoryPool<TAllocator, TOpts>` creates a standalone allocator scoped to a method or algorithm, independent of `AllocationManager`. This is useful when you want an allocator that doesn't exist in the built-in set, or you want to isolate allocations from the global state:

```csharp
using var pool = new MemoryPool<TLSF, TLSF.CreationOptions>(
    new TLSF.CreationOptions { alignment = 16 });

using var array = new UnsafeArray<int>(10, pool.AllocationHandle);
```

Collections work with any `AllocationHandle`, regardless of whether it came from `AllocationManager` or a `MemoryPool`. You do not need to initialize `AllocationManager` when using only standalone pools. The allocator lives as long as the pool, and all its memory is released when the pool is disposed.

## Struct semantics

All collections are structs with no managed references. This has two important consequences:

- **No GC overhead.** The struct itself is stack-allocated or inlineable. The memory it points to is unmanaged and outside the GC's view.

- **Pass by value copies the struct.** If you pass an `UnsafeList<T>` to a method without `ref`, the method operates on a copy. Additions, removals, and resizes on that copy are invisible to the caller. Always use `ref` for mutation:

```csharp
public void Process(ref UnsafeList<int> list) { ... }
```

## Safety checks system

The library supports two compile-time safety levels:

- `MHP_ENABLE_SAFETY_CHECKS` — enables bounds checking, use-after-free detection, double-free detection, and `IsCreated` validity verification (checks that the internal memory handle is still registered in the tracking database).

- `MHP_ENABLE_STACKTRACE` — adds stack trace capture on every allocation, enabling precise leak investigation. Requires `MHP_ENABLE_SAFETY_CHECKS`.

When `MHP_ENABLE_SAFETY_CHECKS` is not defined, the safety fields compile away and `IsCreated` only checks whether the internal pointer is non-null without verifying the actual validity of the memory. This matches the performance of raw pointer code.

## AllocationOption

Allocation operation can take an optional `AllocationOption`:

| Value | Behavior |
|---|---|
| `None` | Default — memory is returned as-is |
| `Clear` | Zero the allocated memory before returning |

## Content files packaging

The library is packaged as content files rather than a traditional assembly. Source files are embedded directly into the consuming project at build time. This enables the AOT compiler to see through every call site, inline aggressively, and strip unused code paths — including the entire safety check infrastructure when the relevant constants are undefined.
