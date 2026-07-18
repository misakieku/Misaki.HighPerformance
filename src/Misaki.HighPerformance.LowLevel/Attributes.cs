namespace Misaki.HighPerformance.LowLevel;

[AttributeUsage(AttributeTargets.Struct)]
public class NonCopyableAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue)]
public class OwnerAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
public class DiligentAttribute : Attribute
{
}
