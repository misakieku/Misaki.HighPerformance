using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Services;

var unfreeArray = new UnsafeArray<int>(10, Allocator.Persistent);
unfreeArray.Dispose();

AllocationManager.Dispose();
