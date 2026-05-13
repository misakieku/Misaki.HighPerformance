using Misaki.HighPerformance.LowLevel.Buffer;
using System.Diagnostics;
using System.Text;

namespace Misaki.HighPerformance.LowLevel;

/// <summary>
/// An exception that is thrown when a memory leak is detected.
/// </summary>
public class MemoryLeakException : Exception
{
    private readonly string _message;

    public override string Message => _message;

    public MemoryLeakException(IEnumerable<AllocationInfo> infos)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Found {infos.Count()} memory lakes!");

#if MHP_ENABLE_STACKTRACE
        foreach (var info in infos)
        {
            stringBuilder.AppendLine();
            GetMessage(stringBuilder, info.StackTrace);
        }
#else
        stringBuilder.AppendLine("No stack trace information available. Please enable MHP_ENABLE_STACKTRACE for detailed leak information.");
#endif

        _message = stringBuilder.ToString();
    }

    public MemoryLeakException(string message)
    {
        _message = message;
    }

    private static void GetMessage(StringBuilder stringBuilder, StackTrace? stackTrace)
    {
        if (stackTrace == null)
        {
            stringBuilder.AppendLine("No stack trace available.");
            return;
        }

        stringBuilder.AppendLine("Memory leak detected at: ");

        for (var i = 0; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var fileName = frame?.GetFileName();

            if (frame != null)
            {
                var methodInfo = DiagnosticMethodInfo.Create(frame);
                stringBuilder.AppendLine($"at {methodInfo?.DeclaringTypeName}.{methodInfo?.ToString()} in {fileName}: line {frame.GetFileLineNumber()}");
            }
        }
    }
}