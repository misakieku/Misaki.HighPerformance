// Polyfills needed for modern C# features on netstandard2.0 target.
// These types are built into .NET 5+ runtimes but must be declared manually
// when targeting netstandard2.0, which source generators must do.

namespace System.Runtime.CompilerServices
{
    // Enables `init` accessors and `record` types (C# 9)
    internal static class IsExternalInit { }

    // Enables `required` members (C# 11)
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
            => FeatureName = featureName;
        public string FeatureName { get; }
        public bool IsOptional { get; init; }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    // Enables `required` constructor attribution (C# 11)
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }
}
