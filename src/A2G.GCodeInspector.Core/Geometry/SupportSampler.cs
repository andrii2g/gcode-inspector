using A2G.GCodeInspector.Core.Layers;

namespace A2G.GCodeInspector.Core.Geometry;

public sealed class SupportSampler
{
    public SupportSampleResult Sample(
        ToolpathSegment candidateSegment,
        IReadOnlyList<ToolpathSegment> previousLayerSegments,
        double supportToleranceMm,
        double sampleSpacingMm)
    {
        var candidate = new Segment2D(candidateSegment.Start, candidateSegment.End);
        var length = candidate.LengthMm;

        if (length <= 0)
        {
            return new SupportSampleResult(
                0,
                candidate.Start,
                0,
                0,
                0,
                0);
        }

        var intervals = BuildIntervals(length, sampleSpacingMm);
        var previousSegments = previousLayerSegments
            .Where(segment => segment.IsExtrusion)
            .Select(segment => new Segment2D(segment.Start, segment.End))
            .ToArray();

        var supportedLength = 0.0;
        var unsupportedLength = 0.0;
        var currentUnsupportedRun = 0.0;
        var currentUnsupportedStart = 0.0;
        var longestUnsupportedRun = 0.0;
        var longestUnsupportedMidpointDistance = 0.0;

        foreach (var interval in intervals)
        {
            var midpointDistance = interval.StartDistanceMm + (interval.LengthMm / 2.0);
            var samplePoint = candidate.PointAtDistance(midpointDistance);
            var isSupported = IsSupported(samplePoint, previousSegments, supportToleranceMm);

            if (isSupported)
            {
                supportedLength += interval.LengthMm;

                if (currentUnsupportedRun > longestUnsupportedRun)
                {
                    longestUnsupportedRun = currentUnsupportedRun;
                    longestUnsupportedMidpointDistance = currentUnsupportedStart + (currentUnsupportedRun / 2.0);
                }

                currentUnsupportedRun = 0;
                currentUnsupportedStart = interval.EndDistanceMm;
                continue;
            }

            unsupportedLength += interval.LengthMm;
            if (currentUnsupportedRun == 0)
            {
                currentUnsupportedStart = interval.StartDistanceMm;
            }

            currentUnsupportedRun += interval.LengthMm;
        }

        if (currentUnsupportedRun > longestUnsupportedRun)
        {
            longestUnsupportedRun = currentUnsupportedRun;
            longestUnsupportedMidpointDistance = currentUnsupportedStart + (currentUnsupportedRun / 2.0);
        }

        var supportCoveragePercent = previousSegments.Length == 0
            ? 0
            : (supportedLength / length) * 100.0;
        var unsupportedCoveragePercent = previousSegments.Length == 0
            ? 100.0
            : (unsupportedLength / length) * 100.0;

        return new SupportSampleResult(
            longestUnsupportedRun,
            candidate.PointAtDistance(longestUnsupportedMidpointDistance),
            supportedLength,
            unsupportedLength,
            supportCoveragePercent,
            unsupportedCoveragePercent);
    }

    private static bool IsSupported(
        Point2D samplePoint,
        IReadOnlyList<Segment2D> previousSegments,
        double supportToleranceMm)
    {
        if (previousSegments.Count == 0)
        {
            return false;
        }

        foreach (var previousSegment in previousSegments)
        {
            if (GeometryMath.DistancePointToSegment(samplePoint, previousSegment) <= supportToleranceMm)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<SampleInterval> BuildIntervals(double lengthMm, double sampleSpacingMm)
    {
        var intervals = new List<SampleInterval>();
        if (lengthMm < sampleSpacingMm)
        {
            intervals.Add(new SampleInterval(0, lengthMm));
            return intervals;
        }

        var startDistance = 0.0;
        while (startDistance < lengthMm)
        {
            var endDistance = Math.Min(startDistance + sampleSpacingMm, lengthMm);
            intervals.Add(new SampleInterval(startDistance, endDistance));
            startDistance = endDistance;
        }

        return intervals;
    }

    private sealed record SampleInterval(double StartDistanceMm, double EndDistanceMm)
    {
        public double LengthMm => EndDistanceMm - StartDistanceMm;
    }
}
