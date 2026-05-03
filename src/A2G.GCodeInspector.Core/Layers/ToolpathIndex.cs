namespace A2G.GCodeInspector.Core.Layers;

public sealed class ToolpathIndex
{
    public ToolpathIndex(IReadOnlyList<PrintLayer> layers)
    {
        Layers = layers;
        Segments = layers.SelectMany(layer => layer.Segments).ToArray();
        ExtrusionSegments = Segments.Where(segment => segment.IsExtrusion).ToArray();
    }

    public IReadOnlyList<PrintLayer> Layers { get; }

    public IReadOnlyList<ToolpathSegment> Segments { get; }

    public IReadOnlyList<ToolpathSegment> ExtrusionSegments { get; }

    public PrintLayer? GetLayer(int layerIndex)
    {
        return Layers.FirstOrDefault(layer => layer.LayerIndex == layerIndex);
    }
}
