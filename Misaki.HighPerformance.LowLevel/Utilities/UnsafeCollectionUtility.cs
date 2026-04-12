using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Utilities;

/// <summary>
/// Provides extension methods for copying elements between unsafe collections and spans, converting collections to
/// arrays or lists, and searching for values.
/// </summary>
public static unsafe class UnsafeCollectionUtility
{
    [Conditional("DEBUG")]
    internal static void ReportDoubleFree<T>(void* buffer)
    {
        if (buffer == null)
        {
            return;
        }

        var message = $"The {typeof(T).Name} is not created or already disposed.";
#if MHP_ENABLE_STACKTRACE
        var stackTrace = new StackTrace(1, true);
        var sb = new System.Text.StringBuilder();
        foreach (var frame in stackTrace.GetFrames())
        {
            var fileName = frame?.GetFileName();
            if (frame != null)
            {
                var methodInfo = DiagnosticMethodInfo.Create(frame);
                sb.AppendLine($"File: {fileName}, Type: {methodInfo?.DeclaringTypeName}, Method: {methodInfo?.Name}, Line: {frame.GetFileLineNumber()}");
            }
        }

        message += Environment.NewLine + sb.ToString();
#endif
        Debug.WriteLine(message);
    }

    /// <summary>
    /// Converts a managed array to an UnsafeArray by copying its elements to unmanaged memory.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array, which must be unmanaged.</typeparam>
    /// <param name="source">The managed array to convert.</param>
    /// <param name="allocationHandle">The allocation handle to use for memory allocation of the UnsafeArray.</param>
    /// <returns>A new UnsafeArray containing a copy of the source array elements.</returns>
    public static UnsafeArray<T> ToUnsafeArray<T>(this T[] source, AllocationHandle allocationHandle)
        where T : unmanaged
    {
        var array = new UnsafeArray<T>(source.Length, allocationHandle);
        fixed (T* pSrc = source)
        {
            MemCpy(array.GetUnsafePtr(), pSrc, (uint)(source.Length * sizeof(T)));
        }

        return array;
    }

    /// <summary>
    /// Converts a managed List to an UnsafeList by copying its elements to unmanaged memory.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list, which must be unmanaged.</typeparam>
    /// <param name="source">The managed List to convert.</param>
    /// <param name="allocationHandle">The allocation handle to use for memory allocation of the UnsafeList.</param>
    /// <returns>A new UnsafeList containing a copy of the source list elements.</returns>
    public static UnsafeList<T> ToUnsafeList<T>(this List<T> source, AllocationHandle allocationHandle)
        where T : unmanaged
    {
        var list = new UnsafeList<T>(source.Count, allocationHandle);
        fixed (T* pSrc = CollectionsMarshal.AsSpan(source))
        {
            MemCpy(list.GetUnsafePtr(), pSrc, (uint)(source.Count * sizeof(T)));
        }

        return list;
    }
}
