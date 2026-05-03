using A2G.GCodeInspector.Cli.Output;
using A2G.GCodeInspector.Core.Analysis;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Parsing;

namespace A2G.GCodeInspector.Cli.Commands;

public sealed class AnalyzeCommand
{
    private readonly GCodeParser _gcodeParser;
    private readonly GCodeAnalyzer _analyzer;
    private readonly CliArgumentParser _argumentParser;
    private readonly ConsoleReportWriter _consoleReportWriter;
    private readonly MarkdownReportWriter _markdownReportWriter;
    private readonly JsonReportWriter _jsonReportWriter;
    private readonly Func<string, string> _readAllText;
    private readonly Action<string, string> _writeAllText;
    private readonly TextWriter _standardOutput;
    private readonly TextWriter _standardError;

    public AnalyzeCommand()
        : this(
            new GCodeParser(),
            new GCodeAnalyzer(),
            new CliArgumentParser(),
            new ConsoleReportWriter(),
            new MarkdownReportWriter(),
            new JsonReportWriter(),
            File.ReadAllText,
            File.WriteAllText,
            Console.Out,
            Console.Error)
    {
    }

    public AnalyzeCommand(
        GCodeParser gcodeParser,
        GCodeAnalyzer analyzer,
        CliArgumentParser argumentParser,
        ConsoleReportWriter consoleReportWriter,
        MarkdownReportWriter markdownReportWriter,
        JsonReportWriter jsonReportWriter,
        Func<string, string> readAllText,
        Action<string, string> writeAllText,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        _gcodeParser = gcodeParser;
        _analyzer = analyzer;
        _argumentParser = argumentParser;
        _consoleReportWriter = consoleReportWriter;
        _markdownReportWriter = markdownReportWriter;
        _jsonReportWriter = jsonReportWriter;
        _readAllText = readAllText;
        _writeAllText = writeAllText;
        _standardOutput = standardOutput;
        _standardError = standardError;
    }

    public int Execute(string[] args)
    {
        AnalyzeArguments parsedArguments;

        try
        {
            parsedArguments = _argumentParser.ParseAnalyze(args);
        }
        catch (CliArgumentException exception)
        {
            _standardError.WriteLine(exception.Message);
            return 64;
        }

        return Execute(parsedArguments);
    }

    public int Execute(AnalyzeArguments arguments)
    {
        try
        {
            var gcode = ReadInput(arguments.FilePath);
            var layers = _gcodeParser.Parse(gcode);
            var report = _analyzer.Analyze(new ToolpathIndex(layers), arguments.Options);
            var resolvedPath = Path.GetFullPath(arguments.FilePath);
            var output = SelectReportWriter(arguments.Format).Write(
                new ReportRenderContext(
                    InputFileName: Path.GetFileName(resolvedPath),
                    InputPath: NormalizeDisplayPath(arguments.FilePath),
                    InputSizeBytes: new FileInfo(resolvedPath).Length,
                    Arguments: arguments,
                    Report: report));
            WriteOutput(arguments.OutputPath, output);
            return DetermineExitCode(report, arguments.FailOn);
        }
        catch (CliInputReadException exception)
        {
            _standardError.WriteLine(exception.Message);
            return 65;
        }
        catch (GCodeParseException exception)
        {
            _standardError.WriteLine(exception.Message);
            return 66;
        }
        catch (CliOutputWriteException exception)
        {
            _standardError.WriteLine(exception.Message);
            return 67;
        }
        catch (Exception exception)
        {
            _standardError.WriteLine(exception.Message);
            return 70;
        }
    }

    private string ReadInput(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new CliInputReadException($"Input file cannot be read: {filePath}");
            }

            return _readAllText(filePath);
        }
        catch (CliInputReadException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CliInputReadException($"Input file cannot be read: {filePath}", exception);
        }
    }

    private void WriteOutput(string? outputPath, string content)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            _standardOutput.Write(content);
            if (!content.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                _standardOutput.WriteLine();
            }

            return;
        }

        try
        {
            _writeAllText(outputPath, content);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            throw new CliOutputWriteException($"Output file cannot be written: {outputPath}", exception);
        }
    }

    private IReportWriter SelectReportWriter(AnalyzeOutputFormat format)
    {
        return format switch
        {
            AnalyzeOutputFormat.Text => _consoleReportWriter,
            AnalyzeOutputFormat.Markdown => _markdownReportWriter,
            AnalyzeOutputFormat.Json => _jsonReportWriter,
            _ => throw new InvalidOperationException($"Unsupported output format: {format}"),
        };
    }

    private static int DetermineExitCode(AnalysisReport report, FailOnMode failOn)
    {
        var hasCritical = report.Findings.Any(finding => finding.Severity == RiskSeverity.Critical);
        var hasWarningOrCritical = report.Findings.Any(finding => finding.Severity is RiskSeverity.Warning or RiskSeverity.Critical);

        return failOn switch
        {
            FailOnMode.None => 0,
            FailOnMode.Warning when hasWarningOrCritical => 1,
            FailOnMode.Critical when hasCritical => 2,
            _ => 0,
        };
    }

    private static string NormalizeDisplayPath(string inputPath)
    {
        return Path.IsPathRooted(inputPath)
            ? Path.GetFileName(inputPath)
            : inputPath;
    }

    private sealed class CliInputReadException : Exception
    {
        public CliInputReadException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    private sealed class CliOutputWriteException : Exception
    {
        public CliOutputWriteException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
