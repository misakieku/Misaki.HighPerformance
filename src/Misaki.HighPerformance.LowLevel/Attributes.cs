namespace Misaki.HighPerformance.LowLevel;

[AttributeUsage(AttributeTargets.Struct)]
public class NonCopyableAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
public class OwnershipTransferAttribute : Attribute
{
}