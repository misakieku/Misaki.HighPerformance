namespace Misaki.Highperformance.TypeGenerator.Models;

public enum StorageLayout
{
    Sequential,
    SimdUnion128,
    SimdUnion256
}

public enum OperationStrategy
{
    ScalarComponent,
    SimdIntrinsics128,
    SimdIntrinsics256
}
