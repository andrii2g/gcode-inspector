namespace A2G.GCodeInspector.Cli.Output;

public sealed class MarkdownReportWriter : IReportWriter
{
    public string Write(ReportRenderContext context)
    {
        var lines = new List<string>
        {
            "# GCode Inspector Report",
            string.Empty,
            $"Input: `{context.InputPath}`",
            string.Empty,
            $"Total findings: {context.Report.Summary.Findings.Total}",
        };

        foreach (var finding in context.Report.Findings)
        {
            lines.Add(string.Empty);
            lines.Add($"## {finding.RuleId} {finding.Title}");
            lines.Add(finding.Message);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
