using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BenchmarkDotNet.Attributes;

namespace WriteableBitmapEx.Benchmarks;

/// <summary>
/// Benchmarks for the hot WriteableBitmapEx drawing, blitting and transformation paths.
/// WriteableBitmap pixel operations are thread-affine but do not require an STA apartment, so these
/// run directly against live bitmaps. Compare before/after with:
/// <c>dotnet run -c Release --project WriteableBitmapEx.Benchmarks -- --filter *Clear*</c>.
/// </summary>
[MemoryDiagnoser]
public class DrawingBenchmarks
{
    private const int W = 512;
    private const int H = 512;

    private WriteableBitmap _bmp = null!;
    private WriteableBitmap _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        _bmp = BitmapFactory.New(W, H);
        _source = BitmapFactory.New(256, 256);
        _source.Clear(Colors.CornflowerBlue);
        _source.FillEllipseCentered(128, 128, 100, 100, Colors.Orange);
    }

    [Benchmark(Baseline = true)]
    public int ToColorInt() => Colors.CornflowerBlue.ToColorInt();

    [Benchmark]
    public void Clear_Color() => _bmp.Clear(Colors.CornflowerBlue);

    [Benchmark]
    public void Clear_Empty() => _bmp.Clear();

    [Benchmark]
    public void DrawLines()
    {
        using (_bmp.GetBitmapContext())
        {
            for (int i = 0; i < 256; i++)
            {
                _bmp.DrawLine(0, i, W - 1, H - 1 - i, unchecked((int)0xFF00FF00));
            }
        }
    }

    [Benchmark]
    public void DrawLinesAa()
    {
        using (_bmp.GetBitmapContext())
        {
            for (int i = 0; i < 256; i++)
            {
                _bmp.DrawLineAa(0, i, W - 1, H - 1 - i, Colors.Lime, 2);
            }
        }
    }

    [Benchmark]
    public void FillEllipse() => _bmp.FillEllipseCentered(W / 2, H / 2, 200, 150, Colors.Tomato);

    [Benchmark]
    public void Blit_None()
        => _bmp.Blit(new Rect(0, 0, 256, 256), _source, new Rect(0, 0, 256, 256), WriteableBitmapExtensions.BlendMode.None);

    [Benchmark]
    public void Blit_Alpha()
        => _bmp.Blit(new Rect(0, 0, 256, 256), _source, new Rect(0, 0, 256, 256), WriteableBitmapExtensions.BlendMode.Alpha);

    [Benchmark]
    public WriteableBitmap Resize_Bilinear()
        => _bmp.Resize(W / 2, H / 2, WriteableBitmapExtensions.Interpolation.Bilinear);

    [Benchmark]
    public WriteableBitmap Rotate90() => _bmp.Rotate(90);

    [Benchmark]
    public WriteableBitmap RotateFree45() => _bmp.RotateFree(45);

    [Benchmark]
    public WriteableBitmap Invert() => _bmp.Invert();

    [Benchmark]
    public WriteableBitmap AdjustBrightness() => _bmp.AdjustBrightness(40);
}
