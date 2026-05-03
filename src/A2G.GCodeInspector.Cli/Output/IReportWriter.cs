namespace A2G.GCodeInspector.Cli.Output;

public interface IReportWriter
{
    string Write(ReportRenderContext context);
}
