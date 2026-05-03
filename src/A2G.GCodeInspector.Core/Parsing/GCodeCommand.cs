namespace A2G.GCodeInspector.Core.Parsing;

public sealed record GCodeCommand(
    int LineNumber,
    string Opcode,
    IReadOnlyDictionary<char, double> Parameters,
    string? Comment);
