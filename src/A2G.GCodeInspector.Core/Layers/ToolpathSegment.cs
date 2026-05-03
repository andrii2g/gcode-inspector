using A2G.GCodeInspector.Core.Geometry;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Core.Layers;

public sealed record ToolpathSegment(
    int LineNumber,
    int LayerIndex,
    double ZMm,
    FeatureType FeatureType,
    string? RawFeatureType,
    Point2D Start,
    Point2D End,
    double ExtrusionDeltaMm,
    double? FeedRateMmPerMinute,
    string? RawCommentContext)
{
    public double LengthMm => Point2D.Distance(Start, End);

    public double AngleDegreesNormalized0To180
    {
        get
        {
            var angle = Math.Atan2(End.YMm - Start.YMm, End.XMm - Start.XMm) * (180.0 / Math.PI);
            var normalized = angle % 180.0;
            return normalized < 0 ? normalized + 180.0 : normalized;
        }
    }

    public bool IsExtrusion => ExtrusionDeltaMm > 0;

    public bool IsTravel => ExtrusionDeltaMm <= 0;

    public bool IsBridgeFeature =>
        FeatureType is FeatureType.BridgeInfill or FeatureType.InternalBridgeInfill;
}
