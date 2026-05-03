using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Core.Layers;

public sealed record PrintLayer(
    int LayerIndex,
    int? SlicerLayerIndex,
    double ZMm,
    double? SlicerZMm,
    double? HeightMm,
    IReadOnlyList<ToolpathSegment> Segments)
{
    public IReadOnlyList<ToolpathSegment> ExtrusionSegments =>
        Segments.Where(segment => segment.IsExtrusion).ToArray();

    public IReadOnlyList<ToolpathSegment> BridgeFeatureSegments =>
        Segments.Where(segment => segment.IsExtrusion && segment.IsBridgeFeature).ToArray();

    public IReadOnlyList<ToolpathSegment> SupportSegments =>
        Segments.Where(
                segment => segment.IsExtrusion
                    && segment.FeatureType is FeatureType.SupportMaterial or FeatureType.SupportMaterialInterface)
            .ToArray();

    public IReadOnlyList<ToolpathSegment> PerimeterSegments =>
        Segments.Where(
                segment => segment.IsExtrusion
                    && segment.FeatureType is FeatureType.Perimeter or FeatureType.ExternalPerimeter or FeatureType.OverhangPerimeter)
            .ToArray();
}
