using A2G.GCodeInspector.Core.Geometry;

namespace A2G.GCodeInspector.Core.Parsing;

public sealed record GCodeMove(
    Point2D Start,
    Point2D End,
    double StartZMm,
    double EndZMm,
    double ExtrusionDeltaMm,
    double? FeedRateMmPerMinute);
