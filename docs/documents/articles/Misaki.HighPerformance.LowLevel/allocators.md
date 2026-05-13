# Allocators

Every allocation in this library goes through an `AllocationHandle`. The handle determines where memory comes from, how it's organized, and when it's reclaimed. There is no default allocator — you always choose one explicitly.

## AllocationHandle

`AllocationHandle` is a struct with a state pointer and three function pointers:

```
AllocationHandle
  _state   : void*    — allocator-specific context
  _alloc   : delegate — allocate
  _realloc : delegate — reallocate
  _free    : delegate — free
```

The function pointers let any allocator implementation be wrapped without boxing, virtual dispatch, or GC pressure. To create a custom allocator, you only need to populate this struct.

Every collection stores the `AllocationHandle` it was created with and uses it for all internal memory operations.

## Built-in allocators

The library ships with allocators managed by `AllocationManager`:

| Allocator | Handle access | Backing | Lifetime | Reuse | Thread safety |
|---|---|---|---|---|---|
| Heap | `AllocationHandle.Persistent` | Heap (replaced by mimalloc if `MHP_ENABLE_MIMALLOC` is defined) | Until freed | Controlled by malloc or mimalloc | Yes |
| FreeList | `AllocationHandle.FreeList` | Heap | Until freed | Yes — reuses freed blocks | Yes (remote-free queue) |
| TLSF | `AllocationHandle.TLSF` | Virtual memory chunks | Until freed | Yes — low fragmentation | Yes (lock-protected) |
| VirtualArena | `AllocationHandle.Temp` | Virtual memory (reserve on init, commit on demand) | Until `ResetTempAllocator()` | No — bulk reset only | Yes |
| VirtualStack | `AllocationManager.CreateStackScope().AllocationHandle` | Virtual memory (reserve on init, commit on demand) | Via `VirtualStack.Scope` | Yes when scope disposed | No — thread-local |

The library also provides heap-based variants of `Arena` and `Stack` for direct use via `MemoryPool`:

| Allocator | Backing | Lifetime | Reuse |
|---|---|---|---|
| Arena | Heap (`NativeMemory.Alloc`) | Until `Reset()` or dispose | No — bulk reset only |
| Stack | Heap (`NativeMemory.Alloc`) | Via `Stack.Scope` | Yes when scope disposed |

### VirtualArena (Temp)

VirtualArena reserves a large virtual address range on initialization and commits physical memory on demand as allocations are made. Allocation bumps an offset pointer — there is no free-list walk, no block splitting, and no metadata search. This makes it the fastest allocator in the library.

**Free is a no-op.** Individual frees do nothing. The entire arena is reset at once by calling `AllocationManager.ResetTempAllocator()`, which rewinds the offset back to zero. This makes the arena ideal for frame-scoped or phase-scoped work where you want to allocate freely and discard everything at once.

```csharp
// Temp allocations are freed collectively.
var a = new UnsafeArray<int>(10, AllocationHandle.Temp);
var b = new UnsafeList<int>(AllocationHandle.Temp);

// Reset everything.
AllocationManager.ResetTempAllocator();

// Both a and b are now invalid.
```

Under the hood, `Temp` uses a `MemoryPool<VirtualArena>`. The arena uses 64 KB pages for OS-level commit granularity and is thread-safe with a lock-free bump-allocate path.

### FreeList (FreeList)

The free-list allocator reclaims and reuses individual blocks. When you free a block, it is returned to a size-bucketed free list and can satisfy a future allocation of the same bucket. This avoids the overhead of re-committing virtual memory while keeping fragmentation low.

FreeList uses per-thread caches for the hot path and a remote-free queue for cross-thread deallocation. This means threads can free each other's allocations without a global lock on the common path.

```csharp
// FreeList allocations can be freed and reused independently.
var a = new UnsafeArray<int>(10, AllocationHandle.FreeList);
a.Dispose(); // Memory goes back to the free list.

var b = new UnsafeArray<int>(10, AllocationHandle.FreeList);
// Likely backed by the same memory as a.
```

