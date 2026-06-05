using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace WriteableBitmapEx.Tests;

/// <summary>
/// Coverage for the v2-added methods (Binning, CropRelative, RotateFree) and previously-untested
/// blitting, filtering and conversion paths.
/// </summary>
public class CoverageTests
{
    private const int OpaqueRed = unchecked((int)0xFFFF0000);

    [WpfFact]
    public void Binning_Factor2_HalvesDimensionsAndAveragesUniformColor()
    {
        var bmp = BitmapFactory.New(4, 4);
        bmp.Clear(Colors.Red);

        var binned = bmp.Binning(2);

        Assert.Equal(2, binned.PixelWidth);
        Assert.Equal(2, binned.PixelHeight);
        // Averaging a uniform red block yields the same red.
        Assert.Equal(OpaqueRed, binned.GetPixeli(0, 0));
        Assert.Equal(OpaqueRed, binned.GetPixeli(1, 1));
    }

    [WpfFact]
    public void CropRelative_HalfRegion_ReturnsQuarterSizedRegion()
    {
        var bmp = BitmapFactory.New(8, 8);
        bmp.Clear(Colors.Red);
        bmp.SetPixel(1, 1, Colors.Blue);

        var crop = bmp.CropRelative(new Rect(0, 0, 0.5, 0.5));

        Assert.Equal(4, crop.PixelWidth);
        Assert.Equal(4, crop.PixelHeight);
        Assert.Equal(unchecked((int)0xFF0000FF), crop.GetPixeli(1, 1));
    }

    [WpfFact]
    public void CropRelative_OutOfRange_Throws()
    {
        var bmp = BitmapFactory.New(8, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => bmp.CropRelative(new Rect(0, 0, 1.5, 1.0)));
    }

    [WpfFact]
    public void RotateFree_ZeroDegrees_PreservesDimensionsAndContent()
    {
        var bmp = BitmapFactory.New(8, 8);
        bmp.Clear(Colors.Red);

        var rotated = bmp.RotateFree(0);

        Assert.Equal(8, rotated.PixelWidth);
        Assert.Equal(8, rotated.PixelHeight);
        Assert.Equal(OpaqueRed, rotated.GetPixeli(4, 4)); // center is rotation-invariant
    }

    [WpfFact]
    public void RotateFree_ArbitraryAngle_ProducesNonEmptyResult()
    {
        var bmp = BitmapFactory.New(16, 16);
        bmp.Clear(Colors.Red);

        var rotated = bmp.RotateFree(45);

        Assert.True(rotated.PixelWidth > 0 && rotated.PixelHeight > 0);
    }

    [WpfFact]
    public void ToByteArray_FromByteArray_RoundTripsPixels()
    {
        var src = BitmapFactory.New(4, 4);
        using (src.GetBitmapContext())
        {
            for (int i = 0; i < 16; i++)
            {
                src.SetPixeli(i, unchecked((int)(0xFF000000u | (uint)(i * 0x010203))));
            }
        }

        var bytes = src.ToByteArray();
        var dst = BitmapFactory.New(4, 4).FromByteArray(bytes);

        for (int i = 0; i < 16; i++)
        {
            Assert.Equal(src.GetPixeli(i % 4, i / 4), dst.GetPixeli(i % 4, i / 4));
        }
    }

    [WpfFact]
    public void WriteTga_ProducesNonEmptyStream()
    {
        var bmp = BitmapFactory.New(4, 4);
        bmp.Clear(Colors.Red);

        using var ms = new MemoryStream();
        bmp.WriteTga(ms);

        Assert.True(ms.Length > 18); // larger than the 18-byte TGA header
    }

    [WpfFact]
    public void Blit_None_CopiesSourceRegion()
    {
        var dest = BitmapFactory.New(8, 8);
        dest.Clear();
        var src = BitmapFactory.New(4, 4);
        src.Clear(Colors.Red);

        dest.Blit(new Rect(2, 2, 4, 4), src, new Rect(0, 0, 4, 4), WriteableBitmapExtensions.BlendMode.None);

        Assert.Equal(OpaqueRed, dest.GetPixeli(3, 3)); // inside the blitted region
        Assert.Equal(0, dest.GetPixeli(0, 0));         // outside it
    }

    [WpfFact]
    public void Gray_UniformColor_MatchesScalarFormula()
    {
        var bmp = BitmapFactory.New(4, 4);
        bmp.Clear(Colors.Red); // premultiplied opaque red

        var gray = bmp.Gray();

        const int r = 255, g = 0, b = 0;
        int grayVal = ((r * 6966) + (g * 23436) + (b * 2366)) >> 15;
        int expected = (255 << 24) | (grayVal << 16) | (grayVal << 8) | grayVal;
        Assert.Equal(expected, gray.GetPixeli(2, 2));
    }
}
