using System.Text.Json;
using A2G.GCodeInspector.Cli;
using A2G.GCodeInspector.Cli.Commands;
using A2G.GCodeInspector.Cli.Output;
using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Geometry;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Parsing;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Tests.Cli;

public sealed class JsonOutputContractTests
{
    private readonly GCodeParser _parser = new();
    private readonly GCodeAnalyzer _analyzer = new();
    private readonly JsonReportWriter _writer = new(new ToolInfoProvider());

    [Fact]
    public void EmptyFindingsArrayIsIncluded()
    {
        using var file = TempFile.Create(
            """
            G92 X0 Y0
            ;TYPE:Perimeter
            G1 X5 Y0 Z0.2 E1.0
            """);

        var json = WriteReport(CreateReportContext(file.Path, file.FileName, noFindings: true));
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("findings", out var findings));
        Assert.Equal(JsonValueKind.Array, findings.ValueKind);
        Assert.Equal(0, findings.GetArrayLength());
    }

    [Fact]
    public void SchemaVersionAndToolVersionMatchContract()
    {
        using var file = TempFile.Create(BridgeFixture);
        var json = WriteReport(CreateReportContext(file.Path, file.FileName));
        using var document = JsonDocument.Parse(json);

        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("gcode-inspector", document.RootElement.GetProperty("tool").GetProperty("name").GetString());
        Assert.Equal(new ToolInfoProvider().GetVersion(), document.RootElement.GetProperty("tool").GetProperty("version").GetString());
        Assert.Equal("0.1.0", document.RootElement.GetProperty("tool").GetProperty("version").GetString());
    }

    [Fact]
    public void SeverityCasingAndFindingOrderingAreDeterministic()
    {
        using var file = TempFile.Create(BridgeFixture);
        var json = WriteReport(CreateReportContext(file.Path, file.FileName));
        using var document = JsonDocument.Parse(json);

        var findings = document.RootElement.GetProperty("findings");
        Assert.Equal("critical", findings[0].GetProperty("severity").GetString());
        Assert.Equal("info", findings[1].GetProperty("severity").GetString());
        Assert.Equal("BR001", findings[0].GetProperty("ruleId").GetString());
        Assert.Equal("BR002", findings[1].GetProperty("ruleId").GetString());
    }

    [Fact]
    public void TopLevelAndRepresentativeNestedKeysAreAlphabeticallyOrdered()
    {
        using var file = TempFile.Create(BridgeFixture);
        var json = WriteReport(CreateReportContext(file.Path, file.FileName));

        Assert.InRange(json.IndexOf("\"file\"", StringComparison.Ordinal), 0, int.MaxValue);
        Assert.True(json.IndexOf("\"file\"", StringComparison.Ordinal) < json.IndexOf("\"findings\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"findings\"", StringComparison.Ordinal) < json.IndexOf("\"options\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"options\"", StringComparison.Ordinal) < json.IndexOf("\"schemaVersion\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"schemaVersion\"", StringComparison.Ordinal) < json.IndexOf("\"summary\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"summary\"", StringComparison.Ordinal) < json.IndexOf("\"tool\"", StringComparison.Ordinal));

        var evidenceIndex = json.IndexOf("\"evidence\"", StringComparison.Ordinal);
        var featureTypeIndex = json.IndexOf("\"featureType\"", StringComparison.Ordinal);
        var idIndex = json.IndexOf("\"id\"", StringComparison.Ordinal);
        var layerIndexIndex = json.IndexOf("\"layerIndex\"", StringComparison.Ordinal);
        var metricsIndex = json.IndexOf("\"metrics\"", StringComparison.Ordinal);
        var positionIndex = json.IndexOf("\"position\"", StringComparison.Ordinal);
        var segmentIndex = json.IndexOf("\"segment\"", StringComparison.Ordinal);
        var titleIndex = json.IndexOf("\"title\"", StringComparison.Ordinal);
        var zMmIndex = json.IndexOf("\"zMm\"", StringComparison.Ordinal);

        Assert.True(evidenceIndex < featureTypeIndex);
        Assert.True(featureTypeIndex < idIndex);
        Assert.True(idIndex < layerIndexIndex);
        Assert.True(layerIndexIndex < metricsIndex);
        Assert.True(metricsIndex < positionIndex);
        Assert.True(positionIndex < segmentIndex);
        Assert.True(segmentIndex < titleIndex);
        Assert.True(titleIndex < zMmIndex);
    }

    [Fact]
    public void FloatsAreRoundedToTwoDecimalsInOutput()
    {
        var segment = new ToolpathSegment(
            LineNumber: 42,
            LayerIndex: 3,
            ZMm: 2.3456,
            FeatureType: FeatureType.BridgeInfill,
            RawFeatureType: null,
            Start: new Point2D(1.2345, 9.8765),
            End: new Point2D(7.8912, 3.4567),
            ExtrusionDeltaMm: 1.0,
            FeedRateMmPerMinute: null,
            RawCommentContext: null);
        var layer = new PrintLayer(3, 3, 2.3456, null, null, [segment]);
        var index = new ToolpathIndex([layer]);
        var finding = new RiskFinding(
            Id: "BR001-0001",
            RuleId: "BR001",
            Severity: RiskSeverity.Warning,
            Title: "Long unsupported bridge",
            Message: "Rounded metrics check.",
            LayerIndex: 3,
            ZMm: 2.3456,
            LineNumberStart: 42,
            LineNumberEnd: 42,
            Position: new Point2D(1.2345, 9.8765),
            PositionKind: "unsupportedSpanMidpoint",
            Metrics: new Dictionary<string, double>
            {
                ["segmentLengthMm"] = 12.3456,
                ["unsupportedSpanMm"] = 9.8765,
            },
            Suggestions: ["Try changing the bridge angle for this region."],
            Evidence: ["sampleSpacingMm=1.2345"],
            RelatedFindingId: null);
        var report = new AnalysisReport(
            index,
            new AnalysisOptions(
                SupportToleranceMm: 0.4567,
                SampleSpacingMm: 1.2345,
                AngleStepDegrees: 44.4444,
                AngleMinImprovementPercent: 19.9999),
            [finding]);
        var context = new ReportRenderContext(
            InputFileName: "sample.gcode",
            InputPath: "sample.gcode",
            InputSizeBytes: 123,
            Arguments: new AnalyzeArguments(
                FilePath: "sample.gcode",
                Format: AnalyzeOutputFormat.Json,
                OutputPath: null,
                FailOn: FailOnMode.None,
                Options: report.Options,
                Verbose: false),
            Report: report);

        var json = _writer.Write(context);

        Assert.Contains("\"zMm\": 2.35", json, StringComparison.Ordinal);
        Assert.Contains("\"xMm\": 1.23", json, StringComparison.Ordinal);
        Assert.Contains("\"yMm\": 9.88", json, StringComparison.Ordinal);
        Assert.Contains("\"segmentLengthMm\": 12.35", json, StringComparison.Ordinal);
        Assert.Contains("\"supportToleranceMm\": 0.46", json, StringComparison.Ordinal);
        Assert.Contains("\"angleStepDegrees\": 44.44", json, StringComparison.Ordinal);
        Assert.DoesNotContain("12.3456", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsoluteInputPathIsReducedToFileName()
    {
        using var file = TempFile.Create(BridgeFixture);
        var command = CreateAnalyzeCommand(out var stdout, out _);
        var exitCode = command.Execute(
            [
                file.Path,
                "--format",
                "json",
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "20",
                "--sample-spacing-mm",
                "12",
                "--angle-step-degrees",
                "45",
            ]);
        var json = stdout.ToString();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(0, exitCode);
        Assert.Equal(file.FileName, document.RootElement.GetProperty("file").GetProperty("path").GetString());
    }

    [Fact]
    public void RelativeInputPathIsPreserved()
    {
        using var file = TempFile.CreateInCurrentDirectory(BridgeFixture);
        var command = CreateAnalyzeCommand(out var stdout, out _);
        var exitCode = command.Execute(
            [
                file.FileName,
                "--format",
                "json",
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "20",
                "--sample-spacing-mm",
                "12",
                "--angle-step-degrees",
                "45",
            ]);
        var json = stdout.ToString();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(0, exitCode);
        Assert.Equal(file.FileName, document.RootElement.GetProperty("file").GetProperty("path").GetString());
    }

    private string WriteReport(ReportRenderContext context)
    {
        return _writer.Write(context);
    }

    private static AnalyzeCommand CreateAnalyzeCommand(out StringWriter stdout, out StringWriter stderr)
    {
        stdout = new StringWriter();
        stderr = new StringWriter();

        return new AnalyzeCommand(
            new GCodeParser(),
            new GCodeAnalyzer(),
            new CliArgumentParser(),
            new ConsoleReportWriter(),
            new MarkdownReportWriter(),
            new JsonReportWriter(new ToolInfoProvider()),
            File.ReadAllText,
            File.WriteAllText,
            stdout,
            stderr);
    }

    private ReportRenderContext CreateReportContext(string filePath, string displayPath, bool noFindings = false)
    {
        var layers = _parser.Parse(noFindings ? NoFindingsFixture : BridgeFixture);
        var report = _analyzer.Analyze(
            new ToolpathIndex(layers),
            new AnalysisOptions(MaxBridgeSpanWarningMm: 10, MaxBridgeSpanCriticalMm: 20, SampleSpacingMm: 12, AngleStepDegrees: 45));

        if (noFindings)
        {
            report = new AnalysisReport(report.ToolpathIndex, report.Options, []);
        }

        return new ReportRenderContext(
            InputFileName: Path.GetFileName(filePath),
            InputPath: displayPath,
            InputSizeBytes: new FileInfo(filePath).Length,
            Arguments: new AnalyzeArguments(
                FilePath: displayPath,
                Format: AnalyzeOutputFormat.Json,
                OutputPath: null,
                FailOn: FailOnMode.None,
                Options: report.Options,
                Verbose: false),
            Report: report);
    }

    private const string BridgeFixture =
        """
        G92 X100 Y0
        ;TYPE:Perimeter
        G1 X130 Y0 Z0.2 E1.0
        G92 X15 Y-20
        ;TYPE:Perimeter
        G1 X15 Y20 Z0.2 E2.0
        G92 X0 Y15
        ;TYPE:Perimeter
        G1 X30 Y-15 Z0.2 E3.0
        ;LAYER_CHANGE
        G92 X0 Y0
        ;TYPE:Bridge infill
        G1 X30 Y0 Z0.4 E4.0
        """;

    private const string NoFindingsFixture =
        """
        G92 X0 Y0
        ;TYPE:Perimeter
        G1 X5 Y0 Z0.2 E1.0
        """;

    private sealed class TempFile : IDisposable
    {
        private TempFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public string FileName => System.IO.Path.GetFileName(Path);

        public static TempFile Create(string contents)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.gcode");
            File.WriteAllText(path, contents);
            return new TempFile(path);
        }

        public static TempFile CreateInCurrentDirectory(string contents)
        {
            var path = System.IO.Path.Combine(Environment.CurrentDirectory, $"{Guid.NewGuid():N}.gcode");
            File.WriteAllText(path, contents);
            return new TempFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
