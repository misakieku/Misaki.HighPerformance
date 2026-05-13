# Collection types

All collection types in this library are structs that wrap unmanaged memory allocated through an `AllocationHandle`. They follow the same general API patterns as the BCL collections but operate entirely outside the GC heap.

## Array-like types

| Data structure | Description |
|---|---|
| `UnsafeArray<T>` | A fixed-size array. Supports resize via `Resize()`. |
| `UnsafeList<T>` | A dynamically resizing list. |
| `UnsafeQueue<T>` | A FIFO queue. |
| `UnsafeStack<T>` | A LIFO stack. |
| `UnsafeChunkedList<T>` | A list that stores elements in fixed-size chunks. Adding elements never moves existing ones, providing stable element addresses. |

## Map and set types

| Data structure | Description |
|---|---|
| `UnsafeHashMap<TKey, TValue>` | An unordered associative array of key-value pairs. |
| `UnsafeHashSet<T>` | A set of unique values. |
| `UnsafeMultiHashMap<TKey, TValue>` | An unordered associative array where keys don't have to be unique. Multiple values can share the same key. |

## Sparse types

| Data structure | Description |
|---|---|
| `UnsafeSparseSet<T>` | A sparse set that provides O(1) insertion, deletion, and lookup. Uses the dense/sparse array pattern. Sparse indices work like entity IDs and are automatically generated. |
| `UnsafeSlotMap<T>` | A slot map with generation counters. Fast insertion, removal, and lookup by slot index. The generation counter prevents stale index access to data that has been replaced. |

## String and text types

| Data structure | Description |
|---|---|
| `FixedString32` | A 32-byte UTF-16 string (16 characters max). |
| `FixedString64` | A 64-byte UTF-16 string (32 characters max). |
| `FixedString128` | A 128-byte UTF-16 string (64 characters max). |
| `FixedString256` | A 256-byte UTF-16 string (128 characters max). |
| `FixedString512` | A 512-byte UTF-16 string (256 characters max). |
| `FixedString1024` | A 1024-byte UTF-16 string (512 characters max). |
| `FixedString2048` | A 2048-byte UTF-16 string (1024 characters max). |
| `FixedString4096` | A 4096-byte UTF-16 string (2048 characters max). |
| `FixedText32` | A 32-byte UTF-8 encoded string (30 bytes max). |
| `FixedText64` | A 64-byte UTF-8 encoded string (62 bytes max). |
| `FixedText128` | A 128-byte UTF-8 encoded string (126 bytes max). |
| `FixedText256` | A 256-byte UTF-8 encoded string (254 bytes max). |
| `FixedText512` | A 512-byte UTF-8 encoded string (510 bytes max). |
| `FixedText1024` | A 1024-byte UTF-8 encoded string (1022 bytes max). |
| `FixedText2048` | A 2048-byte UTF-8 encoded string (2046 bytes max). |
| `FixedText4096` | A 4096-byte UTF-8 encoded string (4094 bytes max). |

All fixed string and text types are stack-only. Every copy duplicates the underlying data.

## Parallel types

| Data structure | Description |
|---|---|
| `UnsafeParallelQueue<T>` | A dynamically resizing, lock-free queue. Provides `ParallelProducer` and `ParallelConsumer` views for safe concurrent access. Uses a spin lock only during chunk allocation. |
| `UnsafeParallelHashMap<TKey, TValue>` | A parallel hash map. Provides a `ParallelWriter` for concurrent insertions from multiple threads. Does not resize concurrently — pre-allocate enough capacity. |

## Bit structures

| Data structure | Description |
|---|---|
| `UnsafeBitSet` | An arbitrary-sized array of bits with set, test, clear, and search operations. |

## Utility types

| Type | Description |
|---|---|
| `ReadOnlyUnsafeCollection<T>` | A read-only view over a pointer and count. Implicitly converts to `ReadOnlySpan<T>`. Useful for passing collection data to APIs that expect spans. |
| `DisposablePtr<T>` | A pointer wrapper that calls `Dispose` on the pointed-to value when disposed. Used by allocate-on-heap factory methods like `UnsafeParallelQueue<T>.Allocate()`. |

## Additional resources

- [Introduction](introduction.md) — install, first steps, and safety checks
- [Architecture overview](architecture-overview.md) — layering, AllocationHandle, and struct semantics
- [Allocators](allocators.md) — built-in allocators, MemoryPool, and custom allocators
