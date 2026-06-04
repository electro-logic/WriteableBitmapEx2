using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace WriteableBitmapEx.Tests;

/// <summary>
/// Pixel-level correctness tests. Every test runs on an STA thread (via <c>[WpfFact]</c>) because
/// <see cref="WriteableBitmap"/> is thread-affine.
/// </summary>
public class WriteableBitmapExTests
{
    private const int OpaqueRed = unchecked((int)0xFFFF0000);
    private const int OpaqueGreen = unchecked((int)0xFF00FF00);
    private const int OpaqueBlue = unchecked((int)0xFF0000FF);

    [WpfFact]
    public void New_CreatesBitmapWithRequestedSizeAndPbgra32Format()
    {
        var bmp = BitmapFactory.New(16, 9);

        Assert.Equal(16, bmp.PixelWidth);
        Assert.Equal(9, bmp.PixelHeight);
        Assert.Equal(PixelFormats.Pbgra32, bmp.Format);
    }

    [WpfFact]
    public void New_ClampsNonPositiveDimensionsToOne()
    {
        var bmp = BitmapFactory.New(0, -5);

        Assert.Equal(1, bmp.PixelWidth);
        Assert.Equal(1, bmp.PixelHeight);
    }

    [WpfFact]
    public void ToColorInt_OpaquePrimaries_AreExpectedPremultipliedArgb()
    {
        Assert.Equal(OpaqueRed, Colors.Red.ToColorInt());
        Assert.Equal(OpaqueGreen, Colors.Lime.ToColorInt());
        Assert.Equal(OpaqueBlue, Colors.Blue.ToColorInt());
    }

    [WpfFact]
    public void ToColorInt_FullyTransparentColor_IsZero()
    {
        Assert.Equal(0, Color.FromArgb(0, 10, 20, 30).ToColorInt());
    }

    [WpfFact]
    public void ToColorInt_WithOpacity_ScalesAlpha()
    {
        // Opacity 0 must collapse to a fully transparent (zero) pixel.
        Assert.Equal(0, Colors.Red.ToColorInt(0.0));
        // Opacity 1 is identical to the plain overload.
        Assert.Equal(Colors.Red.ToColorInt(), Colors.Red.ToColorInt(1.0));
    }

    [WpfFact]
    public void SetPixel_GetPixeli_RoundTripsOpaqueColor()
    {
        var bmp = BitmapFactory.New(4, 4);

        bmp.SetPixel(1, 2, Colors.Red);

        Assert.Equal(OpaqueRed, bmp.GetPixeli(1, 2));
    }

    [WpfFact]
    public void SetPixel_GetPixel_RoundTripsOpaqueColor()
    {
        var bmp = BitmapFactory.New(4, 4);

        bmp.SetPixel(3, 0, Colors.Blue);

        Assert.Equal(Colors.Blue, bmp.GetPixel(3, 0));
    }

    [WpfFact]
    public void SetPixeli_WithPrecalculatedIndex_MatchesXyAddressing()
    {
        var bmp = BitmapFactory.New(5, 5);

        bmp.SetPixeli((2 * 5) + 3, OpaqueGreen);

        Assert.Equal(OpaqueGreen, bmp.GetPixeli(3, 2));
    }

    [WpfFact]
    public void Clear_WithColor_FillsEveryPixel()
    {
        var bmp = BitmapFactory.New(7, 5);

        bmp.Clear(Colors.Blue);

        AssertAllPixels(bmp, OpaqueBlue);
    }

    [WpfFact]
    public void Clear_WithoutColor_ZeroesEveryPixel()
    {
        var bmp = BitmapFactory.New(6, 6);
        bmp.Clear(Colors.Red);

        bmp.Clear();

        AssertAllPixels(bmp, 0);
    }

    [WpfFact]
    public void Clear_WideBitmap_FillsAcrossRowsAndBlocks()
    {
        // Width is not a power of two and exceeds the first doubling block.
        var bmp = BitmapFactory.New(257, 3);

        bmp.Clear(Colors.Lime);

        Assert.Equal(OpaqueGreen, bmp.GetPixeli(0, 0));
        Assert.Equal(OpaqueGreen, bmp.GetPixeli(256, 0));
        Assert.Equal(OpaqueGreen, bmp.GetPixeli(128, 1));
        Assert.Equal(OpaqueGreen, bmp.GetPixeli(256, 2));
    }

