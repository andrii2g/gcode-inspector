namespace A2G.GCodeInspector.Cli.Commands;

public sealed class ExplainCommand
{
    private readonly CliArgumentParser _argumentParser;
    private readonly TextWriter _standardOutput;
    private readonly TextWriter _standardError;

    public ExplainCommand()
        : this(new CliArgumentParser(), Console.Out, Console.Error)
    {
    }

    public ExplainCommand(
        CliArgumentParser argumentParser,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        _argumentParser = argumentParser;
        _standardOutput = standardOutput;
        _standardError = standardError;
    }

    public int Execute(string[] args)
    {
        ExplainArguments parsedArguments;

        try
        {
            parsedArguments = _argumentParser.ParseExplain(args);
        }
        catch (CliArgumentException exception)
        {
            _standardError.WriteLine(exception.Message);
            return 64;
        }

        return Execute(parsedArguments);
    }

    public int Execute(ExplainArguments arguments)
    {
        var content = arguments.RuleId switch
        {
            "BR001" => "BR001 Long unsupported bridge: warns when a bridge segment contains a long unsupported span.",
            "BR002" => "BR002 Bridge angle candidate: suggests alternative bridge angles after BR001 finds real bridge risk.",
            _ => null,
        };

        if (content is null)
        {
            _standardError.WriteLine($"Unknown rule id: {arguments.RuleId}");
            return 64;
        }

        _standardOutput.WriteLine(content);
        return 0;
    }
}
