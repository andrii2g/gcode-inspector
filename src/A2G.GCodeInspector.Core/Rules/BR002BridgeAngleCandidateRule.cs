using System.Globalization;
using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Geometry;
using A2G.GCodeInspector.Core.Layers;

namespace A2G.GCodeInspector.Core.Rules;

public sealed class BR002BridgeAngleCandidateRule : IGCodeRule
{
    private readonly BR001LongUnsupportedBridgeRule _bridgeRule = new();
    private readonly SupportSampler _supportSampler = new();

    public IReadOnlyList<RiskFinding> Evaluate(AnalysisContext context)
    {
        var bridgeFindings = _bridgeRule.Evaluate(context);
        var findings = new List<RiskFinding>();

        foreach (var bridgeFinding in bridgeFindings)
        {
            if (!bridgeFinding.Metrics.TryGetValue("unsupportedSpanMm", out var currentUnsupportedSpanMm)
                || currentUnsupportedSpanMm <= 0)
            {
                continue;
            }

            var previousLayer = context.ToolpathIndex.GetLayer(bridgeFinding.LayerIndex - 1);
            if (previousLayer is null || previousLayer.ExtrusionSegments.Count == 0)
            {
                continue;
            }

            var bridgeSegment = FindSegment(context.ToolpathIndex, bridgeFinding);
            if (bridgeSegment is null)
            {
                continue;
            }

            var candidateResults = EvaluateCandidates(
                bridgeSegment,
                bridgeFinding,
                previousLayer.ExtrusionSegments,
                currentUnsupportedSpanMm,
                context.Options);

            var eligibleCandidates = candidateResults
                .Where(candidate => candidate.ImprovementPercent >= context.Options.AngleMinImprovementPercent)
                .OrderBy(candidate => candidate.EstimatedUnsupportedSpanMm)
                .ThenBy(candidate => candidate.AngleDegrees)
                .Take(2)
                .ToArray();

            if (eligibleCandidates.Length == 0)
            {
                continue;
            }

            findings.Add(CreateFinding(bridgeFinding, bridgeSegment, eligibleCandidates, currentUnsupportedSpanMm, findings.Count + 1));
        }

        return findings;
    }

    private IReadOnlyList<CandidateResult> EvaluateCandidates(
        ToolpathSegment bridgeSegment,
        RiskFinding bridgeFinding,
        IReadOnlyList<ToolpathSegment> previousLayerSegments,
        double currentUnsupportedSpanMm,
        AnalysisOptions options)
    {
        var results = new List<CandidateResult>();
        var currentAngle = bridgeSegment.AngleDegreesNormalized0To180;

        foreach (var candidateAngle in GenerateCandidateAngles(options.AngleStepDegrees))
        {
            if (NormalizedAngleDifference(candidateAngle, currentAngle) <= 10.0)
            {
                continue;
            }

            var virtualSegment = BuildVirtualSegment(bridgeFinding.Position, bridgeSegment.LengthMm, candidateAngle);
            var sampleResult = _supportSampler.Sample(
                virtualSegment,
                previousLayerSegments,
                options.SupportToleranceMm,
                options.SampleSpacingMm);

            var improvementPercent = ((currentUnsupportedSpanMm - sampleResult.LongestUnsupportedSpanMm) / currentUnsupportedSpanMm) * 100.0;
            results.Add(new CandidateResult(candidateAngle, sampleResult.LongestUnsupportedSpanMm, improvementPercent));
        }

        return results;
    }

