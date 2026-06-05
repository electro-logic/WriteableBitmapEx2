using System.Windows.Media.Imaging;
using Xunit;

namespace WriteableBitmapEx.Tests;

/// <summary>
/// Verifies the SIMD-accelerated filters (Invert, AdjustBrightness) are bit-identical to their scalar
/// reference for every pixel, exercising both the vectorized body and the scalar tail (width*height is
/// deliberately not a multiple of the SIMD width) and boundary/over-range brightness levels.
/// </summary>
public class SimdFilterTests
{
    private const int W = 50;
    private const int H = 3; // 150 px: not a multiple of 8/16, so the scalar tail is exercised too

    [WpfFact]
    public void Invert_MatchesScalarFormulaIncludingTail()
    {
        var (bmp, original) = MakeVaried();

        var inv = bmp.Invert();

        for (int i = 0; i < original.Length; i++)
        {
            // Invert keeps alpha and inverts RGB => pixel ^ 0x00FFFFFF
            Assert.Equal(original[i] ^ 0x00FFFFFF, inv.GetPixeli(i % W, i / W));
        }
    }

    [WpfTheory]
    [InlineData(0)]
    [InlineData(40)]
    [InlineData(-40)]
    [InlineData(255)]
    [InlineData(-255)]
    [InlineData(300)]   // out of the SIMD range -> scalar path
    [InlineData(-300)]
    public void AdjustBrightness_MatchesScalarReference(int nLevel)
    {
        var (bmp, original) = MakeVaried();

        var adjusted = bmp.AdjustBrightness(nLevel);

        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(ScalarBrightness(original[i], nLevel), adjusted.GetPixeli(i % W, i / W));
        }
    }

    private static (WriteableBitmap bmp, int[] original) MakeVaried()
    {
        var bmp = BitmapFactory.New(W, H);
        var original = new int[W * H];
        using (bmp.GetBitmapContext())
        {
            for (int i = 0; i < original.Length; i++)
            {
                int c = unchecked((int)(0x10203040u + ((uint)i * 0x01030507u)));
                bmp.SetPixeli(i, c);
                original[i] = c;
            }
        }
        return (bmp, original);
    }

    private static int ScalarBrightness(int c, int nLevel)
    {
        int a = (c >> 24) & 0xFF;
        int r = (c >> 16) & 0xFF;
        int g = (c >> 8) & 0xFF;
        int b = c & 0xFF;
        return (a << 24) | (Clamp(r + nLevel) << 16) | (Clamp(g + nLevel) << 8) | Clamp(b + nLevel);

        static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }
}
