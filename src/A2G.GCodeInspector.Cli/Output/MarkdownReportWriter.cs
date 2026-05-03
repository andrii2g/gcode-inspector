namespace A2G.GCodeInspector.Cli.Output;

public sealed class MarkdownReportWriter : IReportWriter
{
    public string Write(ReportRenderContext context)
    {
        var lines = new List<string>
        {
            "# GCode Inspector Report",
            string.Empty,
            "## Summary",
            string.Empty,
            "| Metric | Value |",
            "| --- | ---: |",
            $"| File | `{context.InputFileName}` |",
            $"| Layers | {context.Report.Summary.Layers} |",
            $"| Segments | {context.Report.Summary.Segments} |",
            $"| Critical | {context.Report.Summary.Findings.Critical} |",
            $"| Warning | {context.Report.Summary.Findings.Warning} |",
            $"| Info | {context.Report.Summary.Findings.Info} |",
            string.Empty,
            "## Findings By Severity",
            string.Empty,
            $"Critical: {context.Report.Summary.Findings.Critical}",
            $"Warning: {context.Report.Summary.Findings.Warning}",
            $"Info: {context.Report.Summary.Findings.Info}",
            string.Empty,
            "## Findings By Layer",
        };

        foreach (var layerGroup in context.Report.Findings.GroupBy(finding => finding.LayerIndex))
        {
            lines.Add(string.Empty);
            lines.Add($"- Layer {layerGroup.Key}: {layerGroup.Count()} finding(s)");
        }

        lines.Add(string.Empty);
        lines.Add("## Detailed Findings");

        foreach (var finding in context.Report.Findings)
        {
            lines.Add(string.Empty);
            lines.Add($"### {finding.RuleId} {finding.Title}");
            lines.Add($"- Severity: `{finding.Severity.ToString().ToLowerInvariant()}`");
            lines.Add($"- Layer: {finding.LayerIndex}");
            lines.Add($"- Line: {finding.LineNumberStart}");
            lines.Add($"- Position: `X{finding.Position.XMm:0.00} Y{finding.Position.YMm:0.00}` ({finding.PositionKind})");
            lines.Add(finding.Message);
        }

        lines.Add(string.Empty);
        lines.Add("## Rule Explanations");
        lines.Add(string.Empty);
        lines.Add("- `BR001`: Long unsupported bridge.");
        lines.Add("- `BR002`: Bridge angle candidate.");
        lines.Add(string.Empty);
        lines.Add("## Limitations");
        lines.Add(string.Empty);
        lines.Add("- Support detection is segment-distance based, not exact bead-area reconstruction.");
        lines.Add("- Bridge angle candidates are heuristic and should be verified by regenerating G-code.");

        if (context.Report.Findings.Count == 0)
        {
            lines.Insert(lines.IndexOf("## Detailed Findings") + 1, string.Empty);
            lines.Insert(lines.IndexOf("## Detailed Findings") + 2, "No findings.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
