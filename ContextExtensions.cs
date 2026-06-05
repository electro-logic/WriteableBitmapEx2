// WriteableBitmapEx - Collection of extension methods for the WriteableBitmap class.
// Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
// Licensed under the MIT License. See the LICENSE file in the project root.

namespace System.Windows.Media.Imaging;

/// <summary>
/// Provides the WriteableBitmap context pixel data
/// </summary>
public static partial class WriteableBitmapContextExtensions
{
    /// <summary>
    /// Gets a BitmapContext within which to perform nested IO operations on the bitmap
    /// </summary>
    /// <remarks>For WPF the BitmapContext will lock the bitmap. Call Dispose on the context to unlock</remarks>
    /// <param name="bmp"></param>
    /// <returns></returns>
    public static BitmapContext GetBitmapContext(this WriteableBitmap bmp)
    {
        return new BitmapContext(bmp);
    }

    /// <summary>
    /// Gets a BitmapContext within which to perform nested IO operations on the bitmap
    /// </summary>
    /// <remarks>For WPF the BitmapContext will lock the bitmap. Call Dispose on the context to unlock</remarks>
    /// <param name="bmp">The bitmap.</param>
    /// <param name="mode">The ReadWriteMode. If set to ReadOnly, the bitmap will not be invalidated on dispose of the context, else it will</param>
    /// <returns></returns>
    public static BitmapContext GetBitmapContext(this WriteableBitmap bmp, ReadWriteMode mode)
    {
        return new BitmapContext(bmp, mode);
    }
}
