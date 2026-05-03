namespace A2G.GCodeInspector.Core.Layers;

public sealed record PrintLayer(
    int LayerIndex,
    int? SlicerLayerIndex,
    double ZMm,
    double? SlicerZMm,
    double? HeightMm,
    IReadOnlyList<ToolpathSegment> Segments);
