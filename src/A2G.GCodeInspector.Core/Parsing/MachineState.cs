using A2G.GCodeInspector.Core.Slicer;

namespace A2G.GCodeInspector.Core.Parsing;

public sealed record MachineState(
    double X,
    double Y,
    double Z,
    double E,
    double? F,
    PositioningMode PositioningMode,
    ExtrusionMode ExtrusionMode,
    bool InchesMode,
    int? CurrentLayerIndex,
    double? CurrentLayerZ,
    bool PendingLayerChange,
    int? PendingSlicerLayerIndex,
    double? PendingSlicerZ,
    double? PendingLayerHeight,
    FeatureType CurrentFeatureType,
    string? CurrentRawFeatureType)
{
    public static MachineState Default =>
        new(
            0,
            0,
            0,
            0,
            null,
            PositioningMode.Absolute,
            ExtrusionMode.Absolute,
            false,
            null,
            null,
            false,
            null,
            null,
            null,
            FeatureType.Unknown,
            null);
}
