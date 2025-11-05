using Misaki.HighPerformance.LowLevel.Buffer;
using System.Diagnostics;
using System.Text;

namespace Misaki.HighPerformance.LowLevel;

/// <summary>
/// An exception that is thrown when a memory leak is detected.
/// </summary>
/// <param name="Infos">An array of AllocationInfo containing details about the memory leaks.</param>
public class MemoryLeakException : Exception
{
    private readonly IEnumerable<AllocationInfo>? _infos;
    private readonly string _message = string.Empty;

    public MemoryLeakException(IEnumerable<AllocationInfo> infos)
    {
        _infos = infos;
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
            if (frame != null)
            {
                stringBuilder.AppendLine($"File: {frame.GetFileName()}, Method: {DiagnosticMethodInfo.Create(frame)?.Name}, Line: {frame.GetFileLineNumber()}");
            }
        }

        return stringBuilder.ToString();
    }

    public override string Message
    {
        get
        {
            if (_infos == null)
            {
                return _message;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Found {_infos.Count()} memory lakes!");

            foreach (var info in _infos)
            {
                stringBuilder.AppendLine(GetMessage(info.StackTrace));
            }

            return stringBuilder.ToString();
        }
    }
}