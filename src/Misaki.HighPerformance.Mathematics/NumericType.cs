namespace Misaki.HighPerformance.Mathematics;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class NumericTypeAttribute : Attribute
{
    public NumericTypeAttribute(Type componentType, int componentSize, int row, int column, string typePrefix, bool arithmetic = true, bool canInverse = true, bool isHalf = false, Type? elementType = default, Type? vectorType = default)
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