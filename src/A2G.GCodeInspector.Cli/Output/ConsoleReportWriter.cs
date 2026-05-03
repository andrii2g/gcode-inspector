namespace A2G.GCodeInspector.Cli.Output;

public sealed class ConsoleReportWriter
{
    public void WritePlaceholder(string message)
    {
        Console.WriteLine(message);
    }
}
