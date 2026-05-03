using A2G.GCodeInspector.Cli;
using A2G.GCodeInspector.Cli.Commands;
using A2G.GCodeInspector.Cli.Output;
using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Parsing;

namespace A2G.GCodeInspector.Tests.Cli;

public sealed class AnalyzeCommandExitCodeTests
{
    [Fact]
    public void MissingFileReturnsExitCode65()
    {
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                @"C:\definitely-missing\sample.gcode",
            ]);

        Assert.Equal(65, exitCode);
    }

    [Fact]
    public void InvalidFormatReturnsExitCode64()
    {
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                "sample.gcode",
                "--format",
                "xml",
            ]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void InvalidFailOnReturnsExitCode64()
    {
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                "sample.gcode",
                "--fail-on",
                "info",
            ]);

        Assert.Equal(64, exitCode);
    }

    [Fact]
    public void ParseErrorReturnsExitCode66()
    {
        using var file = TempFile.Create(
            """
            G1 Xbad Y0 E1
            """);

        var command = CreateCommand(out _, out _);
        var exitCode = command.Execute([file.Path]);

        Assert.Equal(66, exitCode);
    }

    [Fact]
    public void OutputWriteErrorReturnsExitCode67()
    {
        using var file = TempFile.Create(WarningFixture);
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "report.md");
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                file.Path,
                "--format",
                "markdown",
                "--output",
                missingDirectory,
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "50",
                "--sample-spacing-mm",
                "12",
            ]);

        Assert.Equal(67, exitCode);
    }

    [Fact]
    public void FailOnNoneReturnsZeroWithCriticalFindings()
    {
        using var file = TempFile.Create(CriticalFixture);
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                file.Path,
                "--fail-on",
                "none",
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "20",
            ]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void FailOnWarningReturnsOneWithWarningFindings()
    {
        using var file = TempFile.Create(WarningFixture);
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                file.Path,
                "--fail-on",
                "warning",
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "50",
                "--sample-spacing-mm",
                "12",
            ]);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void FailOnWarningReturnsOneWithCriticalFindings()
    {
        using var file = TempFile.Create(CriticalFixture);
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                file.Path,
                "--fail-on",
                "warning",
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "20",
            ]);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void FailOnCriticalReturnsZeroWithWarningOnlyFindings()
    {
        using var file = TempFile.Create(WarningFixture);
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                file.Path,
                "--fail-on",
                "critical",
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "50",
                "--sample-spacing-mm",
                "12",
            ]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void FailOnCriticalReturnsTwoWithCriticalFindings()
    {
        using var file = TempFile.Create(CriticalFixture);
        var command = CreateCommand(out _, out _);

        var exitCode = command.Execute(
            [
                file.Path,
                "--fail-on",
                "critical",
                "--max-bridge-span-warning",
                "10",
                "--max-bridge-span-critical",
                "20",
            ]);

        Assert.Equal(2, exitCode);
    }

    private static AnalyzeCommand CreateCommand(out StringWriter stdout, out StringWriter stderr)
    {
        stdout = new StringWriter();
        stderr = new StringWriter();

        return new AnalyzeCommand(
            new GCodeParser(),
            new GCodeAnalyzer(),
            new CliArgumentParser(),
            new ConsoleReportWriter(),
            new MarkdownReportWriter(),
            new JsonReportWriter(),
            File.ReadAllText,
            File.WriteAllText,
            stdout,
            stderr);
    }

    private const string WarningFixture =
        """
        G92 X15 Y-20
        ;TYPE:Perimeter
        G1 X15 Y20 Z0.2 E1.0
        G92 X0 Y15
        ;TYPE:Perimeter
        G1 X30 Y-15 Z0.2 E2.0
        ;LAYER_CHANGE
        G92 X0 Y0
        ;TYPE:Bridge infill
        G1 X30 Y0 Z0.4 E3.0
        """;

    private const string CriticalFixture =
        """
        ;TYPE:Bridge infill
        G1 X30 Y0 Z0.2 E1.0
        ;LAYER_CHANGE
        G92 X0 Y0
        ;TYPE:Bridge infill
        G1 X30 Y0 Z0.4 E2.0
        """;

    private sealed class TempFile : IDisposable
    {
        private TempFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempFile Create(string contents)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.gcode");
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
