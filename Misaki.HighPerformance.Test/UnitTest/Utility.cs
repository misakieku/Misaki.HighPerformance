using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Misaki.HighPerformance.Test.UnitTest;

internal static class Utility
{
    extension(Assert)
    {
        public unsafe static void IsNull(void* ptr, string message = "Expected pointer to be null.", [CallerArgumentExpression(nameof(ptr))] string conditionExpression = "")
        {
            Assert.IsTrue(ptr == null, message, conditionExpression);
        }

        public unsafe static void IsNotNull(void* ptr, string message = "Expected pointer to be not null.", [CallerArgumentExpression(nameof(ptr))] string conditionExpression = "")
        {
            Assert.IsTrue(ptr != null, message, conditionExpression);
        }
    }
}
