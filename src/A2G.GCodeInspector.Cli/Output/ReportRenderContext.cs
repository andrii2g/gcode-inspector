using A2G.GCodeInspector.Cli.Commands;
using A2G.GCodeInspector.Core.Analysis;

namespace A2G.GCodeInspector.Cli.Output;

public sealed record ReportRenderContext(
    string InputFileName,
    string InputPath,
    long InputSizeBytes,
    AnalyzeArguments Arguments,
    AnalysisReport Report);
