using System.Text.Json;

namespace A2G.GCodeInspector.Cli.Output;

public sealed class JsonReportWriter : IReportWriter
{
    public string Write(ReportRenderContext context)
    {
        var payload = new
        {
            findings = context.Report.Findings.Select(finding => new
            {
                id = finding.Id,
                lineNumberStart = finding.LineNumberStart,
                message = finding.Message,
                ruleId = finding.RuleId,
                severity = finding.Severity.ToString().ToLowerInvariant(),
            }),
            summary = new
            {
                critical = context.Report.Summary.Findings.Critical,
                info = context.Report.Summary.Findings.Info,
                total = context.Report.Summary.Findings.Total,
                warning = context.Report.Summary.Findings.Warning,
            },
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }
}
