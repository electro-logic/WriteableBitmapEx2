using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace WriteableBitmapEx.Tests;

/// <summary>
/// Verifies the ReadOnlySpan&lt;int&gt; point overloads: they accept stackalloc spans and array slices
/// (the allocation-free forms) and produce results identical to the int[] path.
/// </summary>
public class SpanOverloadsTests
{
    private const int Green = unchecked((int)0xFF00FF00);

    [WpfFact]
    public void DrawPolyline_StackallocSpan_MatchesArrayResult()
    {
        int[] pts = [2, 2, 17, 2, 17, 17, 2, 17, 2, 2];

        var fromArray = Draw(b => b.DrawPolyline(pts, Colors.Lime));
        var fromSpan = Draw(b =>
        {
            Span<int> span = stackalloc int[] { 2, 2, 17, 2, 17, 17, 2, 17, 2, 2 };
            b.DrawPolyline(span, Colors.Lime);
        });

        AssertSamePixels(fromArray, fromSpan);
    }

    [WpfFact]
    public void FillPolygon_ReadOnlySpanSlice_MatchesArrayResult()
    {
        int[] triangle = [3, 3, 16, 3, 10, 16, 3, 3];

        var fromArray = Draw(b => b.FillPolygon(triangle, Colors.Lime));
        var fromSlice = Draw(b =>
        {
            // The triangle lives inside a larger buffer; pass only the slice (no copy).
            int[] buffer = [99, 99, 3, 3, 16, 3, 10, 16, 3, 3];
            ReadOnlySpan<int> slice = buffer.AsSpan(2);
            b.FillPolygon(slice, Colors.Lime);
        });

        AssertSamePixels(fromArray, fromSlice);
        Assert.Equal(Green, fromSlice.GetPixeli(10, 8)); // interior actually filled
    }

    [WpfFact]
    public void DrawCurve_StackallocSpan_MatchesArrayResult()
    {
        int[] pts = [5, 20, 15, 5, 25, 35, 35, 20];

        var fromArray = Draw(b => b.DrawCurve(pts, 0.5f, Colors.Lime), 40, 40);
        var fromSpan = Draw(b =>
        {
            Span<int> span = stackalloc int[] { 5, 20, 15, 5, 25, 35, 35, 20 };
            b.DrawCurve(span, 0.5f, Colors.Lime);
        }, 40, 40);

        AssertSamePixels(fromArray, fromSpan);
        Assert.True(CountNonZero(fromSpan) > 0, "DrawCurve via span should set at least one pixel.");
    }

    [WpfFact]
    public void SetRow_StackallocSpan_WritesEntireRow()
    {
        var bmp = BitmapFactory.New(5, 3);
        bmp.Clear();

        Span<int> row = stackalloc int[5];
        row.Fill(Green);
        bmp.SetRow(1, row);

        for (int x = 0; x < 5; x++)
        {
            Assert.Equal(Green, bmp.GetPixeli(x, 1));
            Assert.Equal(0, bmp.GetPixeli(x, 0));
            Assert.Equal(0, bmp.GetPixeli(x, 2));
        }
    }

    private static WriteableBitmap Draw(Action<WriteableBitmap> draw, int w = 20, int h = 20)
    {
        var bmp = BitmapFactory.New(w, h);
        bmp.Clear();
        draw(bmp);
        return bmp;
    }

    private static void AssertSamePixels(WriteableBitmap expected, WriteableBitmap actual)
    {
        Assert.Equal(expected.PixelWidth, actual.PixelWidth);
        Assert.Equal(expected.PixelHeight, actual.PixelHeight);
        for (int y = 0; y < expected.PixelHeight; y++)
        {
            for (int x = 0; x < expected.PixelWidth; x++)
            {
                Assert.Equal(expected.GetPixeli(x, y), actual.GetPixeli(x, y));
            }
        }
    }

    private static int CountNonZero(WriteableBitmap bmp)
    {
        int count = 0;
        for (int y = 0; y < bmp.PixelHeight; y++)
        {
            for (int x = 0; x < bmp.PixelWidth; x++)
            {
                if (bmp.GetPixeli(x, y) != 0)
                {
                    count++;
                }
            }
        }
        return count;
    }
}
