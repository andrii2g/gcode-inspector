using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Parsing;

namespace A2G.GCodeInspector.Tests.Analysis;

public sealed class GCodeAnalyzerTests
{
    private readonly GCodeParser _parser = new();
    private readonly GCodeAnalyzer _analyzer = new();

    [Fact]
    public void AnalyzeReturnsBr001AndBr002FindingsForRiskyBridgeFixture()
    {
        var report = Analyze(
            """
            G92 X15 Y-20
            ;TYPE:Perimeter
            G1 X15 Y20 Z0.2 E1.0
            G92 X0 Y15
            ;TYPE:Perimeter
            G1 X30 Y-15 Z0.2 E2.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X30 Y0 Z0.4 E3.0
            """,
            new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 12, AngleStepDegrees: 45));

        Assert.Collection(
            report.Findings,
            finding =>
            {
                Assert.Equal("BR001", finding.RuleId);
                Assert.Equal(RiskSeverity.Warning, finding.Severity);
            },
            finding =>
            {
                Assert.Equal("BR002", finding.RuleId);
                Assert.Equal(RiskSeverity.Info, finding.Severity);
                Assert.Equal("BR001-0001", finding.RelatedFindingId);
            });
    }

    [Fact]
    public void AnalyzeSortsFindingsDeterministicallyWithinSameSegment()
    {
        var report = Analyze(
            """
            G92 X15 Y-20
            ;TYPE:Perimeter
            G1 X15 Y20 Z0.2 E1.0
            G92 X0 Y15
            ;TYPE:Perimeter
            G1 X30 Y-15 Z0.2 E2.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X30 Y0 Z0.4 E3.0
            """,
            new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 12, AngleStepDegrees: 15));

        Assert.Equal(["BR001", "BR002"], report.Findings.Select(finding => finding.RuleId));
    }

    [Fact]
    public void AnalyzeComputesSummaryCountsFromToolpathAndFindings()
    {
        var report = Analyze(
            """
            G92 X15 Y-20
            ;TYPE:Perimeter
            G1 X15 Y20 Z0.2 E1.0
            G92 X0 Y15
            ;TYPE:Perimeter
            G1 X30 Y-15 Z0.2 E2.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X30 Y0 Z0.4 E3.0
            """,
            new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 50, SampleSpacingMm: 12, AngleStepDegrees: 45));

        Assert.Equal(2, report.Summary.Layers);
        Assert.Equal(3, report.Summary.Segments);
        Assert.Equal(2, report.Summary.Findings.Total);
        Assert.Equal(0, report.Summary.Findings.Critical);
        Assert.Equal(1, report.Summary.Findings.Warning);
        Assert.Equal(1, report.Summary.Findings.Info);
    }

    [Fact]
    public void AnalyzePreservesResolvedAnalysisOptionsOnReport()
    {
        var options = new AnalysisOptions(
            MaxBridgeSpanWarningMm: 11,
            MaxBridgeSpanCriticalMm: 33,
            SupportToleranceMm: 0.5,
            SampleSpacingMm: 12,
            DetectHeuristicBridges: false,
            AngleStepDegrees: 30,
            AngleMinImprovementPercent: 25);

        var report = Analyze(
            """
            G92 X15 Y-20
            ;TYPE:Perimeter
            G1 X15 Y20 Z0.2 E1.0
            ;LAYER_CHANGE
            G92 X0 Y0
            ;TYPE:Bridge infill
            G1 X30 Y0 Z0.4 E2.0
            """,
            options);

        Assert.Equal(options, report.Options);
    }

    private AnalysisReport Analyze(string gcode, AnalysisOptions options)
    {
        var layers = _parser.Parse(gcode);
        var toolpathIndex = new ToolpathIndex(layers);
        return _analyzer.Analyze(toolpathIndex, options);
    }
}
