using A2G.GCodeInspector.Core.Geometry;

namespace A2G.GCodeInspector.Core.Analysis;

public sealed record RiskFinding(
    string Id,
    string RuleId,
    RiskSeverity Severity,
    string Title,
    string Message,
    int LayerIndex,
    double ZMm,
    int LineNumberStart,
    int LineNumberEnd,
    Point2D Position,
    string PositionKind,
    IReadOnlyDictionary<string, double> Metrics,
    IReadOnlyList<string> Suggestions,
    IReadOnlyList<string> Evidence,
    string? RelatedFindingId);
