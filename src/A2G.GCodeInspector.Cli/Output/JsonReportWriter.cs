using System.Text.Json;
using System.Text.Json.Serialization;
using A2G.GCodeInspector.Cli;
using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Cli.Output;

public sealed class JsonReportWriter : IReportWriter
{
    private readonly ToolInfoProvider _toolInfoProvider;

    public JsonReportWriter()
        : this(new ToolInfoProvider())
    {
    }

    public JsonReportWriter(ToolInfoProvider toolInfoProvider)
    {
        _toolInfoProvider = toolInfoProvider;
    }

    public string Write(ReportRenderContext context)
    {
        var payload = new JsonReportDto(
            File: new JsonFileDto(
                Name: context.InputFileName,
                Path: context.InputPath,
                SizeBytes: context.InputSizeBytes),
            Findings: context.Report.Findings.Select(finding => CreateFindingDto(context.Report.ToolpathIndex, finding)).ToArray(),
            Options: new JsonOptionsDto(
                AngleMinImprovementPercent: Round(context.Arguments.Options.AngleMinImprovementPercent),
                AngleStepDegrees: Round(context.Arguments.Options.AngleStepDegrees),
                DetectHeuristicBridges: context.Arguments.Options.DetectHeuristicBridges,
                FailOn: ToLowerInvariant(context.Arguments.FailOn),
                Format: ToLowerInvariant(context.Arguments.Format),
                MaxBridgeSpanCriticalMm: Round(context.Arguments.Options.MaxBridgeSpanCriticalMm),
                MaxBridgeSpanWarningMm: Round(context.Arguments.Options.MaxBridgeSpanWarningMm),
                SampleSpacingMm: Round(context.Arguments.Options.SampleSpacingMm),
                SupportToleranceMm: Round(context.Arguments.Options.SupportToleranceMm)),
            SchemaVersion: 1,
            Summary: new JsonSummaryDto(
                Findings: new JsonSummaryFindingsDto(
                    Critical: context.Report.Summary.Findings.Critical,
                    Info: context.Report.Summary.Findings.Info,
                    Total: context.Report.Summary.Findings.Total,
                    Warning: context.Report.Summary.Findings.Warning),
                Layers: context.Report.Summary.Layers,
                Segments: context.Report.Summary.Segments),
            Tool: new JsonToolDto(
                Name: "gcode-inspector",
                Version: _toolInfoProvider.GetVersion()));

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }

    private static JsonFindingDto CreateFindingDto(ToolpathIndex toolpathIndex, RiskFinding finding)
    {
        var segment = FindSegment(toolpathIndex, finding);
        var featureType = segment is null
            ? "unknown"
            : ToCamelCase(segment.FeatureType);

        return new JsonFindingDto(
            Evidence: finding.Evidence.ToArray(),
            FeatureType: featureType,
            Id: finding.Id,
            LayerIndex: finding.LayerIndex,
            LineNumberEnd: finding.LineNumberEnd,
            LineNumberStart: finding.LineNumberStart,
            Message: finding.Message,
            Metrics: CreateOrderedMetrics(finding.Metrics),
            Position: new JsonPositionDto(
                Kind: finding.PositionKind,
                XMm: Round(finding.Position.XMm),
                YMm: Round(finding.Position.YMm)),
            RelatedFindingId: finding.RelatedFindingId,
            RuleId: finding.RuleId,
            Segment: CreateSegmentDto(segment),
            Severity: finding.Severity.ToString().ToLowerInvariant(),
            Suggestions: finding.Suggestions.ToArray(),
            Title: finding.Title,
            ZMm: Round(finding.ZMm));
    }

    private static JsonSegmentDto CreateSegmentDto(ToolpathSegment? segment)
    {
        return segment is null
            ? new JsonSegmentDto(
                AngleDegrees: 0,
                End: new JsonPointDto(0, 0),
                LengthMm: 0,
                Start: new JsonPointDto(0, 0))
            : new JsonSegmentDto(
                AngleDegrees: Round(segment.AngleDegreesNormalized0To180),
                End: new JsonPointDto(Round(segment.End.XMm), Round(segment.End.YMm)),
                LengthMm: Round(segment.LengthMm),
                Start: new JsonPointDto(Round(segment.Start.XMm), Round(segment.Start.YMm)));
    }

    private static ToolpathSegment? FindSegment(ToolpathIndex toolpathIndex, RiskFinding finding)
    {
        return toolpathIndex
            .GetLayer(finding.LayerIndex)?
            .Segments
            .FirstOrDefault(segment => segment.LineNumber == finding.LineNumberStart);
    }

