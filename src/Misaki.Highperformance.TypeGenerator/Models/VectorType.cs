namespace Misaki.Highperformance.TypeGenerator.Models;

public sealed record VectorType(ScalarType Element, int Dimension)
{
    private static readonly string[] s_components = ["x", "y", "z", "w"];

    public string Name => $"{Element.Name}{Dimension}";
    public int TotalBytes => Element.SizeInBytes * Dimension;

    public StorageLayout Storage
    {
        get
        {
            if (Element.IsBool)
            {
                return Dimension == 4 ? StorageLayout.SimdUnion128 : StorageLayout.Sequential;
            }

            return (Element.HasHardwareSimd, TotalBytes) switch
            {
                (true, 16) => StorageLayout.SimdUnion128,
                (true, 32) => StorageLayout.SimdUnion256,
                _ => StorageLayout.Sequential
            };
        }
    }

    public OperationStrategy OpStrategy => Storage switch
    {
        StorageLayout.SimdUnion128 => OperationStrategy.SimdIntrinsics128,
        StorageLayout.SimdUnion256 => OperationStrategy.SimdIntrinsics256,
        _ => OperationStrategy.ScalarComponent
    };

    public int SimdVectorBits => Storage switch
    {
        StorageLayout.SimdUnion128 => 128,
        StorageLayout.SimdUnion256 => 256,
        _ => 0
    };

    public string BoolVectorName => $"bool{Dimension}";
    public string IntVectorName => $"int{Dimension}";
    public string UIntVectorName => $"uint{Dimension}";
    public string FloatVectorName => $"float{Dimension}";
    public string DoubleVectorName => $"double{Dimension}";
    public string HalfVectorName => $"half{Dimension}";

    public IReadOnlyList<string> Components => s_components.Take(Dimension).ToArray();
}
