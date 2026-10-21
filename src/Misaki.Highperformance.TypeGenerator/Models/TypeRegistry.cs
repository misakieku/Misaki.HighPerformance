namespace Misaki.Highperformance.TypeGenerator.Models;

public static class TypeRegistry
{
    public static readonly ScalarType Float = new(
        ScalarKind.Float,
        "float",
        "float",
        "float",
        4,
        "f",
        "0.0f",
        "1.0f",
        "global::System.MathF",
        HasHardwareSimd: true,
        TypeCapabilities.Arithmetic | TypeCapabilities.Trig | TypeCapabilities.Hyperbolic | TypeCapabilities.Exponential | TypeCapabilities.Rounding | TypeCapabilities.LinearAlgebra
    );

    public static readonly ScalarType Double = new(
        ScalarKind.Double,
        "double",
        "double",
        "double",
        8,
        "",
        "0.0",
        "1.0",
        "global::System.Math",
        HasHardwareSimd: true,
        TypeCapabilities.Arithmetic | TypeCapabilities.Trig | TypeCapabilities.Hyperbolic | TypeCapabilities.Exponential | TypeCapabilities.Rounding | TypeCapabilities.LinearAlgebra
    );

    public static readonly ScalarType Half = new(
        ScalarKind.Half,
        "half",
        "global::System.Half",
        "global::System.Half",
        2,
        "",
        "(global::System.Half)0.0f",
        "(global::System.Half)1.0f",
        "global::System.Half",
        HasHardwareSimd: false,
        TypeCapabilities.Arithmetic | TypeCapabilities.Trig | TypeCapabilities.Hyperbolic | TypeCapabilities.Exponential | TypeCapabilities.Rounding | TypeCapabilities.LinearAlgebra
    );

    public static readonly ScalarType Int = new(
        ScalarKind.Int,
        "int",
        "int",
        "int",
        4,
        "",
        "0",
        "1",
        "global::System.Math",
        HasHardwareSimd: true,
        TypeCapabilities.Arithmetic | TypeCapabilities.Bitwise | TypeCapabilities.Shift | TypeCapabilities.LinearAlgebra
    );

    public static readonly ScalarType UInt = new(
        ScalarKind.UInt,
        "uint",
        "uint",
        "uint",
        4,
        "u",
        "0u",
        "1u",
        "global::System.Math",
        HasHardwareSimd: true,
        TypeCapabilities.Arithmetic | TypeCapabilities.Bitwise | TypeCapabilities.Shift | TypeCapabilities.LinearAlgebra
    );

    public static readonly ScalarType Bool = new(
        ScalarKind.Bool,
        "bool",
        "bool",
        "uint",
        4,
        "",
        "false",
        "true",
        "",
        HasHardwareSimd: false,
        TypeCapabilities.Bitwise | TypeCapabilities.Logic
    );

    public static IReadOnlyList<ScalarType> AllScalars { get; } =
    [
        Float,
        Double,
        Half,
        Int,
        UInt,
        Bool
    ];

    public static IEnumerable<VectorType> AllVectors =>
        from scalar in AllScalars
        from dim in new[] { 2, 3, 4 }
        select new VectorType(scalar, dim);

    public static IEnumerable<MatrixType> AllMatrices =>
        from scalar in AllScalars
        from row in new[] { 2, 3, 4 }
        from col in new[] { 2, 3, 4 }
        select new MatrixType(scalar, row, col);
}
