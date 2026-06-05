// WriteableBitmapEx - Collection of extension methods for the WriteableBitmap class.
// Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
// Licensed under the MIT License. See the LICENSE file in the project root.

using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Windows.Media.Imaging;

/// <summary>
/// Cross-platform factory for WriteableBitmaps
/// </summary>
public static class BitmapFactory
{
    /// <summary>
    /// Creates a new WriteableBitmap of the specified width and height
    /// </summary>
    /// <remarks>For WPF the default DPI is 96x96 and PixelFormat is Pbgra32</remarks>
    /// <param name="pixelWidth">The width of the bitmap in pixels.</param>
    /// <param name="pixelHeight">The height of the bitmap in pixels.</param>
    /// <returns>A new <see cref="WriteableBitmap"/> in the Pbgra32 pixel format.</returns>
    public static WriteableBitmap New(int pixelWidth, int pixelHeight)
    {
        if (pixelHeight < 1)
        {
            pixelHeight = 1;
        }

        if (pixelWidth < 1)
        {
            pixelWidth = 1;
        }
        return new WriteableBitmap(pixelWidth, pixelHeight, 96.0, 96.0, PixelFormats.Pbgra32, null);
    }

    /// <summary>
    /// Converts the input BitmapSource to the Pbgra32 format WriteableBitmap which is internally used by the WriteableBitmapEx.
    /// </summary>
    /// <param name="source">The source bitmap.</param>
    /// <returns>A <see cref="WriteableBitmap"/> in the Pbgra32 pixel format.</returns>
    public static WriteableBitmap ConvertToPbgra32Format(BitmapSource source)
    {
        // Convert to Pbgra32 if it's a different format
        if (source.Format == PixelFormats.Pbgra32)
        {
            return new WriteableBitmap(source);
        }

        var formatedBitmapSource = new FormatConvertedBitmap();
        formatedBitmapSource.BeginInit();
        formatedBitmapSource.Source = source;
        formatedBitmapSource.DestinationFormat = PixelFormats.Pbgra32;
        formatedBitmapSource.EndInit();
        return new WriteableBitmap(formatedBitmapSource);
    }

    /// <summary>
    /// Loads an image from the calling assembly's resource file and returns a new WriteableBitmap.
    /// </summary>
    /// <remarks>
    /// Resolves the resource assembly via <see cref="Assembly.GetCallingAssembly"/>. That call is trim-safe,
    /// but aggressive inlining (e.g. under Native AOT) can make the calling assembly ambiguous, so this method
    /// is marked <see cref="MethodImplOptions.NoInlining"/>. For full AOT robustness prefer the overload that
    /// takes the <see cref="Assembly"/> explicitly.
    /// </remarks>
    /// <param name="relativePath">Only the relative path to the resource file. The assembly name is retrieved automatically.</param>
    /// <returns>A new WriteableBitmap containing the pixel data.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WriteableBitmap FromResource(string relativePath)
    {
        return FromResource(Assembly.GetCallingAssembly(), relativePath);
    }

    /// <summary>
    /// Loads an image from the specified assembly's resource file and returns a new WriteableBitmap.
    /// This overload avoids the <see cref="Assembly.GetCallingAssembly"/> stack walk, which makes it robust under trimming and Native AOT.
    /// </summary>
    /// <param name="assembly">The assembly that contains the WPF resource.</param>
    /// <param name="relativePath">Only the relative path to the resource file.</param>
    /// <returns>A new WriteableBitmap containing the pixel data.</returns>
    public static WriteableBitmap FromResource(Assembly assembly, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var fullName = assembly.FullName ?? throw new ArgumentException("Assembly FullName is null", nameof(assembly));
        var asmName = new AssemblyName(fullName).Name;
        return FromContent(asmName + ";component/" + relativePath);
    }

    /// <summary>
    /// Loads an image from the applications content and returns a new WriteableBitmap.
    /// </summary>
    /// <param name="relativePath">Only the relative path to the content file.</param>
    /// <returns>A new WriteableBitmap containing the pixel data.</returns>
    public static WriteableBitmap FromContent(string relativePath)
    {
        using var bmpStream = Application.GetResourceStream(new Uri(relativePath, UriKind.Relative)).Stream;
        return FromStream(bmpStream);
    }

    /// <summary>
    /// Loads the data from an image stream and returns a new WriteableBitmap.
    /// </summary>
    /// <param name="stream">The stream with the image data.</param>
    /// <returns>A new WriteableBitmap containing the pixel data.</returns>
    public static WriteableBitmap FromStream(Stream stream)
    {
        var bmpi = new BitmapImage();
        bmpi.BeginInit();
        bmpi.CreateOptions = BitmapCreateOptions.None;
        bmpi.StreamSource = stream;
        bmpi.EndInit();
        var bmp = new WriteableBitmap(bmpi);
        bmpi.UriSource = null;
        return bmp;
    }
}
