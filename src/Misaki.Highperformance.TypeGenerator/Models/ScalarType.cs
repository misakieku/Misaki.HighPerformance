namespace Misaki.Highperformance.TypeGenerator.Models;

public enum ScalarKind
{
    Float,
    Double,
    Half,
    Int,
    UInt,
    Bool
}

public sealed record ScalarType(
    ScalarKind Kind,
    string Name,
    string LogicalTypeName,
    string StorageTypeName,
    int SizeInBytes,
    string LiteralSuffix,
    string ZeroLiteral,
    string OneLiteral,
    string MathClass,
    bool HasHardwareSimd,
    TypeCapabilities Capabilities
)
{
    public bool IsFloatingPoint => Kind is ScalarKind.Float or ScalarKind.Double or ScalarKind.Half;
    public bool IsInteger => Kind is ScalarKind.Int or ScalarKind.UInt;
    public bool IsSigned => Kind is ScalarKind.Float or ScalarKind.Double or ScalarKind.Half or ScalarKind.Int;
    public bool IsBool => Kind is ScalarKind.Bool;

    public string FormatLiteral(double val) => Kind switch
    {
        ScalarKind.Half => $"(System.Half){val}f",
        ScalarKind.Float => $"{val}f",
        ScalarKind.Double => $"{val:0.0###############}",
        ScalarKind.UInt => $"{(uint)val}u",
        ScalarKind.Int => $"{(int)val}",
        ScalarKind.Bool => val != 0 ? "true" : "false",
        _ => $"{val}"
    };

    public string EmitPackFromLogical(string expr) => Kind switch
    {
        ScalarKind.Bool => $"({expr} ? ~0u : 0u)",
        _ => expr
    };

    public string EmitUnpackToLogical(string expr) => Kind switch
    {
        ScalarKind.Bool => $"({expr} != 0)",
        _ => expr
    };
}
