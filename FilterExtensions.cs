// WriteableBitmapEx - Collection of extension methods for the WriteableBitmap class.
// Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
// Licensed under the MIT License. See the LICENSE file in the project root.

using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Windows.Media.Imaging;

/// <summary>
/// Collection of filter / convolution extension methods for the WriteableBitmap class.
/// </summary>
public static unsafe partial class WriteableBitmapExtensions
{
    #region Kernels

    ///<summary>
    /// Gaussian blur kernel with the size 5x5
    ///</summary>
    public static int[,] KernelGaussianBlur5x5 { get; } = {
                                                   {1,  4,  7,  4, 1},
                                                   {4, 16, 26, 16, 4},
                                                   {7, 26, 41, 26, 7},
                                                   {4, 16, 26, 16, 4},
                                                   {1,  4,  7,  4, 1}
                                             };

    ///<summary>
    /// Gaussian blur kernel with the size 3x3
    ///</summary>
    public static int[,] KernelGaussianBlur3x3 { get; } = {
                                                   {16, 26, 16},
                                                   {26, 41, 26},
                                                   {16, 26, 16}
                                                };

    ///<summary>
    /// Sharpen kernel with the size 3x3
    ///</summary>
    public static int[,] KernelSharpen3x3 { get; } = {
                                             { 0, -2,  0},
                                             {-2, 11, -2},
                                             { 0, -2,  0}
                                          };

    #endregion

    #region Methods

    #region Convolute

    /// <summary>
    /// Creates a new filtered WriteableBitmap.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="kernel">The kernel used for convolution.</param>
    /// <returns>A new WriteableBitmap that is a filtered version of the input.</returns>
    public static WriteableBitmap Convolute(this WriteableBitmap bmp, int[,] kernel)
    {
        var kernelFactorSum = 0;
        foreach (var b in kernel)
        {
            kernelFactorSum += b;
        }
        return bmp.Convolute(kernel, kernelFactorSum, 0);
    }

