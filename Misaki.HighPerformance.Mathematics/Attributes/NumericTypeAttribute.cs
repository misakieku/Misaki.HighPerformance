namespace Misaki.HighPerformance.Mathematics.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
internal class NumericTypeAttribute : Attribute
{
    public NumericTypeAttribute(Type componentType, int componentSize, int row, int column, string typePrefix, bool arithmetic = true, bool canInverse = true, Type? elementType = default, Type? vectorType = default)
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
internal class NumericConvertableAttribute : Attribute
{
    public NumericConvertableAttribute(string template, params Type[] types)
    {
    }
}