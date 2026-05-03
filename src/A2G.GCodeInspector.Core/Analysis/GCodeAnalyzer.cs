using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Rules;

namespace A2G.GCodeInspector.Core.Analysis;

public sealed class GCodeAnalyzer
{
    private readonly IReadOnlyList<IGCodeRule> _rules;

    public GCodeAnalyzer()
        : this(
        [
            new BR001LongUnsupportedBridgeRule(),
            new BR002BridgeAngleCandidateRule(),
        ])
    {
    }

    public GCodeAnalyzer(IReadOnlyList<IGCodeRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToArray();
    }

    public AnalysisReport Analyze(
        ToolpathIndex toolpathIndex,
        AnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toolpathIndex);

        var effectiveOptions = options ?? new AnalysisOptions();
        var context = new AnalysisContext(toolpathIndex, effectiveOptions);
        return Analyze(context);
    }

    public AnalysisReport Analyze(AnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = _rules
            .SelectMany(rule => rule.Evaluate(context))
            .OrderBy(finding => finding.LayerIndex)
            .ThenBy(finding => finding.LineNumberStart)
            .ThenBy(finding => finding.LineNumberEnd)
            .ThenBy(finding => SeverityRank(finding.Severity))
            .ThenBy(finding => finding.RuleId, StringComparer.Ordinal)
            .ThenBy(finding => finding.Id, StringComparer.Ordinal)
            .ToArray();

        return new AnalysisReport(context.ToolpathIndex, context.Options, findings);
    }

    private static int SeverityRank(RiskSeverity severity)
    {
        return severity switch
        {
            RiskSeverity.Critical => 0,
            RiskSeverity.Warning => 1,
            RiskSeverity.Info => 2,
            _ => 3,
        };
    }
}
