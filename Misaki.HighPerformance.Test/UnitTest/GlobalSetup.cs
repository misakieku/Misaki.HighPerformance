using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.Test.UnitTest;

[TestClass]
public static class GlobalSetup
{
    [GlobalTestInitialize]
    public static void GlobalInitialize(TestContext ctx)
    {
        AllocationManager.Initialize(AllocationManagerDesc.Default);
    }

    [GlobalTestCleanup]
    public static void GlobalCleanup(TestContext ctx)
    {
        AllocationManager.Dispose();
    }
}
