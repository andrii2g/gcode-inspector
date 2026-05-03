using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Tests.Slicer;

public sealed class SlicerCommentClassifierTests
{
    private readonly SlicerCommentClassifier _classifier = new();

    [Fact]
    public void KnownTypeCommentIsClassifiedCaseInsensitively()
    {
        var classified = _classifier.TryClassifyFeatureType(
            "type:Support material interface",
            out var featureType,
            out var rawFeatureType,
            out var slicerFlavor);

        Assert.True(classified);
        Assert.Equal(FeatureType.SupportMaterialInterface, featureType);
        Assert.Equal("Support material interface", rawFeatureType);
        Assert.Equal(SlicerFlavor.Slic3rStyle, slicerFlavor);
    }

    [Fact]
    public void UnknownTypeFallsBackToUnknownAndPreservesRawText()
    {
        var classified = _classifier.TryClassifyFeatureType(
            "TYPE:  Custom Region  ",
            out var featureType,
            out var rawFeatureType,
            out var slicerFlavor);

        Assert.True(classified);
        Assert.Equal(FeatureType.Unknown, featureType);
        Assert.Equal("Custom Region", rawFeatureType);
        Assert.Equal(SlicerFlavor.Slic3rStyle, slicerFlavor);
    }

    [Fact]
    public void NonTypeCommentIsNotClassified()
    {
        var classified = _classifier.TryClassifyFeatureType(
            "LAYER:42",
            out var featureType,
            out var rawFeatureType,
            out var slicerFlavor);

        Assert.False(classified);
        Assert.Equal(FeatureType.Unknown, featureType);
        Assert.Null(rawFeatureType);
        Assert.Equal(SlicerFlavor.Unknown, slicerFlavor);
    }
}
