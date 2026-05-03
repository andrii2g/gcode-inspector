using System.Globalization;
using A2G.GCodeInspector.Core.Geometry;
using A2G.GCodeInspector.Core.Layers;
using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Core.Parsing;

public sealed class GCodeParser
{
    private const double LayerHeightToleranceMm = 0.001;

    public IReadOnlyList<PrintLayer> Parse(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        return Parse(normalized.Split('\n'));
    }

    public IReadOnlyList<PrintLayer> Parse(IEnumerable<string> lines)
    {
        var state = MachineState.Default;
        var builders = new List<LayerBuilder>();
        var lineNumber = 0;

        foreach (var rawLine in lines)
        {
            lineNumber++;
            ProcessLine(rawLine, lineNumber, builders, ref state);
        }

        return builders
            .Select(builder => builder.Build())
            .ToArray();
    }

    private static void ProcessLine(
        string rawLine,
        int lineNumber,
        List<LayerBuilder> builders,
        ref MachineState state)
    {
        var line = rawLine.Trim();
        var commentIndex = line.IndexOf(';');
        var comment = commentIndex >= 0 ? line[(commentIndex + 1)..].Trim() : null;
        var commandText = commentIndex >= 0 ? line[..commentIndex].Trim() : line;

        if (!string.IsNullOrWhiteSpace(comment))
        {
            state = ApplyCommentMetadata(comment, state);
        }

        if (string.IsNullOrWhiteSpace(commandText))
        {
            return;
        }

        var command = ParseCommand(commandText, comment, lineNumber);

        switch (command.Opcode)
        {
            case "G0":
            case "G1":
                ProcessMove(command, builders, ref state);
                return;
            case "G90":
                state = state with { PositioningMode = PositioningMode.Absolute };
                return;
            case "G91":
                state = state with { PositioningMode = PositioningMode.Relative };
                return;
            case "G92":
                ProcessSetPosition(command, ref state);
                return;
            case "M82":
                state = state with { ExtrusionMode = ExtrusionMode.Absolute };
                return;
            case "M83":
                state = state with { ExtrusionMode = ExtrusionMode.Relative };
                return;
            case "G20":
                state = state with { InchesMode = true };
                return;
            case "G21":
                state = state with { InchesMode = false };
                return;
            default:
                return;
        }
    }

    private static void ProcessMove(
        GCodeCommand command,
        List<LayerBuilder> builders,
        ref MachineState state)
    {
        if (state.InchesMode)
        {
            throw new GCodeParseException(command.LineNumber, "inch mode is unsupported for movement commands.");
        }

        var startX = state.X;
        var startY = state.Y;
        var startZ = state.Z;
        var startE = state.E;

        var newX = ResolveAxis('X', command.Parameters, state.X, state.PositioningMode);
        var newY = ResolveAxis('Y', command.Parameters, state.Y, state.PositioningMode);
        var newZ = ResolveAxis('Z', command.Parameters, state.Z, state.PositioningMode);
        var extrusionDelta = ResolveExtrusionDelta(command.Parameters, state.E, state.ExtrusionMode);
        var newE = ResolveNewExtruderPosition(command.Parameters, state.E, state.ExtrusionMode);
        var newF = command.Parameters.TryGetValue('F', out var feedRate) ? feedRate : state.F;

        state = state with
        {
            X = newX,
            Y = newY,
            Z = newZ,
            E = newE,
            F = newF,
        };

        var xyDistance = Point2D.Distance(new Point2D(startX, startY), new Point2D(newX, newY));
        if (xyDistance <= 0)
        {
            return;
        }

        var isExtrusion = extrusionDelta > 0;
        if (state.CurrentLayerIndex is null && !isExtrusion)
        {
            return;
        }

        if (isExtrusion)
        {
            EnsureLayerForExtrusion(builders, ref state, newZ);
        }

        if (state.CurrentLayerIndex is null)
        {
            return;
        }

        var segment = new ToolpathSegment(
            command.LineNumber,
            state.CurrentLayerIndex.Value,
            newZ,
            state.CurrentFeatureType,
            state.CurrentRawFeatureType,
            new Point2D(startX, startY),
            new Point2D(newX, newY),
            extrusionDelta,
            newF,
            command.Comment);

        builders[state.CurrentLayerIndex.Value].Segments.Add(segment);
    }

    private static void EnsureLayerForExtrusion(
        List<LayerBuilder> builders,
        ref MachineState state,
        double newZ)
    {
        var needsFirstLayer = state.CurrentLayerIndex is null;
        var needsPendingLayer = state.PendingLayerChange;
        var needsZLayer = state.CurrentLayerZ is not null
            && newZ > state.CurrentLayerZ.Value + LayerHeightToleranceMm;

        if (!needsFirstLayer && !needsPendingLayer && !needsZLayer)
        {
            return;
        }

        var nextLayerIndex = state.CurrentLayerIndex is null ? 0 : state.CurrentLayerIndex.Value + 1;
        builders.Add(
            new LayerBuilder(
                nextLayerIndex,
                state.PendingSlicerLayerIndex,
                newZ,
                state.PendingSlicerZ,
                state.PendingLayerHeight));

        state = state with
        {
            CurrentLayerIndex = nextLayerIndex,
            CurrentLayerZ = newZ,
            PendingLayerChange = false,
            PendingSlicerLayerIndex = null,
            PendingSlicerZ = null,
            PendingLayerHeight = null,
        };
    }

