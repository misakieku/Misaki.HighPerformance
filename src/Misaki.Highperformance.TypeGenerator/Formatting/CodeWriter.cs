using System.Text;

namespace Misaki.Highperformance.TypeGenerator.Formatting;

public sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void WriteLine(string text = "")
    {
        if (string.IsNullOrEmpty(text))
        {
            _sb.AppendLine();
            return;
        }

        var lines = text.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                _sb.AppendLine();
            }
            else
            {
                _sb.Append(new string(' ', _indent * 4));
                _sb.AppendLine(line);
            }
        }
    }

    public void Indent() => _indent++;
    public void Unindent() => _indent = Math.Max(0, _indent - 1);

    public IDisposable Scope(string header, string open = "{", string close = "}")
    {
        if (!string.IsNullOrWhiteSpace(header))
        {
            WriteLine(header);
        }
        WriteLine(open);
        _indent++;
        return new ScopeDisposable(() =>
        {
            _indent = Math.Max(0, _indent - 1);
            WriteLine(close);
        });
    }

    public IDisposable Region(string name)
    {
        WriteLine($"#region {name}");
        return new ScopeDisposable(() =>
        {
            WriteLine("#endregion");
            WriteLine();
        });
    }

    public override string ToString() => _sb.ToString();

    private sealed class ScopeDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
