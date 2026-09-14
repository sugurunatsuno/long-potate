namespace GlassToKey.PrintStudio.Core.Geometry;

/// <summary>
/// A rectangle expressed in normalized trackpad coordinates. Rotation is stored
/// independently so renderers can preserve the original oriented rectangle.
/// </summary>
public readonly record struct NormalizedRect(
    double X,
    double Y,
    double Width,
    double Height,
    double RotationDegrees = 0)
{
    public double CenterX => X + (Width / 2.0);
    public double CenterY => Y + (Height / 2.0);

    public NormalizedRect MirrorHorizontally() => this with
    {
        X = 1.0 - X - Width,
        RotationDegrees = NormalizeDegrees(-RotationDegrees),
    };

    private static double NormalizeDegrees(double value)
    {
        var result = value % 360.0;
        if (result <= -180.0)
        {
            result += 360.0;
        }
        else if (result > 180.0)
        {
            result -= 360.0;
        }

        return result;
    }
}
