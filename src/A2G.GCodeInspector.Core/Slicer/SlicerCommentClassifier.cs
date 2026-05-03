namespace A2G.GCodeInspector.Core.Slicer;

public sealed class SlicerCommentClassifier
{
    public bool TryClassifyFeatureType(
        string? comment,
        out FeatureType featureType,
        out string? rawFeatureType,
        out SlicerFlavor slicerFlavor)
    {
        featureType = FeatureType.Unknown;
        rawFeatureType = null;
        slicerFlavor = SlicerFlavor.Unknown;

        if (string.IsNullOrWhiteSpace(comment))
        {
            return false;
        }

        const string prefix = "TYPE:";
        if (!comment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        rawFeatureType = comment[prefix.Length..].Trim();
        slicerFlavor = SlicerFlavor.Slic3rStyle;

        featureType = rawFeatureType.ToUpperInvariant() switch
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

        return true;
    }
}
