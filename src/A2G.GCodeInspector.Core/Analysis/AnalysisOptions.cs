namespace A2G.GCodeInspector.Core.Analysis;

public sealed record AnalysisOptions(
    double MaxBridgeSpanWarningMm = 25.0,
    double MaxBridgeSpanCriticalMm = 50.0,
    double SupportToleranceMm = 0.45,
    double SampleSpacingMm = 1.0,
    bool DetectHeuristicBridges = true);
