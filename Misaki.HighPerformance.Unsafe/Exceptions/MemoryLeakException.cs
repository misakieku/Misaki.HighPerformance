using Misaki.HighPerformance.Unsafe.Services;
using System.Diagnostics;
using System.Text;

namespace Misaki.HighPerformance.Unsafe;

internal class MemoryLeakException(params AllocationInfo[] Infos) : Exception
{
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
                stringBuilder.AppendLine($"File: {frame.GetFileName()}, Line: {frame.GetFileLineNumber()}");
            }
        }

        return stringBuilder.ToString();
    }

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