using System.Reflection;

namespace A2G.GCodeInspector.Cli;

public sealed class ToolInfoProvider
{
    public string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? "0.1.0";
    }
}
