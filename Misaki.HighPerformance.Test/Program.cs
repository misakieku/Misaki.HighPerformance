using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

//BenchmarkRunner.Run<SPMDBenchmark>();

AllocationManager.Initialize(AllocationManagerInitOpts.Default);
var set = new UnsafeBitSet(100, AllocationHandle.Persistent, AllocationOption.Clear);
set.SetBit(0);
Console.WriteLine(set.NextSetBit(0));

set.Dispose();
AllocationManager.Dispose();
