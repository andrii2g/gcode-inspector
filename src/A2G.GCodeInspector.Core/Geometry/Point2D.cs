namespace A2G.GCodeInspector.Core.Geometry;

public sealed record Point2D(double XMm, double YMm)
{
    public static double Distance(Point2D start, Point2D end)
    {
        var deltaX = end.XMm - start.XMm;
        var deltaY = end.YMm - start.YMm;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}
