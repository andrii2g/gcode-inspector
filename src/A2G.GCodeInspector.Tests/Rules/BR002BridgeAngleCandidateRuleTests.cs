using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Parsing;
using A2G.GCodeInspector.Core.Rules;

namespace A2G.GCodeInspector.Tests.Rules;

public sealed class BR002BridgeAngleCandidateRuleTests
{
    private readonly GCodeParser _parser = new();
    private readonly BR002BridgeAngleCandidateRule _rule = new();

    [Fact]
    public void Br002DoesNotRunWithoutBr001Findings()
    {
        var findings = Evaluate(
            """
            G92 X15 Y-20
            ;TYPE:Perimeter
            G1 X15 Y20 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X5 Y0 Z0.4 E2.0
            """);

        Assert.Empty(findings);
    }

    [Fact]
    public void Br002DoesNotEmitWhenImprovementIsBelowTwentyPercent()
    {
        var findings = Evaluate(
            """
            G92 X5 Y-20
            ;TYPE:Perimeter
            G1 X5 Y20 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X30 Y0 Z0.4 E2.0
            """,
            new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10));

        Assert.Empty(findings);
    }

    [Fact]
    public void Br002EmitsOneInfoFindingWhenBestCandidateImprovementMeetsThreshold()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X15 Y-20
                ;TYPE:Perimeter
                G1 X15 Y20 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10)));

        Assert.Equal(RiskSeverity.Info, finding.Severity);
        Assert.Equal("BR002", finding.RuleId);
    }

    [Fact]
    public void Br002ReturnsAtMostTwoCandidateAngles()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X15 Y-20
                ;TYPE:Perimeter
                G1 X15 Y20 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10, AngleStepDegrees: 15)));

        var candidateLine = finding.Evidence.Single(line => line.StartsWith("candidateAngles=", StringComparison.Ordinal));
        var values = candidateLine["candidateAngles=".Length..].Split(", ", StringSplitOptions.RemoveEmptyEntries);
        Assert.True(values.Length <= 2);
    }

    [Fact]
    public void Br002ExcludesAnglesWithinTenDegreesOfCurrentBridgeAngle()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X15 Y-20
                ;TYPE:Perimeter
                G1 X15 Y20 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10, AngleStepDegrees: 5)));

        var candidateLine = finding.Evidence.Single(line => line.StartsWith("candidateAngles=", StringComparison.Ordinal));
        Assert.DoesNotContain("0", candidateLine, StringComparison.Ordinal);
        Assert.DoesNotContain("5", candidateLine, StringComparison.Ordinal);
        Assert.DoesNotContain("10", candidateLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Br002TieBreaksBySmallerEstimatedSpanThenSmallerAngle()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X15 Y-20
                ;TYPE:Perimeter
                G1 X15 Y20 Z0.2 E1.0
                G92 X5 Y-20
                ;TYPE:Perimeter
                G1 X5 Y20 Z0.2 E2.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E3.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10, AngleStepDegrees: 45)));

        var candidateLine = finding.Evidence.Single(line => line.StartsWith("candidateAngles=", StringComparison.Ordinal));
        Assert.Contains("90, 135", candidateLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Br002RelatedFindingIdReferencesCorrectBr001Id()
    {
        var finding = Assert.Single(
            Evaluate(
                """
                G92 X15 Y-20
                ;TYPE:Perimeter
                G1 X15 Y20 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10)));

        Assert.Equal("BR001-0001", finding.RelatedFindingId);
    }

    [Fact]
    public void CandidateAnglesAreDeterministic()
    {
        var first = Assert.Single(
            Evaluate(
                """
                G92 X15 Y-20
                ;TYPE:Perimeter
                G1 X15 Y20 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10, AngleStepDegrees: 15)));

        var second = Assert.Single(
            Evaluate(
                """
                G92 X15 Y-20
                ;TYPE:Perimeter
                G1 X15 Y20 Z0.2 E1.0
                ;LAYER_CHANGE
                G92 X0 Y0
                ;TYPE:Bridge infill
                G1 X30 Y0 Z0.4 E2.0
                """,
                new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 10, AngleStepDegrees: 15)));

        Assert.Equal(first.Message, second.Message);
        Assert.Equal(first.Evidence, second.Evidence);
    }

    private IReadOnlyList<RiskFinding> Evaluate(string gcode, AnalysisOptions? options = null)
    {
        var layers = _parser.Parse(gcode);
        var index = new ToolpathIndex(layers);
        var context = new AnalysisContext(index, options ?? new AnalysisOptions());
        return _rule.Evaluate(context);
    }
}
