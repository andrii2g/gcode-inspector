namespace A2G.GCodeInspector.Cli;

public sealed class CliArgumentException : Exception
{
    public CliArgumentException(string message)
        : base(message)
    {
    }
}