### TLSF (Persistent)

The Two-Level Segregated Fit allocator guarantees O(1) allocation and deallocation with very low external fragmentation. It organizes free blocks by size class in a two-level bitmap index, which lets it find a best-fit block in constant time. TLSF backs its memory pool with virtual memory chunks allocated via `Mmap`.

`AllocationHandle.Persistent` maps to the manager's internal TLSF allocator. Use it for long-lived allocations where fragmentation matters and where you need consistent O(1) performance.

The TLSF implementation is single-threaded internally and wrapped in a lock by the manager. For concurrent use from multiple threads, access is serialized through that lock.

```csharp
// Persistent (TLSF) for long-lived allocations.
var cache = new UnsafeHashMap<int, EntityData>(
    AllocationHandle.Persistent
);

// ... use for the lifetime of the application ...

cache.Dispose();
```

### VirtualStack (Stack)

VirtualStack is a LIFO allocator backed by a reserved virtual address range that commits physical memory on demand. It allocates by bumping an offset (like VirtualArena) but adds a scope mechanism that rewinds to a saved position on dispose.

The stack is **not thread-safe** and is designed for single-threaded or thread-local contexts. Each thread gets its own stack through `AllocationManager.CreateStackScope()`:

```csharp
// Creates a thread-local stack scope.
// The scope's AllocationHandle can be used for allocations
// that are automatically reclaimed when the scope ends.
using var scope = AllocationManager.CreateStackScope();

// Allocate from the stack.
var temp = new UnsafeArray<int>(10, scope.AllocationHandle);

// When scope is disposed, all allocations are rewound.
```

The stack uses 64 KB commit granularity and supports scope nesting. The scope records the offset at creation and rewinds to it on dispose — allocations from an inner scope are always reclaimed before the outer scope.

### Arena (heap)

`Arena` is the heap-based counterpart of `VirtualArena`. It uses `NativeMemory.Alloc` to allocate a fixed-size buffer on the heap and bumps an offset pointer for allocations. It supports the same bulk-reset pattern but without virtual memory reservation.

```csharp
using var arena = new MemoryPool<Arena, Arena.CreationOptions>(
    new Arena.CreationOptions { size = 1024 * 1024 });

var arr = new UnsafeArray<int>(10, arena.AllocationHandle);

// Reset rewinds the offset. Memory stays allocated.
arena.Allocator.Reset();
```

`Arena` is thread-safe with a lock-free bump-allocate path.

### Stack (heap)

`Stack` is the heap-based counterpart of `VirtualStack`. It uses `NativeMemory.Alloc` to allocate a fixed-size buffer on the heap and uses the same scope mechanism to rewind allocations on scope dispose.

```csharp
using var stack = new MemoryPool<Stack, Stack.CreationOptions>(
    new Stack.CreationOptions { size = 1024 * 1024 });

using (var scope = stack.Allocator.CreateScope(stack.AllocationHandle))
{
    var arr = new UnsafeArray<int>(10, scope.AllocationHandle);
} // Scope dispose rewinds all allocations.
```

`Stack` is **not thread-safe** and is designed for single-threaded contexts.

## AllocationManager configuration

`AllocationManager` can be configured with an `AllocationManagerDesc` to control the capacity and alignment of each built-in allocator:

```csharp
var desc = new AllocationManagerDesc
{
    ArenaCapacity = 1024 * 1024 * 1024,     // 1 GB virtual reservation
    StackCapacity = 32 * 1024 * 1024,       // 32 MB per thread
    FreeListChunkSize = 64 * 1024,          // 64 KB chunks
    FreeListDefaultAlignment = 16,          // 16-byte alignment
    TLSFAlignment = 16,                     // 16-byte alignment
    TLSFInitialChunkSize = 64 * 1024 * 1024 // 64 MB initial chunk
};

AllocationManager.Initialize(desc);
```

Calling `Initialize()` with no arguments uses these same defaults.

## MemoryPool for scoped allocators

