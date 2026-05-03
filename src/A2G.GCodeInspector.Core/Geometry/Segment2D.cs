namespace A2G.GCodeInspector.Core.Geometry;

public sealed record Segment2D(Point2D Start, Point2D End)
{
    public double LengthMm => Point2D.Distance(Start, End);

    public Point2D PointAtDistance(double distanceMm)
    {
        if (LengthMm <= 0)
        {
            return Start;
        }

        var ratio = distanceMm / LengthMm;
        return new Point2D(
            Start.XMm + ((End.XMm - Start.XMm) * ratio),
            Start.YMm + ((End.YMm - Start.YMm) * ratio));
    }
}
