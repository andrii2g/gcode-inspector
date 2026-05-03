namespace A2G.GCodeInspector.Core.Geometry;

public static class GeometryMath
{
    public static double DistancePointToSegment(Point2D point, Segment2D segment)
    {
        var dx = segment.End.XMm - segment.Start.XMm;
        var dy = segment.End.YMm - segment.Start.YMm;

        if (dx == 0 && dy == 0)
        {
            return Point2D.Distance(point, segment.Start);
        }

        var t = ((point.XMm - segment.Start.XMm) * dx + (point.YMm - segment.Start.YMm) * dy)
            / ((dx * dx) + (dy * dy));
        var clamped = Math.Clamp(t, 0, 1);
        var projection = new Point2D(
            segment.Start.XMm + (clamped * dx),
            segment.Start.YMm + (clamped * dy));

        return Point2D.Distance(point, projection);
    }
}
