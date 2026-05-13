using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Test.UnitTest;

internal static class Utility
{
    extension(Assert)
    {
        public static unsafe void IsNull(void* ptr, string message = "Expected pointer to be null.", [CallerArgumentExpression(nameof(ptr))] string conditionExpression = "")
        {
            Assert.IsTrue(ptr == null, message, conditionExpression);
        }

        public static unsafe void IsNotNull(void* ptr, string message = "Expected pointer to be not null.", [CallerArgumentExpression(nameof(ptr))] string conditionExpression = "")
        {
            Assert.IsTrue(ptr != null, message, conditionExpression);
        }
    }
}
