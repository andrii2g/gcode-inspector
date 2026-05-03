using A2G.GCodeInspector.Core.Analysis;

namespace A2G.GCodeInspector.Core.Rules;

public interface IGCodeRule
{
    IReadOnlyList<RiskFinding> Evaluate(AnalysisContext context);
}
