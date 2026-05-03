using A2G.GCodeInspector.Core.Analysis;

namespace A2G.GCodeInspector.Cli.Commands;

public sealed record AnalyzeArguments(
    string FilePath,
    AnalyzeOutputFormat Format,
    string? OutputPath,
    FailOnMode FailOn,
    AnalysisOptions Options,
    bool Verbose);
