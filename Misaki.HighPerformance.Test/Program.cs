using Misaki.HighPerformance.Test;
using Misaki.HighPerformance.Unsafe.Services;

AllocationManager.Initialize(512_000);
var test = new CollectionBenchmark();
test.UnsafeArray();
AllocationManager.Dispose();
