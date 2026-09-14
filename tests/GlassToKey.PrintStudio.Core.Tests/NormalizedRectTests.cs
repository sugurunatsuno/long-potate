using GlassToKey.PrintStudio.Core.Geometry;
using Xunit;

namespace GlassToKey.PrintStudio.Core.Tests;

public sealed class NormalizedRectTests
{
    [Fact]
    public void MirrorHorizontally_MirrorsPositionAndRotation()
    {
        var source = new NormalizedRect(0.10, 0.20, 0.25, 0.30, -15.0);

        var result = source.MirrorHorizontally();

        Assert.Equal(0.65, result.X, precision: 10);
        Assert.Equal(0.20, result.Y, precision: 10);
        Assert.Equal(0.25, result.Width, precision: 10);
        Assert.Equal(0.30, result.Height, precision: 10);
        Assert.Equal(15.0, result.RotationDegrees, precision: 10);
    }
}