`MemoryPool<TAllocator, TOpts>` creates a standalone allocator outside of `AllocationManager`. This is useful when you want:

- An allocator type not available in the built-in set
- An allocator scoped to a single method or algorithm
- Isolation from the global allocation state

```csharp
using var pool = new MemoryPool<TLSF, TLSF.CreationOptions>(
    new TLSF.CreationOptions
    {
        alignment = 16,
        initialChunkSize = 1024 * 1024
    });

using var array = new UnsafeArray<int>(10, pool.AllocationHandle);

// When pool is disposed, all TLSF memory is released.
```

The pool wraps any type implementing `IMemoryAllocator<TSelf, TOpts>`. This includes `Arena`, `VirtualArena`, `Stack`, `VirtualStack`, `TLSF`, and `DynamicArena`:

```csharp
// Heap-based arena.
using var arenaPool = new MemoryPool<Arena, Arena.CreationOptions>(
    new Arena.CreationOptions { size = 1024 * 1024 });

// Virtual-memory-based stack.
using var stackPool = new MemoryPool<VirtualStack, VirtualStack.CreationOptions>(
    new VirtualStack.CreationOptions { reserveCapacity = 1024 * 1024 });

// Dynamically growing arena (heap).
using var dynamicPool = new MemoryPool<DynamicArena, DynamicArena.CreationOptions>(
    new DynamicArena.CreationOptions { initialSize = 4096 });
```

`DynamicArena` creates linked arenas that grow automatically when full, with no virtual address reservation upfront.

## Custom allocators

Creating a custom allocator requires populating an `AllocationHandle` with your own allocate, reallocate, and free functions:

```csharp
static void* MyAlloc(void* state, nuint size, nuint alignment, AllocationOption option)
{
    // Your allocation logic.
}

static void* MyRealloc(void* state, void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption option)
{
    // Your reallocation logic.
}

static void MyFree(void* state, void* ptr)
{
    // Your deallocation logic.
}

var handle = new AllocationHandle(
    myAllocatorState,
    &MyAlloc,
    &MyRealloc,
    &MyFree
);

var array = new UnsafeArray<int>(10, handle);
```

For more structured custom allocators, implement `IMemoryAllocator<TSelf, TOpts>` and use `MemoryPool<TAllocator, TOpts>`:

```csharp
public unsafe struct MyAllocator
    : IMemoryAllocator<MyAllocator, MyAllocator.CreationOptions>
{
    public struct CreationOptions { /* ... */ }

    public static MyAllocator Create(in CreationOptions opts) { /* ... */ }
    public void* Allocate(nuint size, nuint alignment, AllocationOption option) { /* ... */ }
    public void* Reallocate(void* ptr, nuint oldSize, nuint newSize, nuint alignment, AllocationOption option) { /* ... */ }
    public void Free(void* ptr) { /* ... */ }
    public void Dispose() { /* ... */ }
}

using var pool = new MemoryPool<MyAllocator, MyAllocator.CreationOptions>(/* ... */);
```

## AllocationOption

`AllocationOption` is a flags enum that controls per-allocation behavior:

| Value | Behavior |
|---|---|
| `None` | Memory is returned as-is, contents are undefined |
| `Clear` | All allocated bytes are zeroed before returning |

```csharp
// Request zeroed memory.
var ptr = handle.Alloc(1024, 16, AllocationOption.Clear);
```

`Clear` is useful for security-sensitive data or when you need deterministic initialization. Omitting it avoids the cost of touching every page.

## Enable Mimalloc

You can define `MHP_ENABLE_MIMALLOC` to use mimalloc as the underlying allocator for `AllocationHandle.Persistent` and `MemoryUtility.Malloc` instead of the default C allocator.

> Using mimalloc requires to install the `TerraFX.Interop.Mimalloc` package.

## Additional resources

- [Introduction](introduction.md) — install, first steps, and safety checks
- [Architecture overview](architecture-overview.md) — layering, MemoryHandle, and struct semantics
- [Collection types](collection-types.md) — all available data structures
