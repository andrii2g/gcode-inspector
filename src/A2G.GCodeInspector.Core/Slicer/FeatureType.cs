namespace A2G.GCodeInspector.Core.Slicer;

public enum FeatureType
{
    Unknown,
    BridgeInfill,
    InternalBridgeInfill,
    OverhangPerimeter,
    InternalInfill,
    SolidInfill,
    TopSolidInfill,
    Perimeter,
    ExternalPerimeter,
    SupportMaterial,
    SupportMaterialInterface,
}
