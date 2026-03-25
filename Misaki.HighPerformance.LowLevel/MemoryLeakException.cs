using Misaki.HighPerformance.LowLevel.Buffer;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Misaki.HighPerformance.LowLevel;

/// <summary>
/// An exception that is thrown when a memory leak is detected.
/// </summary>
/// <param name="Infos">An array of AllocationInfo containing details about the memory leaks.</param>
public class MemoryLeakException : Exception
{
    private readonly string _message;

    public override string Message => _message;

    public MemoryLeakException(ReadOnlySpan<AllocationInfo> infos)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Found {infos.Length} memory lakes!");

        foreach (var info in infos)
        {
            stringBuilder.AppendLine(GetMessage(info.StackTrace));
        }

        _message = stringBuilder.ToString();
    }

    public MemoryLeakException(string message)
    {
        _message = message;
    }

    private static string GetMessage(StackTrace? stackTrace)
    {
        if (stackTrace == null)
        {
            return "No stack trace available.";
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Memory leak detected at: ");

        for (var i = 0; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var fileName = frame?.GetFileName();
            
            if (frame != null)
            {
                stringBuilder.AppendLine($"File: {fileName}, Method: {DiagnosticMethodInfo.Create(frame)?.Name}, Line: {frame.GetFileLineNumber()}");
            }
        }

        return stringBuilder.ToString();
    }
}