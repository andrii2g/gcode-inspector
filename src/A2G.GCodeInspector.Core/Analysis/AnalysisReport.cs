using A2G.GCodeInspector.Core.Layers;

namespace A2G.GCodeInspector.Core.Analysis;

public sealed record AnalysisReport(
    ToolpathIndex ToolpathIndex,
    AnalysisOptions Options,
    IReadOnlyList<RiskFinding> Findings)
{
    public AnalysisSummary Summary { get; } = AnalysisSummary.From(ToolpathIndex, Findings);
}

public sealed record AnalysisSummary(
    int Layers,
    int Segments,
    FindingSummary Findings)
{
    public static AnalysisSummary From(
        ToolpathIndex toolpathIndex,
        IReadOnlyList<RiskFinding> findings)
    {
        return new AnalysisSummary(
            Layers: toolpathIndex.Layers.Count,
            Segments: toolpathIndex.AllSegments.Count,
            Findings: FindingSummary.From(findings));
    }
}

public sealed record FindingSummary(
    int Total,
    int Critical,
    int Warning,
    int Info)
{
    public static FindingSummary From(IReadOnlyList<RiskFinding> findings)
    {
        var critical = findings.Count(finding => finding.Severity == RiskSeverity.Critical);
        var warning = findings.Count(finding => finding.Severity == RiskSeverity.Warning);
        var info = findings.Count(finding => finding.Severity == RiskSeverity.Info);

        return new FindingSummary(
            Total: findings.Count,
            Critical: critical,
            Warning: warning,
            Info: info);
    }
}
