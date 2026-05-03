using System.Globalization;
using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Geometry;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Core.Rules;

public sealed class BR001LongUnsupportedBridgeRule : IGCodeRule
{
    private static readonly IReadOnlyList<string> Suggestions =
    [
        "Try changing the bridge angle for this region.",
        "Add local support or support enforcers.",
        "Add sacrificial internal walls or ribs to shorten the span.",
        "Reduce bridge speed and verify bridge flow/fan settings.",
        "Consider splitting the model or changing orientation.",
    ];

    private readonly SupportSampler _supportSampler = new();

    public IReadOnlyList<RiskFinding> Evaluate(AnalysisContext context)
    {
        var findings = new List<RiskFinding>();

        foreach (var layer in context.ToolpathIndex.Layers)
        {
            if (layer.LayerIndex == 0)
            {
                continue;
            }

            var previousLayer = context.ToolpathIndex.GetLayer(layer.LayerIndex - 1);
            var previousMaterial = previousLayer?.ExtrusionSegments ?? [];

            foreach (var candidate in layer.ExtrusionSegments.Where(segment => IsCandidate(segment, previousMaterial, context.Options)))
            {
                var sampleResult = _supportSampler.Sample(
                    candidate,
                    previousMaterial,
                    context.Options.SupportToleranceMm,
                    context.Options.SampleSpacingMm);

                if (sampleResult.LongestUnsupportedSpanMm < context.Options.MaxBridgeSpanWarningMm)
                {
                    continue;
                }

                var severity = sampleResult.LongestUnsupportedSpanMm >= context.Options.MaxBridgeSpanCriticalMm
                    ? RiskSeverity.Critical
                    : RiskSeverity.Warning;

                findings.Add(CreateFinding(candidate, sampleResult, context.Options, severity, findings.Count + 1));
            }
        }

        return findings;
    }

    private bool IsCandidate(
        ToolpathSegment segment,
        IReadOnlyList<ToolpathSegment> previousMaterial,
        AnalysisOptions options)
    {
        if (segment.IsBridgeFeature)
        {
            return true;
        }

        if (!options.DetectHeuristicBridges)
        {
            return false;
        }

        if (segment.LengthMm < options.MaxBridgeSpanWarningMm)
        {
            return false;
        }

        if (segment.FeatureType is FeatureType.Perimeter or FeatureType.ExternalPerimeter)
        {
            return false;
        }

        if (segment.FeatureType is not (FeatureType.Unknown or FeatureType.SolidInfill or FeatureType.TopSolidInfill or FeatureType.InternalInfill))
        {
            return false;
        }

        var supportResult = _supportSampler.Sample(
            segment,
            previousMaterial,
            options.SupportToleranceMm,
            options.SampleSpacingMm);

        return supportResult.SupportCoveragePercent < 50.0;
    }

    private static RiskFinding CreateFinding(
        ToolpathSegment candidate,
        SupportSampleResult sampleResult,
        AnalysisOptions options,
        RiskSeverity severity,
        int sequenceNumber)
    {
        var metrics = new Dictionary<string, double>
        {
            ["segmentLengthMm"] = candidate.LengthMm,
            ["supportCoveragePercent"] = sampleResult.SupportCoveragePercent,
            ["supportedLengthMm"] = sampleResult.SupportedLengthMm,
            ["thresholdCriticalMm"] = options.MaxBridgeSpanCriticalMm,
            ["thresholdWarningMm"] = options.MaxBridgeSpanWarningMm,
            ["unsupportedCoveragePercent"] = sampleResult.UnsupportedCoveragePercent,
            ["unsupportedSpanMm"] = sampleResult.LongestUnsupportedSpanMm,
        };

        var evidence = new List<string>
        {
            $"featureType={candidate.FeatureType}",
            $"sampleSpacingMm={options.SampleSpacingMm.ToString(CultureInfo.InvariantCulture)}",
            $"supportToleranceMm={options.SupportToleranceMm.ToString(CultureInfo.InvariantCulture)}",
        };

        return new RiskFinding(
            Id: $"BR001-{sequenceNumber:0000}",
            RuleId: "BR001",
            Severity: severity,
            Title: "Long unsupported bridge",
            Message: $"Layer {candidate.LayerIndex} at Z={candidate.ZMm:0.##} mm contains a bridge segment with approximately {sampleResult.LongestUnsupportedSpanMm:0.##} mm unsupported span.",
            LayerIndex: candidate.LayerIndex,
            ZMm: candidate.ZMm,
            LineNumberStart: candidate.LineNumber,
            LineNumberEnd: candidate.LineNumber,
            Position: sampleResult.LongestUnsupportedSpanMidpoint,
            PositionKind: "unsupportedSpanMidpoint",
            Metrics: metrics,
            Suggestions: Suggestions,
            Evidence: evidence,
            RelatedFindingId: null);
    }
}
