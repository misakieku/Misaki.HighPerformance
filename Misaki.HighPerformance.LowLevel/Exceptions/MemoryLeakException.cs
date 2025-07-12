using Misaki.HighPerformance.LowLevel.Buffer;
using System.Diagnostics;
using System.Text;

namespace Misaki.HighPerformance.LowLevel.Exceptions;


public class MemoryLeakException(params AllocationManager.AllocationInfo[] Infos) : Exception
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
                stringBuilder.AppendLine($"File: {frame.GetFileName()}, Method: {DiagnosticMethodInfo.Create(frame)?.Name}, Line: {frame.GetFileLineNumber()}");
            }
        }

        return stringBuilder.ToString();
    }

    public override string Message
    {
        get
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Found {Infos.Length} memory lakes!");
            foreach (var info in Infos)
            {
                stringBuilder.AppendLine(GetMessage(info.StackTrace));
            }

            return stringBuilder.ToString();
        }
    }
}