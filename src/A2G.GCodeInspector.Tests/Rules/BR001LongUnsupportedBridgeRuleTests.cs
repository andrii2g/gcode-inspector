using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Parsing;
using A2G.GCodeInspector.Core.Rules;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Tests.Rules;

public sealed class BR001LongUnsupportedBridgeRuleTests
{
    private readonly GCodeParser _parser = new();
    private readonly BR001LongUnsupportedBridgeRule _rule = new();

    [Fact]
    public void LayerZeroIsNotAnalyzedAsBridgeRisk()
    {
        var findings = Evaluate(
            """
            ;TYPE:Bridge infill
            G1 X30 Y0 Z0.2 E1.0
            """);

        Assert.Empty(findings);
    }

    [Fact]
    public void SupportedBridgeEmitsNoFinding()
    {
        var findings = Evaluate(
            """
            ;TYPE:Perimeter
            G1 X30 Y0 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X30 Y0 Z0.4 E2.0
            """);

        Assert.Empty(findings);
    }

    [Fact]
    public void UnsupportedSpanBelowThresholdEmitsNoFinding()
    {
        var findings = Evaluate(
            """
            G92 X0 Y10
            ;TYPE:Perimeter
            G1 X5 Y10 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X24.99 Y0 Z0.4 E2.0
            """);

        Assert.Empty(findings);
    }

    [Fact]
    public void UnsupportedSpanAtWarningThresholdEmitsWarning()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                ;TYPE:Perimeter
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X25 Y0 Z0.4 E2.0
                """));

        Assert.Equal(RiskSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void UnsupportedSpanAtCriticalThresholdEmitsCritical()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                ;TYPE:Perimeter
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X50 Y0 Z0.4 E2.0
                """));

        Assert.Equal(RiskSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void ThresholdOverrideWorks()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                ;TYPE:Perimeter
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X20 Y0 Z0.4 E2.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 30)));

        Assert.Equal(RiskSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void FeatureTypeBridgeInfillIsAnalyzed()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                ;TYPE:Perimeter
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """));

        Assert.Equal("BR001", finding.RuleId);
    }

    [Fact]
    public void FeatureTypeInternalBridgeInfillIsAnalyzed()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                ;TYPE:Perimeter
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Internal bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """));

        Assert.Equal("BR001", finding.RuleId);
    }

    [Fact]
    public void HeuristicBridgeCandidateIsDetectedWhenCoverageBelowFiftyPercent()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                G1 X30 Y0 Z0.4 E2.0
                """));

        Assert.Equal(RiskSeverity.Warning, finding.Severity);
        Assert.True(finding.Metrics["supportCoveragePercent"] < 50.0);
    }

    [Fact]
    public void HeuristicBridgeCandidateIsNotDetectedAtExactlyFiftyPercentCoverage()
    {
        var findings = Evaluate(
            """
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Internal infill
            G1 X10 Y0 Z0.4 E2.0
            """,
            new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 5));

        Assert.Empty(findings);
    }

    [Fact]
    public void HeuristicCandidateIsNotEmittedForPerimeterOrExternalPerimeter()
    {
        var perimeterFindings = Evaluate(
            """
            G92 X0 Y10
            ;TYPE:Perimeter
            G1 X5 Y10 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Perimeter
            G1 X30 Y0 Z0.4 E2.0
            """);

        var externalPerimeterFindings = Evaluate(
            """
            G92 X0 Y10
            ;TYPE:Perimeter
            G1 X5 Y10 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:External perimeter
            G1 X30 Y0 Z0.4 E2.0
            """);

        Assert.Empty(perimeterFindings);
        Assert.Empty(externalPerimeterFindings);
    }

    [Fact]
    public void FindingPositionIsUnsupportedSpanMidpoint()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                ;TYPE:Perimeter
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """));

        Assert.Equal("unsupportedSpanMidpoint", finding.PositionKind);
        Assert.Equal(15.0, finding.Position.XMm, precision: 5);
    }

    [Fact]
    public void FindingContainsLayerActualZLineMetricsEvidenceAndSuggestions()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X0 Y10
                ;TYPE:Perimeter
                G1 X5 Y10 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """));

        Assert.Equal(1, finding.LayerIndex);
        Assert.Equal(0.4, finding.ZMm);
        Assert.Equal(finding.LineNumberStart, finding.LineNumberEnd);
        Assert.Contains("unsupportedSpanMm", finding.Metrics.Keys);
        Assert.Contains("supportCoveragePercent", finding.Metrics.Keys);
        Assert.NotEmpty(finding.Evidence);
        Assert.NotEmpty(finding.Suggestions);
    }

    private IReadOnlyList<RiskFinding> Evaluate(string gcode, AnalysisOptions? options = null)
    {
        var layers = _parser.Parse(gcode);
        var index = new ToolpathIndex(layers);
        var context = new AnalysisContext(index, options ?? new AnalysisOptions());
        return _rule.Evaluate(context);
    }
}
