#if DEBUG
using System.Diagnostics;
using System.Text;
#endif

namespace Misaki.HighPerformance.Unsafe;

public readonly struct MemoryLeakExceptionInfo
{
    public nuint Size
    {
        get; init;
    }
#if DEBUG
    public StackTrace StackTrace
    {
        get; init;
    }
#endif
}

public class MemoryLeakException(params MemoryLeakExceptionInfo[] Infos) : Exception
{
#if DEBUG
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
#endif

    public override string Message
    {
        get
        {
#if DEBUG
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Found {Infos.Length} memory lakes!");
            foreach (var info in Infos)
            {
                stringBuilder.AppendLine(GetMessage(info.StackTrace));
            }

            return stringBuilder.ToString();
#else
            return $"There are still {Infos.Length} buffers that hold {Infos.Sum(i => (uint)i.Size)} bytes in total are not freed yet. Please free them before disposing. Switch to debug mode for more information.";
#endif
        }
    }
}