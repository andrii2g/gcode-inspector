using A2G.GCodeInspector.Core.Analysis;

namespace A2G.GCodeInspector.Cli.Output;

public sealed class ConsoleReportWriter : IReportWriter
{
    public string Write(ReportRenderContext context)
    {
        var lines = new List<string>
        {
            "GCode Inspector analysis",
            $"File: {context.InputFileName}",
            $"Layers: {context.Report.Summary.Layers}",
            $"Segments: {context.Report.Summary.Segments}",
            $"Findings: {context.Report.Summary.Findings.Critical} critical, {context.Report.Summary.Findings.Warning} warning, {context.Report.Summary.Findings.Info} info",
        };

        foreach (var finding in context.Report.Findings)
        {
            lines.Add(string.Empty);
            lines.Add(FormatFinding(finding));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatFinding(RiskFinding finding)
    {
        var unsupportedSpanMm = finding.Metrics.TryGetValue("unsupportedSpanMm", out var span)
            ? span
            : 0;

        var suggestion = finding.Suggestions.FirstOrDefault() ?? "None";

        return string.Join(
            Environment.NewLine,
            $"[{finding.Severity.ToString().ToUpperInvariant()}] {finding.RuleId} {finding.Title}",
            $"Layer: {finding.LayerIndex}",
            $"Z: {finding.ZMm:0.##} mm",
            $"Line: {finding.LineNumberStart}",
            $"Position: X{finding.Position.XMm:0.00} Y{finding.Position.YMm:0.00} ({finding.PositionKind})",
            $"Metric: unsupportedSpanMm = {unsupportedSpanMm:0.00}",
            $"Suggestion: {suggestion}");
    }
}