    /// <summary>
    /// Creates a new filtered WriteableBitmap.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="kernel">The kernel used for convolution.</param>
    /// <param name="kernelFactorSum">The factor used for the kernel summing.</param>
    /// <param name="kernelOffsetSum">The offset used for the kernel summing.</param>
    /// <returns>A new WriteableBitmap that is a filtered version of the input.</returns>
    public static WriteableBitmap Convolute(this WriteableBitmap bmp, int[,] kernel, int kernelFactorSum, int kernelOffsetSum)
    {
        var kh = kernel.GetUpperBound(0) + 1;
        var kw = kernel.GetUpperBound(1) + 1;

        if ((kw & 1) == 0)
        {
            throw new InvalidOperationException("Kernel width must be odd!");
        }
        if ((kh & 1) == 0)
        {
            throw new InvalidOperationException("Kernel height must be odd!");
        }

        using var srcContext = bmp.GetBitmapContext(ReadWriteMode.ReadOnly);
        var w = srcContext.Width;
        var h = srcContext.Height;
        var result = BitmapFactory.New(w, h);

        using var resultContext = result.GetBitmapContext();
        var pixels = srcContext.Pixels;
        var resultPixels = resultContext.Pixels;
        var index = 0;
        var kwh = kw >> 1;
        var khh = kh >> 1;

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var a = 0;
                var r = 0;
                var g = 0;
                var b = 0;

                for (var kx = -kwh; kx <= kwh; kx++)
                {
                    var px = kx + x;
                    // Repeat pixels at borders
                    if (px < 0)
                    {
                        px = 0;
                    }
                    else if (px >= w)
                    {
                        px = w - 1;
                    }

                    for (var ky = -khh; ky <= khh; ky++)
                    {
                        var py = ky + y;
                        // Repeat pixels at borders
                        if (py < 0)
                        {
                            py = 0;
                        }
                        else if (py >= h)
                        {
                            py = h - 1;
                        }

                        var col = pixels[(py * w) + px];
                        var k = kernel[ky + kwh, kx + khh];
                        a += ((col >> 24) & 0x000000FF) * k;
                        r += ((col >> 16) & 0x000000FF) * k;
                        g += ((col >> 8) & 0x000000FF) * k;
                        b += ((col) & 0x000000FF) * k;
                    }
                }

                var ta = (a / kernelFactorSum) + kernelOffsetSum;
                var tr = (r / kernelFactorSum) + kernelOffsetSum;
                var tg = (g / kernelFactorSum) + kernelOffsetSum;
                var tb = (b / kernelFactorSum) + kernelOffsetSum;

                // Clamp to byte boundaries
                var ba = (byte)((ta > 255) ? 255 : ((ta < 0) ? 0 : ta));
                var br = (byte)((tr > 255) ? 255 : ((tr < 0) ? 0 : tr));
                var bg = (byte)((tg > 255) ? 255 : ((tg < 0) ? 0 : tg));
                var bb = (byte)((tb > 255) ? 255 : ((tb < 0) ? 0 : tb));

                resultPixels[index++] = (ba << 24) | (br << 16) | (bg << 8) | (bb);
            }
        }
        return result;
    }

    #endregion

    #region Invert

    /// <summary>
    /// Creates a new inverted WriteableBitmap and returns it.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <returns>The new inverted WriteableBitmap.</returns>
    public static WriteableBitmap Invert(this WriteableBitmap bmp)
    {
        using var srcContext = bmp.GetBitmapContext(ReadWriteMode.ReadOnly);
        var result = BitmapFactory.New(srcContext.Width, srcContext.Height);
        using var resultContext = result.GetBitmapContext();
        InvertCore(srcContext.AsSpan(), resultContext.AsSpan());
        return result;
    }

    /// <summary>
    /// Inverts the RGB channels (keeping alpha). For a premultiplied ARGB pixel this is exactly
    /// <c>pixel ^ 0x00FFFFFF</c>, which is vectorized with the widest available SIMD width.
    /// </summary>
    private static void InvertCore(ReadOnlySpan<int> src, Span<int> dst)
    {
        const int rgbMask = 0x00FFFFFF;
        int i = 0;

        if (System.Numerics.Vector.IsHardwareAccelerated && src.Length >= Vector<int>.Count)
        {
            var mask = new Vector<int>(rgbMask);
            int vectorEnd = src.Length - Vector<int>.Count;
            for (; i <= vectorEnd; i += Vector<int>.Count)
            {
                (new Vector<int>(src.Slice(i)) ^ mask).CopyTo(dst.Slice(i));
            }
        }

        for (; i < src.Length; i++)
        {
            dst[i] = src[i] ^ rgbMask;
        }
    }

    #endregion

    #region Color transformations

    /// <summary>
    /// Creates a new WriteableBitmap which is the grayscaled version of this one and returns it. The gray values are equal to the brightness values.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <returns>The new gray WriteableBitmap.</returns>
    public static WriteableBitmap Gray(this WriteableBitmap bmp)
    {
        using var context = bmp.GetBitmapContext(ReadWriteMode.ReadOnly);
        var nWidth = context.Width;
        var nHeight = context.Height;
        var px = context.Pixels;
        var result = BitmapFactory.New(nWidth, nHeight);

        using (var dest = result.GetBitmapContext())
        {
            var rp = dest.Pixels;
            var len = context.Length;
            for (var i = 0; i < len; i++)
            {
                // Extract
                var c = px[i];
                var a = (c >> 24) & 0x000000FF;
                var r = (c >> 16) & 0x000000FF;
                var g = (c >> 8) & 0x000000FF;
                var b = (c) & 0x000000FF;

                // Convert to gray with constant factors 0.2126, 0.7152, 0.0722
                var gray = ((r * 6966) + (g * 23436) + (b * 2366)) >> 15;
                r = g = b = gray;

                // Set
                rp[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a new WriteableBitmap which is contrast adjusted version of this one and returns it.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="level">Level of contrast as double. [-255.0, 255.0] </param>
    /// <returns>The new WriteableBitmap.</returns>
    public static WriteableBitmap AdjustContrast(this WriteableBitmap bmp, double level)
    {
        var factor = (int)(259.0 * (level + 255.0) / (255.0 * (259.0 - level)) * 255.0);

        using var context = bmp.GetBitmapContext(ReadWriteMode.ReadOnly);
        var nWidth = context.Width;
        var nHeight = context.Height;
        var px = context.Pixels;
        var result = BitmapFactory.New(nWidth, nHeight);

        using (var dest = result.GetBitmapContext())
        {
            var rp = dest.Pixels;
            var len = context.Length;
            for (var i = 0; i < len; i++)
            {
                // Extract
                var c = px[i];
                var a = (c >> 24) & 0x000000FF;
                var r = (c >> 16) & 0x000000FF;
                var g = (c >> 8) & 0x000000FF;
                var b = (c) & 0x000000FF;

                // Adjust contrast based on computed factor
                r = ((factor * (r - 128)) >> 8) + 128;
                g = ((factor * (g - 128)) >> 8) + 128;
                b = ((factor * (b - 128)) >> 8) + 128;

                // Clamp
                r = r < 0 ? 0 : r > 255 ? 255 : r;
                g = g < 0 ? 0 : g > 255 ? 255 : g;
                b = b < 0 ? 0 : b > 255 ? 255 : b;

                // Set
                rp[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a new WriteableBitmap which is brightness adjusted version of this one and returns it.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="nLevel">Level of contrast as double. [-255.0, 255.0] </param>
    /// <returns>The new WriteableBitmap.</returns>
    public static WriteableBitmap AdjustBrightness(this WriteableBitmap bmp, int nLevel)
    {
        using var context = bmp.GetBitmapContext(ReadWriteMode.ReadOnly);
        var result = BitmapFactory.New(context.Width, context.Height);
        using (var dest = result.GetBitmapContext())
        {
            AdjustBrightnessCore(context.AsSpan(), dest.AsSpan(), nLevel);
        }
        return result;
    }

    /// <summary>
    /// Adds <paramref name="nLevel"/> to each RGB channel (alpha untouched) with clamping to [0, 255].
    /// This is unsigned byte saturating add/subtract, so it maps directly to AVX2 when available.
    /// </summary>
    private static void AdjustBrightnessCore(ReadOnlySpan<int> src, Span<int> dst, int nLevel)
    {
        int i = 0;
        int level = nLevel < 0 ? -nLevel : nLevel;

        if (Avx2.IsSupported && level <= 255 && src.Length >= Vector256<int>.Count)
        {
            // Per-pixel bytes are B, G, R, A (little-endian). The delta 0x00LLLLLL adds the level to
            // B, G and R and 0 to A; byte saturating add/sub gives the same clamp as the scalar path.
            var delta = Vector256.Create(level | (level << 8) | (level << 16)).AsByte();
            bool add = nLevel >= 0;
            int vectorEnd = src.Length - Vector256<int>.Count;
            for (; i <= vectorEnd; i += Vector256<int>.Count)
            {
                var pixels = Vector256.Create(src.Slice(i)).AsByte();
                var adjusted = add ? Avx2.AddSaturate(pixels, delta) : Avx2.SubtractSaturate(pixels, delta);
                adjusted.AsInt32().CopyTo(dst.Slice(i));
            }
        }

        for (; i < src.Length; i++)
        {
            var c = src[i];
            var a = (c >> 24) & 0x000000FF;
            var r = (c >> 16) & 0x000000FF;
            var g = (c >> 8) & 0x000000FF;
            var b = (c) & 0x000000FF;

            r += nLevel;
            g += nLevel;
            b += nLevel;

            r = r < 0 ? 0 : r > 255 ? 255 : r;
            g = g < 0 ? 0 : g > 255 ? 255 : g;
            b = b < 0 ? 0 : b > 255 ? 255 : b;

            dst[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }
    }

    /// <summary>
    /// Creates a new WriteableBitmap which is gamma adjusted version of this one and returns it.
    /// </summary>
    /// <param name="bmp">The WriteableBitmap.</param>
    /// <param name="value">Value of gamma for adjustment. Original is 1.0.</param>
    /// <returns>The new WriteableBitmap.</returns>
    public static WriteableBitmap AdjustGamma(this WriteableBitmap bmp, double value)
    {
        using var context = bmp.GetBitmapContext(ReadWriteMode.ReadOnly);
        var nWidth = context.Width;
        var nHeight = context.Height;
        var px = context.Pixels;
        var result = BitmapFactory.New(nWidth, nHeight);

        using (var dest = result.GetBitmapContext())
        {
            var rp = dest.Pixels;
            var gammaCorrection = 1.0 / value;
            var len = context.Length;
            for (var i = 0; i < len; i++)
            {
                // Extract
                var c = px[i];
                var a = (c >> 24) & 0x000000FF;
                var r = (c >> 16) & 0x000000FF;
                var g = (c >> 8) & 0x000000FF;
                var b = (c) & 0x000000FF;

                // Gamma adjustment
                r = (int)(255.0 * Math.Pow(r / 255.0, gammaCorrection));
                g = (int)(255.0 * Math.Pow(g / 255.0, gammaCorrection));
                b = (int)(255.0 * Math.Pow(b / 255.0, gammaCorrection));

                // Clamps
                r = r < 0 ? 0 : r > 255 ? 255 : r;
                g = g < 0 ? 0 : g > 255 ? 255 : g;
                b = b < 0 ? 0 : b > 255 ? 255 : b;

                // Set
                rp[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        return result;
    }

    #endregion

    #endregion
}
