using System.Globalization;
using A2G.GCodeInspector.Cli.Commands;
using A2G.GCodeInspector.Core.Analysis;

namespace A2G.GCodeInspector.Cli;

public sealed class CliArgumentParser
{
    public AnalyzeArguments ParseAnalyze(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? filePath = null;
        string? outputPath = null;
        var format = AnalyzeOutputFormat.Text;
        var failOn = FailOnMode.None;
        var maxBridgeSpanWarningMm = 25.0;
        var maxBridgeSpanCriticalMm = 50.0;
        var supportToleranceMm = 0.45;
        var sampleSpacingMm = 1.0;
        var detectHeuristicBridges = true;
        var angleStepDegrees = 45.0;
        var angleMinImprovementPercent = 20.0;
        var verbose = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                if (filePath is not null)
                {
                    throw new CliArgumentException("analyze accepts exactly one input file path.");
                }

                filePath = arg;
                continue;
            }

            switch (arg)
            {
                case "--format":
                    format = ParseFormat(ReadRequiredValue(args, ref index, arg));
                    break;
                case "--output":
                    outputPath = ReadRequiredValue(args, ref index, arg);
                    break;
                case "--fail-on":
                    failOn = ParseFailOn(ReadRequiredValue(args, ref index, arg));
                    break;
                case "--max-bridge-span-warning":
                    maxBridgeSpanWarningMm = ParsePositiveDouble(ReadRequiredValue(args, ref index, arg), arg);
                    break;
                case "--max-bridge-span-critical":
                    maxBridgeSpanCriticalMm = ParsePositiveDouble(ReadRequiredValue(args, ref index, arg), arg);
                    break;
                case "--support-tolerance-mm":
                    supportToleranceMm = ParsePositiveDouble(ReadRequiredValue(args, ref index, arg), arg);
                    break;
                case "--sample-spacing-mm":
                    sampleSpacingMm = ParsePositiveDouble(ReadRequiredValue(args, ref index, arg), arg);
                    break;
                case "--detect-heuristic-bridges":
                    detectHeuristicBridges = ParseBoolean(ReadRequiredValue(args, ref index, arg), arg);
                    break;
                case "--angle-step-degrees":
                    angleStepDegrees = ParsePositiveDouble(ReadRequiredValue(args, ref index, arg), arg);
                    break;
                case "--angle-min-improvement-percent":
                    angleMinImprovementPercent = ParsePositiveDouble(ReadRequiredValue(args, ref index, arg), arg);
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                default:
                    throw new CliArgumentException($"unknown option: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new CliArgumentException("analyze requires an input file path.");
        }

        if (maxBridgeSpanCriticalMm < maxBridgeSpanWarningMm)
        {
            throw new CliArgumentException("--max-bridge-span-critical must be >= --max-bridge-span-warning.");
        }

        if (angleStepDegrees > 90.0)
        {
            throw new CliArgumentException("--angle-step-degrees must be > 0 and <= 90.");
        }

        if (angleMinImprovementPercent >= 100.0)
        {
            throw new CliArgumentException("--angle-min-improvement-percent must be > 0 and < 100.");
        }

        var options = new AnalysisOptions(
            MaxBridgeSpanWarningMm: maxBridgeSpanWarningMm,
            MaxBridgeSpanCriticalMm: maxBridgeSpanCriticalMm,
            SupportToleranceMm: supportToleranceMm,
            SampleSpacingMm: sampleSpacingMm,
            DetectHeuristicBridges: detectHeuristicBridges,
            AngleStepDegrees: angleStepDegrees,
            AngleMinImprovementPercent: angleMinImprovementPercent);

        return new AnalyzeArguments(filePath, format, outputPath, failOn, options, verbose);
    }

    public ExplainArguments ParseExplain(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length != 1)
        {
            throw new CliArgumentException("explain requires exactly one rule id.");
        }

        var ruleId = args[0].Trim();
        if (ruleId.Length == 0)
        {
            throw new CliArgumentException("explain requires a non-empty rule id.");
        }

        return new ExplainArguments(ruleId.ToUpperInvariant());
    }

    private static string ReadRequiredValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new CliArgumentException($"{optionName} requires a value.");
        }

        index++;
        return args[index];
    }

    private static AnalyzeOutputFormat ParseFormat(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "text" => AnalyzeOutputFormat.Text,
            "markdown" => AnalyzeOutputFormat.Markdown,
            "json" => AnalyzeOutputFormat.Json,
            _ => throw new CliArgumentException("format must be text, markdown, or json."),
        };
    }

    private static FailOnMode ParseFailOn(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "none" => FailOnMode.None,
            "warning" => FailOnMode.Warning,
            "critical" => FailOnMode.Critical,
            _ => throw new CliArgumentException("fail-on must be none, warning, or critical."),
        };
    }

    private static double ParsePositiveDouble(string value, string optionName)
    {
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new CliArgumentException($"{optionName} must be a valid number.");
        }

        if (parsed <= 0)
        {
            throw new CliArgumentException($"{optionName} must be > 0.");
        }

        return parsed;
    }

    private static bool ParseBoolean(string value, string optionName)
    {
        return value.ToLowerInvariant() switch
        {
            "true" => true,
            "false" => false,
            _ => throw new CliArgumentException($"{optionName} must be true or false."),
        };
    }
}
