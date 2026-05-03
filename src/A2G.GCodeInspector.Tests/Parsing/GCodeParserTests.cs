using A2G.GCodeInspector.Core.Parsing;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Tests.Parsing;

public sealed class GCodeParserTests
{
    private readonly GCodeParser _parser = new();

    [Fact]
    public void OmittedAxesInheritModalState()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X10 Y10 Z0.2 E1.0 F1200
            G1 X20 E2.0
            """);

        var segment = Assert.Single(Assert.Single(layers).Segments.Skip(1));
        Assert.Equal(10, segment.Start.XMm);
        Assert.Equal(10, segment.Start.YMm);
        Assert.Equal(20, segment.End.XMm);
        Assert.Equal(10, segment.End.YMm);
        Assert.Equal(0.2, segment.ZMm);
        Assert.Equal(1200, segment.FeedRateMmPerMinute);
    }

    [Fact]
    public void AbsoluteXyzWithRelativeExtrusionIsSupported()
    {
        var layers = _parser.Parse(
            """
            M83
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E0.5
            G1 X10 Y0 E0.25
            """);

        var segments = Assert.Single(layers).Segments;
        Assert.Equal(0.5, segments[0].ExtrusionDeltaMm);
        Assert.Equal(0.25, segments[1].ExtrusionDeltaMm);
    }

    [Fact]
    public void RelativeXyzWithAbsoluteExtrusionIsSupported()
    {
        var layers = _parser.Parse(
            """
            G91
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            G1 X5 Y0 E2.0
            """);

        var segments = Assert.Single(layers).Segments;
        Assert.Equal(5, segments[0].End.XMm);
        Assert.Equal(10, segments[1].End.XMm);
        Assert.Equal(1.0, segments[0].ExtrusionDeltaMm);
        Assert.Equal(1.0, segments[1].ExtrusionDeltaMm);
    }

    [Fact]
    public void RelativeXyzWithRelativeExtrusionIsSupported()
    {
        var layers = _parser.Parse(
            """
            G91
            M83
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E0.5
            G1 X5 Y0 E0.25
            """);

        var segments = Assert.Single(layers).Segments;
        Assert.Equal(5, segments[0].End.XMm);
        Assert.Equal(10, segments[1].End.XMm);
        Assert.Equal(0.5, segments[0].ExtrusionDeltaMm);
        Assert.Equal(0.25, segments[1].ExtrusionDeltaMm);
    }

    [Fact]
    public void M82ToM83TransitionIsSupported()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            M83
            G1 X10 Y0 E0.5
            """);

        var segments = Assert.Single(layers).Segments;
        Assert.Equal(1.0, segments[0].ExtrusionDeltaMm);
        Assert.Equal(0.5, segments[1].ExtrusionDeltaMm);
    }

    [Fact]
    public void M83ToM82TransitionIsSupported()
    {
        var layers = _parser.Parse(
            """
            M83
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E0.5
            M82
            G92 E0
            G1 X10 Y0 E1.25
            """);

        var segments = Assert.Single(layers).Segments;
        Assert.Equal(0.5, segments[0].ExtrusionDeltaMm);
        Assert.Equal(1.25, segments[1].ExtrusionDeltaMm);
    }

    [Fact]
    public void G92ResetsExtrusionWithoutCreatingMovement()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            G92 E0
            G1 X10 Y0 E0.5
            """);

        var segments = Assert.Single(layers).Segments;
        Assert.Equal(2, segments.Count);
        Assert.Equal(0.5, segments[1].ExtrusionDeltaMm);
    }

    [Fact]
    public void G92UpdatesCoordinatesWithoutCreatingMovement()
    {
        var layers = _parser.Parse(
            """
            G92 X10 Y20 Z0.2
            ;TYPE:Perimeter
            G1 X20 Y25 E1.0
            """);

        var segment = Assert.Single(Assert.Single(layers).Segments);
        Assert.Equal(10, segment.Start.XMm);
        Assert.Equal(20, segment.Start.YMm);
        Assert.Equal(20, segment.End.XMm);
        Assert.Equal(25, segment.End.YMm);
    }

    [Fact]
    public void FeedrateIsInherited()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0 F1200
            G1 X10 Y0 E2.0
            """);

        var segment = Assert.Single(Assert.Single(layers).Segments.Skip(1));
        Assert.Equal(1200, segment.FeedRateMmPerMinute);
    }

    [Fact]
    public void InlineCommentsAreSupported()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Bridge infill
            G1 X5 Y0 Z0.2 E1.0 ; inline comment
            """);

        var segment = Assert.Single(Assert.Single(layers).Segments);
        Assert.Equal("inline comment", segment.RawCommentContext);
        Assert.Equal(FeatureType.BridgeInfill, segment.FeatureType);
    }

    [Fact]
    public void EmptyAndCommentOnlyLinesAreValid()
    {
        var layers = _parser.Parse(
            """

            ; comment only
            ;TYPE:Perimeter

            G1 X5 Y0 Z0.2 E1.0
            """);

        Assert.Single(layers);
    }

    [Fact]
    public void UnknownCommandsAreIgnored()
    {
        var layers = _parser.Parse(
            """
            T0
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            """);

        Assert.Single(layers);
    }

    [Fact]
    public void InvalidNumericTokenThrowsParseError()
    {
        var exception = Assert.Throws<GCodeParseException>(
            () => _parser.Parse(
                """
                G1 Xabc Y0
                """));

        Assert.Equal(1, exception.LineNumber);
        Assert.Contains("invalid numeric value for X", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void G20FollowedByMovementThrowsParseError()
    {
        var exception = Assert.Throws<GCodeParseException>(
            () => _parser.Parse(
                """
                G20
                G1 X5 Y0
                """));

        Assert.Equal(2, exception.LineNumber);
        Assert.Contains("inch mode", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void G21IsAccepted()
    {
        var layers = _parser.Parse(
            """
            G20
            G21
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            """);

        Assert.Single(layers);
    }

    [Fact]
    public void ZBasedLayerChangeWithoutCommentsIsSupported()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            G1 X10 Y0 E2.0
            G1 X15 Y0 Z0.4 E3.0
            """);

        Assert.Equal(2, layers.Count);
        Assert.Equal(0.2, layers[0].ZMm);
        Assert.Equal(0.4, layers[1].ZMm);
    }

    [Fact]
    public void CommentBasedLayerChangeWithLayerChangeCommentIsSupported()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            ;LAYER_CHANGE
            G1 X10 Y0 Z0.4 E2.0
            """);

        Assert.Equal(2, layers.Count);
        Assert.Equal(0.4, layers[1].ZMm);
    }

    [Fact]
    public void SlicerLayerMetadataIsPreserved()
    {
        var layers = _parser.Parse(
            """
            ;LAYER:42
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            """);

        Assert.Equal(42, Assert.Single(layers).SlicerLayerIndex);
    }

    [Fact]
    public void SlicerZMetadataIsPreservedWithoutOverridingLayerZ()
    {
        var layers = _parser.Parse(
            """
            ;LAYER_CHANGE
            ;Z:0.40
            ;TYPE:Perimeter
            G1 Z0.42 F1200
            G1 X10 Y10 E0.2
            """);

        var layer = Assert.Single(layers);
        Assert.Equal(0.42, layer.ZMm);
        Assert.Equal(0.40, layer.SlicerZMm);
    }

    [Fact]
    public void ZHopDoesNotCreateNewLayerWhenExtrusionResumesAtPreviousZ()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            G1 Z0.4 F1200
            G1 X10 Y0
            G1 Z0.2 F1200
            G1 X15 Y0 E2.0
            """);

        Assert.Single(layers);
        Assert.Equal(3, layers[0].Segments.Count);
        Assert.Equal(0.2, layers[0].Segments[^1].ZMm);
    }

    [Fact]
    public void KnownTypeCommentsAreMapped()
    {
        var layers = _parser.Parse(
            """
            ;type:Support material interface
            G1 X5 Y0 Z0.2 E1.0
            """);

        Assert.Equal(FeatureType.SupportMaterialInterface, Assert.Single(layers).Segments[0].FeatureType);
    }

    [Fact]
    public void UnknownTypeFallsBackToUnknownAndPreservesRawType()
    {
        var layers = _parser.Parse(
            """
            ;TYPE:Something custom
            G1 X5 Y0 Z0.2 E1.0
            """);

        var segment = Assert.Single(Assert.Single(layers).Segments);
        Assert.Equal(FeatureType.Unknown, segment.FeatureType);
        Assert.Equal("Something custom", segment.RawFeatureType);
    }
}
