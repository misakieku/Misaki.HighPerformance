using System.Threading;

namespace Misaki.HighPerformance.Image.Runtime
{
    internal static class MemoryStats
    {
        private static int s_allocations;

        public static int Allocations
        {
            get
            {
                return s_allocations;
            }
        }

        internal static void Allocated()
        {
            Interlocked.Increment(ref s_allocations);
        }

        internal static void Freed()
        {
            Interlocked.Decrement(ref s_allocations);
        }
    }
}