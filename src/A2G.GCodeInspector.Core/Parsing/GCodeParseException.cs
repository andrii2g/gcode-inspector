namespace A2G.GCodeInspector.Core.Parsing;

public sealed class GCodeParseException : Exception
{
    public GCodeParseException(int lineNumber, string message)
        : base($"Parse error at line {lineNumber}: {message}")
    {
        LineNumber = lineNumber;
    }

    public int LineNumber { get; }
}
