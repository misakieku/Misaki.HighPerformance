namespace Misaki.Highperformance.TypeGenerator.Models;

[Flags]
public enum TypeCapabilities
{
    None = 0,
    Arithmetic    = 1 << 0, // +, -, *, /, %
    Bitwise       = 1 << 1, // &, |, ^, ~, !
    Shift         = 1 << 2, // <<, >>
    Logic         = 1 << 3, // any, all, select
    Trig          = 1 << 4, // sin, cos, tan, asin, acos, atan
    Hyperbolic    = 1 << 5, // sinh, cosh, tanh
    Exponential   = 1 << 6, // exp, exp2, exp10, log, log2, log10, pow, sqrt, rsqrt
    Rounding      = 1 << 7, // floor, ceil, round, trunc, frac
    LinearAlgebra = 1 << 8, // dot, cross, length, lengthsq, distance, distancesq, normalize, reflect, project
}
