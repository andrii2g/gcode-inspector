using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Parsing;

namespace A2G.GCodeInspector.Tests.Layers;

public sealed class ToolpathIndexTests
{
    private readonly GCodeParser _parser = new();

    [Fact]
    public void ToolpathIndexFlattensLayersAndSegments()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            ;LAYER_CHANGE
            ;TYPE:Bridge infill
            G1 X10 Y0 Z0.4 E2.0
            """);

        var index = new ToolpathIndex(layers);

        Assert.Equal(2, index.Layers.Count);
        Assert.Equal(2, index.Segments.Count);
        Assert.Equal(2, index.ExtrusionSegments.Count);
        Assert.Equal(1, index.GetLayer(1)?.BridgeFeatureSegments.Count);
    }
}
