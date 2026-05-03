using A2G.GCodeInspector.Core.Layers;

namespace A2G.GCodeInspector.Core.Analysis;

public sealed record AnalysisContext(
    ToolpathIndex ToolpathIndex,
    AnalysisOptions Options);