    private static RiskFinding CreateFinding(
        RiskFinding bridgeFinding,
        ToolpathSegment bridgeSegment,
        IReadOnlyList<CandidateResult> candidates,
        double currentUnsupportedSpanMm,
        int sequenceNumber)
    {
        var bestCandidate = candidates[0];
        var candidateAngles = string.Join(", ", candidates.Select(candidate => candidate.AngleDegrees.ToString("0.##", CultureInfo.InvariantCulture)));
        var evidence = new List<string>
        {
            $"candidateAngles={candidateAngles}",
        };

        if (candidates.Length > 1)
        {
            evidence.Add($"secondaryCandidateAngleDegrees={candidates[1].AngleDegrees.ToString("0.##", CultureInfo.InvariantCulture)}");
            evidence.Add($"secondaryEstimatedUnsupportedSpanMm={candidates[1].EstimatedUnsupportedSpanMm.ToString("0.##", CultureInfo.InvariantCulture)}");
        }

        var metrics = new Dictionary<string, double>
        {
            ["bestCandidateAngleDegrees"] = bestCandidate.AngleDegrees,
            ["bestEstimatedUnsupportedSpanMm"] = bestCandidate.EstimatedUnsupportedSpanMm,
            ["currentAngleDegrees"] = bridgeSegment.AngleDegreesNormalized0To180,
            ["currentUnsupportedSpanMm"] = currentUnsupportedSpanMm,
            ["improvementPercent"] = bestCandidate.ImprovementPercent,
        };

        return new RiskFinding(
            Id: $"BR002-{sequenceNumber:0000}",
            RuleId: "BR002",
            Severity: RiskSeverity.Info,
            Title: "Bridge angle candidate",
            Message: $"The current bridge direction is approximately {bridgeSegment.AngleDegreesNormalized0To180:0.##} degrees. Candidate bridge angle(s) {candidateAngles} may reduce the estimated unsupported span from {currentUnsupportedSpanMm:0.##} mm to about {bestCandidate.EstimatedUnsupportedSpanMm:0.##} mm.",
            LayerIndex: bridgeFinding.LayerIndex,
            ZMm: bridgeFinding.ZMm,
            LineNumberStart: bridgeFinding.LineNumberStart,
            LineNumberEnd: bridgeFinding.LineNumberEnd,
            Position: bridgeFinding.Position,
            PositionKind: "relatedUnsupportedSpanMidpoint",
            Metrics: metrics,
            Suggestions: ["Try the candidate bridge angle in the slicer and inspect the regenerated G-code before printing."],
            Evidence: evidence,
            RelatedFindingId: bridgeFinding.Id);
    }

    private static ToolpathSegment BuildVirtualSegment(Point2D center, double lengthMm, double angleDegrees)
    {
        var radians = angleDegrees * (Math.PI / 180.0);
        var halfLength = lengthMm / 2.0;
        var deltaX = Math.Cos(radians) * halfLength;
        var deltaY = Math.Sin(radians) * halfLength;

        return new ToolpathSegment(
            LineNumber: 0,
            LayerIndex: 0,
            ZMm: 0,
            FeatureType: default,
            RawFeatureType: null,
            Start: new Point2D(center.XMm - deltaX, center.YMm - deltaY),
            End: new Point2D(center.XMm + deltaX, center.YMm + deltaY),
            ExtrusionDeltaMm: 1.0,
            FeedRateMmPerMinute: null,
            RawCommentContext: null);
    }

    private static ToolpathSegment? FindSegment(ToolpathIndex toolpathIndex, RiskFinding bridgeFinding)
    {
        return toolpathIndex
            .GetLayer(bridgeFinding.LayerIndex)?
            .Segments
            .FirstOrDefault(segment => segment.LineNumber == bridgeFinding.LineNumberStart);
    }

    private static IEnumerable<double> GenerateCandidateAngles(double stepDegrees)
    {
        for (var angle = 0.0; angle < 180.0; angle += stepDegrees)
        {
            yield return angle;
        }
    }

    private static double NormalizedAngleDifference(double candidateAngle, double currentAngle)
    {
        var diff = Math.Abs(candidateAngle - currentAngle);
        return Math.Min(diff, 180.0 - diff);
    }

    private sealed record CandidateResult(double AngleDegrees, double EstimatedUnsupportedSpanMm, double ImprovementPercent);
}
