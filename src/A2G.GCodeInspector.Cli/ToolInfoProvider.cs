using System.Reflection;

namespace A2G.GCodeInspector.Cli;

public sealed class ToolInfoProvider
{
    public string GetVersion()
    {
        return typeof(ToolInfoProvider).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? throw new InvalidOperationException("Assembly informational version is missing.");
    }
}
