namespace A2G.GCodeInspector.Core.Geometry;

public sealed record BoundingBox2D(double MinX, double MinY, double MaxX, double MaxY)
{
    public bool Contains(Point2D point)
    {
        return point.XMm >= MinX
            && point.XMm <= MaxX
            && point.YMm >= MinY
            && point.YMm <= MaxY;
    }
}
