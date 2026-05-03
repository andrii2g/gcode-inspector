using A2G.GCodeInspector.Core.Geometry;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Tests.Geometry;

public sealed class SupportSamplerTests
{
    private readonly SupportSampler _sampler = new();

    [Fact]
    public void PointExactlyAtToleranceIsSupported()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 1, 10, 1)],
            supportToleranceMm: 1.0,
            sampleSpacingMm: 10.0);

        Assert.Equal(10.0, result.SupportedLengthMm);
        Assert.Equal(0.0, result.UnsupportedLengthMm);
    }

    [Fact]
    public void PointFartherThanToleranceIsUnsupported()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 1.01, 10, 1.01)],
            supportToleranceMm: 1.0,
            sampleSpacingMm: 10.0);

        Assert.Equal(0.0, result.SupportedLengthMm);
        Assert.Equal(10.0, result.UnsupportedLengthMm);
    }

    [Fact]
    public void EmptyPreviousLayerMeansAllIntervalsUnsupported()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [],
            supportToleranceMm: 0.45,
            sampleSpacingMm: 1.0);

        Assert.Equal(10.0, result.LongestUnsupportedSpanMm);
        Assert.Equal(0.0, result.SupportCoveragePercent);
    }

    [Fact]
    public void SingleSupportedIntervalSplitsUnsupportedRuns()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(4, 0, 6, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 2.0);

        Assert.Equal(4.0, result.LongestUnsupportedSpanMm);
        Assert.Equal(8.0, result.UnsupportedLengthMm);
    }

    [Fact]
    public void LongestUnsupportedSpanUsesIntervalLengthsNotSampleCount()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 0, 4, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 3.0);

        Assert.Equal(7.0, result.LongestUnsupportedSpanMm);
    }

    [Fact]
    public void UnsupportedSpanPositionIsMidpointOfLongestUnsupportedRun()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 0, 4, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 3.0);

        Assert.Equal(6.5, result.LongestUnsupportedSpanMidpoint.XMm, precision: 5);
        Assert.Equal(0.0, result.LongestUnsupportedSpanMidpoint.YMm, precision: 5);
    }

    [Fact]
    public void SampleSpacingLargerThanSegmentLengthCreatesOneInterval()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 2, 0),
            [],
            supportToleranceMm: 0.45,
            sampleSpacingMm: 10.0);

        Assert.Equal(2.0, result.LongestUnsupportedSpanMm);
        Assert.Equal(2.0, result.UnsupportedLengthMm);
    }

    [Fact]
    public void SupportCoverageIsLengthWeightedByIntervalLength()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 0, 5, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 3.0);

        Assert.Equal(60.0, result.SupportCoveragePercent, precision: 5);
        Assert.Equal(40.0, result.UnsupportedCoveragePercent, precision: 5);
    }

    [Fact]
    public void SupportCoverageUsesFinalShortIntervalActualLength()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 0, 5, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 6.0);

        Assert.Equal(6.0, result.SupportedLengthMm);
        Assert.Equal(4.0, result.UnsupportedLengthMm);
    }

    [Fact]
    public void SupportCoverageDoesNotUseEndpointCoverage()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 0, 0, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 10.0);

        Assert.Equal(0.0, result.SupportCoveragePercent);
    }

    [Fact]
    public void SupportCoverageDoesNotUseRawSampleCountCoverage()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 0, 5, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 6.0);

        Assert.NotEqual(50.0, result.SupportCoveragePercent);
        Assert.Equal(60.0, result.SupportCoveragePercent, precision: 5);
    }

    [Fact]
    public void SupportCoveragePercentCanBeExactlyFifty()
    {
        var result = _sampler.Sample(
            CreateSegment(0, 0, 10, 0),
            [CreateSegment(0, 0, 5, 0)],
            supportToleranceMm: 0.1,
            sampleSpacingMm: 5.0);

        Assert.Equal(50.0, result.SupportCoveragePercent, precision: 5);
    }

    private static ToolpathSegment CreateSegment(double startX, double startY, double endX, double endY)
    {
        return new ToolpathSegment(
            LineNumber: 1,
            LayerIndex: 1,
            ZMm: 0.2,
            FeatureType: FeatureType.Perimeter,
            RawFeatureType: null,
            Start: new Point2D(startX, startY),
            End: new Point2D(endX, endY),
            ExtrusionDeltaMm: 1.0,
            FeedRateMmPerMinute: 1200,
            RawCommentContext: null);
    }
}