    [WpfFact]
    public void Clone_ProducesEqualButIndependentCopy()
    {
        var bmp = BitmapFactory.New(4, 4);
        bmp.Clear(Colors.Red);
        bmp.SetPixel(0, 0, Colors.Blue);

        var clone = bmp.Clone();

        Assert.Equal(bmp.PixelWidth, clone.PixelWidth);
        Assert.Equal(bmp.PixelHeight, clone.PixelHeight);
        Assert.Equal(OpaqueBlue, clone.GetPixeli(0, 0));
        Assert.Equal(OpaqueRed, clone.GetPixeli(3, 3));

        // Mutating the original must not affect the clone.
        bmp.SetPixel(0, 0, Colors.Lime);
        Assert.Equal(OpaqueBlue, clone.GetPixeli(0, 0));
    }

    [WpfFact]
    public void ForEach_VisitsEveryPixelWithCoordinates()
    {
        var bmp = BitmapFactory.New(8, 8);

        bmp.ForEach((x, y) => Color.FromArgb(255, (byte)x, (byte)y, 0));

        Assert.Equal(Color.FromArgb(255, 5, 6, 0).ToColorInt(), bmp.GetPixeli(5, 6));
        Assert.Equal(Color.FromArgb(255, 0, 0, 0).ToColorInt(), bmp.GetPixeli(0, 0));
    }

    [WpfFact]
    public void FillRectangle_FillsInteriorAndLeavesOutsideUntouched()
    {
        var bmp = BitmapFactory.New(10, 10);
        bmp.Clear();

        bmp.FillRectangle(2, 2, 8, 8, Colors.Red);

        Assert.Equal(OpaqueRed, bmp.GetPixeli(5, 5));
        Assert.Equal(0, bmp.GetPixeli(0, 0));
    }

    [WpfFact]
    public void DrawLine_Horizontal_SetsPixelsAlongPath()
    {
        var bmp = BitmapFactory.New(10, 10);
        bmp.Clear();

        bmp.DrawLine(1, 5, 8, 5, Colors.Red);

        Assert.Equal(OpaqueRed, bmp.GetPixeli(1, 5));
        Assert.Equal(OpaqueRed, bmp.GetPixeli(5, 5));
        Assert.Equal(0, bmp.GetPixeli(5, 0));
    }

    [WpfFact]
    public void Crop_ReturnsRegionWithExpectedSizeAndContent()
    {
        var bmp = BitmapFactory.New(8, 8);
        bmp.Clear(Colors.Red);
        bmp.SetPixel(5, 6, Colors.Blue);

        var crop = bmp.Crop(4, 4, 3, 3);

        Assert.Equal(3, crop.PixelWidth);
        Assert.Equal(3, crop.PixelHeight);
        Assert.Equal(OpaqueRed, crop.GetPixeli(0, 0));
        Assert.Equal(OpaqueBlue, crop.GetPixeli(1, 2)); // original (5,6) -> crop (1,2)
    }

    [WpfFact]
    public void Resize_NearestNeighbor_ProducesRequestedSize()
    {
        var bmp = BitmapFactory.New(4, 4);
        bmp.Clear(Colors.Red);

        var resized = bmp.Resize(8, 6, WriteableBitmapExtensions.Interpolation.NearestNeighbor);

        Assert.Equal(8, resized.PixelWidth);
        Assert.Equal(6, resized.PixelHeight);
        Assert.Equal(OpaqueRed, resized.GetPixeli(4, 3));
    }

    [WpfFact]
    public void Rotate90_SwapsDimensions()
    {
        var bmp = BitmapFactory.New(4, 2);

        var rotated = bmp.Rotate(90);

        Assert.Equal(2, rotated.PixelWidth);
        Assert.Equal(4, rotated.PixelHeight);
    }

    [WpfFact]
    public void Flip_AppliedTwice_ReturnsToOriginal()
    {
        var bmp = BitmapFactory.New(4, 3);
        bmp.Clear();
        bmp.SetPixel(0, 0, Colors.Red);
        bmp.SetPixel(3, 2, Colors.Blue);

        var twice = bmp.Flip(WriteableBitmapExtensions.FlipMode.Horizontal)
                       .Flip(WriteableBitmapExtensions.FlipMode.Horizontal);

        Assert.Equal(bmp.PixelWidth, twice.PixelWidth);
        Assert.Equal(bmp.PixelHeight, twice.PixelHeight);
        Assert.Equal(OpaqueRed, twice.GetPixeli(0, 0));
        Assert.Equal(OpaqueBlue, twice.GetPixeli(3, 2));
    }

    private static void AssertAllPixels(WriteableBitmap bmp, int expected)
    {
        for (int y = 0; y < bmp.PixelHeight; y++)
        {
            for (int x = 0; x < bmp.PixelWidth; x++)
            {
                Assert.Equal(expected, bmp.GetPixeli(x, y));
            }
        }
    }
}