    private static void ProcessSetPosition(GCodeCommand command, ref MachineState state)
    {
        state = state with
        {
            X = command.Parameters.TryGetValue('X', out var x) ? x : state.X,
            Y = command.Parameters.TryGetValue('Y', out var y) ? y : state.Y,
            Z = command.Parameters.TryGetValue('Z', out var z) ? z : state.Z,
            E = command.Parameters.TryGetValue('E', out var e) ? e : state.E,
            F = command.Parameters.TryGetValue('F', out var f) ? f : state.F,
        };
    }

    private static GCodeCommand ParseCommand(string commandText, string? comment, int lineNumber)
    {
        var tokens = commandText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            throw new GCodeParseException(lineNumber, "empty command.");
        }

        var opcode = tokens[0].ToUpperInvariant();
        var parameters = new Dictionary<char, double>();

        for (var index = 1; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (token.Length < 2)
            {
                continue;
            }

            var key = char.ToUpperInvariant(token[0]);
            var valueText = token[1..];
            if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new GCodeParseException(lineNumber, $"invalid numeric value for {key}: \"{valueText}\".");
            }

            parameters[key] = value;
        }

        return new GCodeCommand(lineNumber, opcode, parameters, comment);
    }

    private static MachineState ApplyCommentMetadata(string comment, MachineState state)
    {
        if (comment.Equals("LAYER_CHANGE", StringComparison.OrdinalIgnoreCase))
        {
            return state with { PendingLayerChange = true };
        }

        if (TryParseCommentValue(comment, "LAYER:", out var slicerLayerText)
            && int.TryParse(slicerLayerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var slicerLayer))
        {
            return state with { PendingSlicerLayerIndex = slicerLayer };
        }

        if (TryParseCommentValue(comment, "Z:", out var slicerZText)
            && double.TryParse(slicerZText, NumberStyles.Float, CultureInfo.InvariantCulture, out var slicerZ))
        {
            return state with { PendingSlicerZ = slicerZ };
        }

        if (TryParseCommentValue(comment, "HEIGHT:", out var heightText)
            && double.TryParse(heightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
        {
            return state with { PendingLayerHeight = height };
        }

        if (TryParseCommentValue(comment, "TYPE:", out var rawFeatureType))
        {
            return state with
            {
                CurrentFeatureType = ClassifyFeatureType(rawFeatureType),
                CurrentRawFeatureType = rawFeatureType.Trim(),
            };
        }

        return state;
    }

    private static bool TryParseCommentValue(string comment, string prefix, out string value)
    {
        if (comment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = comment[prefix.Length..].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static FeatureType ClassifyFeatureType(string rawFeatureType)
    {
        return rawFeatureType.Trim().ToUpperInvariant() switch
        {
            "BRIDGE INFILL" => FeatureType.BridgeInfill,
            "INTERNAL BRIDGE INFILL" => FeatureType.InternalBridgeInfill,
            "OVERHANG PERIMETER" => FeatureType.OverhangPerimeter,
            "INTERNAL INFILL" => FeatureType.InternalInfill,
            "SOLID INFILL" => FeatureType.SolidInfill,
            "TOP SOLID INFILL" => FeatureType.TopSolidInfill,
            "PERIMETER" => FeatureType.Perimeter,
            "EXTERNAL PERIMETER" => FeatureType.ExternalPerimeter,
            "SUPPORT MATERIAL" => FeatureType.SupportMaterial,
            "SUPPORT MATERIAL INTERFACE" => FeatureType.SupportMaterialInterface,
            _ => FeatureType.Unknown,
        };
    }

    private static double ResolveAxis(
        char axis,
        IReadOnlyDictionary<char, double> parameters,
        double currentValue,
        PositioningMode positioningMode)
    {
        if (!parameters.TryGetValue(axis, out var axisValue))
        {
            return currentValue;
        }

        return positioningMode == PositioningMode.Absolute ? axisValue : currentValue + axisValue;
    }

    private static double ResolveExtrusionDelta(
        IReadOnlyDictionary<char, double> parameters,
        double currentExtruderPosition,
        ExtrusionMode extrusionMode)
    {
        if (!parameters.TryGetValue('E', out var extrusionValue))
        {
            return 0;
        }

        return extrusionMode == ExtrusionMode.Absolute
            ? extrusionValue - currentExtruderPosition
            : extrusionValue;
    }

    private static double ResolveNewExtruderPosition(
        IReadOnlyDictionary<char, double> parameters,
        double currentExtruderPosition,
        ExtrusionMode extrusionMode)
    {
        if (!parameters.TryGetValue('E', out var extrusionValue))
        {
            return currentExtruderPosition;
        }

        return extrusionMode == ExtrusionMode.Absolute
            ? extrusionValue
            : currentExtruderPosition + extrusionValue;
    }

    private sealed record LayerBuilder(
        int LayerIndex,
        int? SlicerLayerIndex,
        double ZMm,
        double? SlicerZMm,
        double? HeightMm)
    {
        public List<ToolpathSegment> Segments { get; } = [];

        public PrintLayer Build()
        {
            return new PrintLayer(LayerIndex, SlicerLayerIndex, ZMm, SlicerZMm, HeightMm, Segments.ToArray());
        }
    }
}
