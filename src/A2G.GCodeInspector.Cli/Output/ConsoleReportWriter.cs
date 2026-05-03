using A2G.GCodeInspector.Core.Analysis;

namespace A2G.GCodeInspector.Cli.Output;

public sealed class ConsoleReportWriter : IReportWriter
{
    public string Write(ReportRenderContext context)
    {
        var lines = new List<string>
        {
            "GCode Inspector Report",
            $"Input: {context.InputPath}",
            $"Findings: {context.Report.Summary.Findings.Total} total",
            $"Critical: {context.Report.Summary.Findings.Critical}",
            $"Warning: {context.Report.Summary.Findings.Warning}",
            $"Info: {context.Report.Summary.Findings.Info}",
        };

        foreach (var finding in context.Report.Findings)
        {
            lines.Add(FormatFinding(finding));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatFinding(RiskFinding finding)
    {
        return $"[{finding.Severity}] {finding.RuleId} {finding.Title} at layer {finding.LayerIndex} line {finding.LineNumberStart}";
    }
}
