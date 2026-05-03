namespace A2G.GCodeInspector.Core.Geometry;

public sealed record SupportSampleResult(
    double LongestUnsupportedSpanMm,
    Point2D LongestUnsupportedSpanMidpoint,
    double SupportedLengthMm,
    double UnsupportedLengthMm,
    double SupportCoveragePercent,
    double UnsupportedCoveragePercent);
