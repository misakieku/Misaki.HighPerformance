using System;

namespace Misaki.HighPerformance.Mathematics.CodeGen
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class NumericTypeAttribute : Attribute
    {
        public NumericTypeAttribute(Type componentType, int componentSize, int row, int column, string typePrefix, bool arithmetic = true, bool canInverse = true, Type? elementType = default, Type? vectorType = default)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public class NumericConvertableAttribute : Attribute
    {
        public NumericConvertableAttribute(string template, params Type[] types)
        {
        }
    }

    [Flags]
    public enum SupportedVectorMath
    {
        None = 0,
        Min = 1 << 0,
        Max = 1 << 1,
        Lerp = 1 << 2,
        Unlerp = 1 << 3,
        Remap = 1 << 4,
        Mad = 1 << 5,
        Clamp = 1 << 6,
        Saturate = 1 << 7,
        Abs = 1 << 8,
        Dot = 1 << 9,
        Tan = 1 << 10,
        TanH = 1 << 11,
        Atan = 1 << 12,
        Atan2 = 1 << 13,
        Cos = 1 << 14,
        CosH = 1 << 15,
        Acos = 1 << 16,
        Sin = 1 << 17,
        SinH = 1 << 18,
        Asin = 1 << 19,
        Floor = 1 << 20,
        Ceil = 1 << 21,
        Round = 1 << 22,
        Trunc = 1 << 23,
        Frac = 1 << 24,
        Rcp = 1 << 25,
        Sign = 1 << 26,
        Pow = 1 << 27,
        Exp = 1 << 28,
        Exp2 = 1 << 29,
        Exp10 = 1 << 30,
        Log = 1 << 31,
        Log2 = 1 << 32,
        Log10 = 1 << 33,
        Fmod = 1 << 34,
        Modf = 1 << 35,
        Sqrt = 1 << 36,
        Rsqrt = 1 << 37,
        Length = 1 << 38,
        LengthSq = 1 << 39,
        Distance = 1 << 40,
        DistanceSq = 1 << 41,
        Cross = 1 << 42,
        SmoothStep = 1 << 43,
        Select = 1 << 44,
        Step = 1 << 45,
        FaceForward = 1 << 46,
        SinCos = 1 << 47,
        Any = 1 << 48,
        All = 1 << 49,
        Normalize = 1 << 50,
        Reflect = 1 << 51,
        Refract = 1 << 52,
        Project = 1 << 53,
        CountBits = 1 << 54,
        Lzcnt = 1 << 55,
        Tzcnt = 1 << 56,
        ReverseBits = 1 << 57,
        Rol = 1 << 58,
        Ror = 1 << 59,
        CeilPow2 = 1 << 60,
        CeilLog2 = 1 << 61,
        FloorLog2 = 1 << 62,
        Radians = 1 << 63,
        Degrees = 1 << 64,
        Cmin = 1 << 65,
        Cmax = 1 << 66,

        FloatingPointMask = ~0 & ~(CountBits | Lzcnt | Tzcnt | ReverseBits | Rol | Ror | CeilPow2 | CeilLog2 | FloorLog2),
        IntegerMask = Min | Max | Mad | Clamp | Abs | Dot | Sign | Any | All | Select | CountBits | Lzcnt | Tzcnt | ReverseBits | Rol | Ror | CeilPow2 | CeilLog2 | FloorLog2 | Cmin | Cmax,
        UnsignedIntegerMask = IntegerMask & ~(Sign),
    }

    [Flags]
    public enum SupportedMatrixMath
    {
        Transpose = 1 << 0,
        Determinant = 1 << 1,
        Inverse = 1 << 2,
        Adjugate = 1 << 3,
        Cofactor = 1 << 4,
        Minor = 1 << 5,
        OuterProduct = 1 << 6,
    }
}