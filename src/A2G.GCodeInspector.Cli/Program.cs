using A2G.GCodeInspector.Cli.Commands;

return args.FirstOrDefault() switch
{
    "analyze" => new AnalyzeCommand().Execute(args.Skip(1).ToArray()),
    "explain" => new ExplainCommand().Execute(args.Skip(1).ToArray()),
    _ => ShowUsage(),
};

static int ShowUsage()
{
    Console.Error.WriteLine("Usage: gcode-inspector <analyze|explain> [options]");
    return 64;
}