    private static SortedDictionary<string, double> CreateOrderedMetrics(IReadOnlyDictionary<string, double> metrics)
    {
        var ordered = new SortedDictionary<string, double>(StringComparer.Ordinal);
        foreach (var entry in metrics)
        {
            ordered[entry.Key] = Round(entry.Value);
        }

        return ordered;
    }

    private static double Round(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static string ToLowerInvariant<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return value.ToString().ToLowerInvariant();
    }

    private static string ToCamelCase(FeatureType featureType)
    {
        return featureType switch
        {
            FeatureType.Unknown => "unknown",
            FeatureType.BridgeInfill => "bridgeInfill",
            FeatureType.InternalBridgeInfill => "internalBridgeInfill",
            FeatureType.OverhangPerimeter => "overhangPerimeter",
            FeatureType.InternalInfill => "internalInfill",
            FeatureType.SolidInfill => "solidInfill",
            FeatureType.TopSolidInfill => "topSolidInfill",
            FeatureType.Perimeter => "perimeter",
            FeatureType.ExternalPerimeter => "externalPerimeter",
            FeatureType.SupportMaterial => "supportMaterial",
            FeatureType.SupportMaterialInterface => "supportMaterialInterface",
            _ => "unknown",
        };
    }

    public sealed record JsonReportDto(
        [property: JsonPropertyName("file")] JsonFileDto File,
        [property: JsonPropertyName("findings")] IReadOnlyList<JsonFindingDto> Findings,
        [property: JsonPropertyName("options")] JsonOptionsDto Options,
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("summary")] JsonSummaryDto Summary,
        [property: JsonPropertyName("tool")] JsonToolDto Tool);

    public sealed record JsonFileDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("sizeBytes")] long SizeBytes);

    public sealed record JsonFindingDto(
        [property: JsonPropertyName("evidence")] IReadOnlyList<string> Evidence,
        [property: JsonPropertyName("featureType")] string FeatureType,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("layerIndex")] int LayerIndex,
        [property: JsonPropertyName("lineNumberEnd")] int LineNumberEnd,
        [property: JsonPropertyName("lineNumberStart")] int LineNumberStart,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("metrics")] IReadOnlyDictionary<string, double> Metrics,
        [property: JsonPropertyName("position")] JsonPositionDto Position,
        [property: JsonPropertyName("relatedFindingId")] string? RelatedFindingId,
        [property: JsonPropertyName("ruleId")] string RuleId,
        [property: JsonPropertyName("segment")] JsonSegmentDto Segment,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("suggestions")] IReadOnlyList<string> Suggestions,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("zMm")] double ZMm);

    public sealed record JsonPositionDto(
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("xMm")] double XMm,
        [property: JsonPropertyName("yMm")] double YMm);

    public sealed record JsonPointDto(
        [property: JsonPropertyName("xMm")] double XMm,
        [property: JsonPropertyName("yMm")] double YMm);

    public sealed record JsonSegmentDto(
        [property: JsonPropertyName("angleDegrees")] double AngleDegrees,
        [property: JsonPropertyName("end")] JsonPointDto End,
        [property: JsonPropertyName("lengthMm")] double LengthMm,
        [property: JsonPropertyName("start")] JsonPointDto Start);

    public sealed record JsonOptionsDto(
        [property: JsonPropertyName("angleMinImprovementPercent")] double AngleMinImprovementPercent,
        [property: JsonPropertyName("angleStepDegrees")] double AngleStepDegrees,
        [property: JsonPropertyName("detectHeuristicBridges")] bool DetectHeuristicBridges,
        [property: JsonPropertyName("failOn")] string FailOn,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("maxBridgeSpanCriticalMm")] double MaxBridgeSpanCriticalMm,
        [property: JsonPropertyName("maxBridgeSpanWarningMm")] double MaxBridgeSpanWarningMm,
        [property: JsonPropertyName("sampleSpacingMm")] double SampleSpacingMm,
        [property: JsonPropertyName("supportToleranceMm")] double SupportToleranceMm);

    public sealed record JsonSummaryDto(
        [property: JsonPropertyName("findings")] JsonSummaryFindingsDto Findings,
        [property: JsonPropertyName("layers")] int Layers,
        [property: JsonPropertyName("segments")] int Segments);

    public sealed record JsonSummaryFindingsDto(
        [property: JsonPropertyName("critical")] int Critical,
        [property: JsonPropertyName("info")] int Info,
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("warning")] int Warning);

    public sealed record JsonToolDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("version")] string Version);
}
