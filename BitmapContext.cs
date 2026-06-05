// WriteableBitmapEx - Collection of extension methods for the WriteableBitmap class.
// Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
// Licensed under the MIT License. See the LICENSE file in the project root.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Windows.Media.Imaging;

/// <summary>
/// Read Write Mode for the BitmapContext.
/// </summary>
public enum ReadWriteMode
{
    /// <summary>
    /// On Dispose of a BitmapContext, do not Invalidate
    /// </summary>
    ReadOnly,

    /// <summary>
    /// On Dispose of a BitmapContext, invalidate the bitmap
    /// </summary>
    ReadWrite
}

/// <summary>
/// A disposable wrapper around a <see cref="WriteableBitmap"/> that locks it and exposes its back buffer
/// as an <c>int*</c> / <see cref="Span{Int32}"/>. Nested contexts on the same bitmap are reference-counted
/// so the bitmap is locked once and invalidated/unlocked only when the outermost context is disposed.
/// </summary>
/// <remarks>
/// The reference-count maps are <see cref="ThreadStaticAttribute">[ThreadStatic]</see>: a WriteableBitmap is
/// thread-affine, so each bitmap is only ever contextualized on its owning thread. Keeping the maps
/// per-thread removes any cross-thread locking from the hot path.
/// </remarks>
public readonly unsafe struct BitmapContext : IDisposable
{
    private readonly ReadWriteMode _mode;

    [ThreadStatic]
    private static Dictionary<WriteableBitmap, int>? _updateCountByBmp;
    [ThreadStatic]
    private static Dictionary<WriteableBitmap, BitmapContextBitmapProperties>? _bitmapPropertiesByBmp;

    private static Dictionary<WriteableBitmap, int> UpdateCountByBmp => _updateCountByBmp ??= [];
    private static Dictionary<WriteableBitmap, BitmapContextBitmapProperties> BitmapPropertiesByBmp => _bitmapPropertiesByBmp ??= [];

    private readonly int _backBufferStride;

    /// <summary>
    /// The Bitmap
    /// </summary>
    public WriteableBitmap WriteableBitmap { get; }

    /// <summary>
    /// Width of the bitmap
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height of the bitmap
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Creates an instance of a BitmapContext, with default mode = ReadWrite
    /// </summary>
    /// <param name="writeableBitmap">The bitmap to wrap and lock.</param>
    public BitmapContext(WriteableBitmap writeableBitmap)
        : this(writeableBitmap, ReadWriteMode.ReadWrite)
    {
    }

    /// <summary>
    /// Creates an instance of a BitmapContext, with specified ReadWriteMode
    /// </summary>
    /// <param name="writeableBitmap">The bitmap to wrap and lock.</param>
    /// <param name="mode">The read/write mode; ReadOnly does not invalidate the bitmap on dispose.</param>
    public BitmapContext(WriteableBitmap writeableBitmap, ReadWriteMode mode)
    {
        WriteableBitmap = writeableBitmap;
        _mode = mode;

        var updateCounts = UpdateCountByBmp;
        var properties = BitmapPropertiesByBmp;
        BitmapContextBitmapProperties bitmapProperties;

        // Ensure the bitmap is in the (thread-local) dictionary of mapped instances
        if (!updateCounts.ContainsKey(writeableBitmap))
        {
            // First context for this bitmap on this thread: lock it and capture its properties
            updateCounts[writeableBitmap] = 1;
            writeableBitmap.Lock();

            bitmapProperties = new BitmapContextBitmapProperties()
            {
                BackBufferStride = writeableBitmap.BackBufferStride,
                Pixels = (int*)writeableBitmap.BackBuffer,
                Width = writeableBitmap.PixelWidth,
                Height = writeableBitmap.PixelHeight,
                Format = writeableBitmap.Format
            };
            properties[writeableBitmap] = bitmapProperties;
        }
        else
        {
            // Nested context: increment the update count and reuse the captured properties
            updateCounts[writeableBitmap]++;
            bitmapProperties = properties[writeableBitmap];
        }

        _backBufferStride = bitmapProperties.BackBufferStride;
        Width = bitmapProperties.Width;
        Height = bitmapProperties.Height;
        Format = bitmapProperties.Format;
        Pixels = bitmapProperties.Pixels;

        double width = _backBufferStride / WriteableBitmapExtensions.SizeOfArgb;
        Length = (int)(width * Height);
    }

    /// <summary>
    /// The pixels as ARGB integer values, where each channel is 8 bit.
    /// </summary>
    public unsafe int* Pixels
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// The pixel format
    /// </summary>
    public PixelFormat Format
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// The number of pixels.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    /// Returns the pixel buffer as a <see cref="Span{Int32}"/> for allocation-free, delegate-free iteration
    /// over the locked back buffer. Only valid for the lifetime of this context.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<int> AsSpan() => new(Pixels, Length);

    /// <summary>
    /// Performs a Copy operation from source to destination BitmapContext
    /// </summary>
    /// <remarks>Equivalent to a native memcpy.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BlockCopy(BitmapContext src, int srcOffset, BitmapContext dest, int destOffset, int count)
    {
        NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, (byte*)dest.Pixels, destOffset, count);
    }

    /// <summary>
    /// Performs a Copy operation from source Array to destination BitmapContext
    /// </summary>
    /// <remarks>Equivalent to a native memcpy.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BlockCopy(int[] src, int srcOffset, BitmapContext dest, int destOffset, int count)
    {
        BlockCopy((ReadOnlySpan<int>)src, srcOffset, dest, destOffset, count);
    }

    /// <summary>
    /// Performs a Copy operation from a source span to a destination BitmapContext
    /// </summary>
    /// <remarks>Equivalent to a native memcpy.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BlockCopy(ReadOnlySpan<int> src, int srcOffset, BitmapContext dest, int destOffset, int count)
    {
        fixed (int* srcPtr = src)
        {
            NativeMethods.CopyUnmanagedMemory((byte*)srcPtr, srcOffset, (byte*)dest.Pixels, destOffset, count);
        }
    }

    /// <summary>
    /// Performs a Copy operation from source Array to destination BitmapContext
    /// </summary>
    /// <remarks>Equivalent to a native memcpy.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BlockCopy(byte[] src, int srcOffset, BitmapContext dest, int destOffset, int count)
    {
        fixed (byte* srcPtr = src)
        {
            NativeMethods.CopyUnmanagedMemory(srcPtr, srcOffset, (byte*)dest.Pixels, destOffset, count);
        }
    }

    /// <summary>
    /// Performs a Copy operation from source BitmapContext to destination Array
    /// </summary>
    /// <remarks>Equivalent to a native memcpy.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BlockCopy(BitmapContext src, int srcOffset, byte[] dest, int destOffset, int count)
    {
        fixed (byte* destPtr = dest)
        {
            NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, destPtr, destOffset, count);
        }
    }

    /// <summary>
    /// Performs a Copy operation from source BitmapContext to destination Array
    /// </summary>
    /// <remarks>Equivalent to a native memcpy.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BlockCopy(BitmapContext src, int srcOffset, int[] dest, int destOffset, int count)
    {
        BlockCopy(src, srcOffset, (Span<int>)dest, destOffset, count);
    }

    /// <summary>
    /// Performs a Copy operation from a source BitmapContext to a destination span
    /// </summary>
    /// <remarks>Equivalent to a native memcpy.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BlockCopy(BitmapContext src, int srcOffset, Span<int> dest, int destOffset, int count)
    {
        fixed (int* destPtr = dest)
        {
            NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, (byte*)destPtr, destOffset, count);
        }
    }

    /// <summary>
    /// Clears the BitmapContext, filling the underlying bitmap with zeros
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        NativeMethods.SetUnmanagedMemory((IntPtr)Pixels, 0, _backBufferStride * Height);
    }

    /// <summary>
    /// Disposes the BitmapContext, unlocking the bitmap and invalidating it (in ReadWrite mode)
    /// when the outermost nested context is disposed.
    /// </summary>
    public void Dispose()
    {
        // Decrement the update count. If it hits zero this is the outermost context.
        if (DecrementRefCount(WriteableBitmap) == 0)
        {
            // Remove this bitmap from the (thread-local) update map
            UpdateCountByBmp.Remove(WriteableBitmap);
            BitmapPropertiesByBmp.Remove(WriteableBitmap);

            // Invalidate the bitmap if ReadWrite _mode
            if (_mode == ReadWriteMode.ReadWrite)
            {
                WriteableBitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
            }

            // Unlock the bitmap
            WriteableBitmap.Unlock();
        }
    }

    private static int DecrementRefCount(WriteableBitmap target)
    {
        var counts = UpdateCountByBmp;
        if (!counts.TryGetValue(target, out int current))
        {
            return -1;
        }
        current--;
        counts[target] = current;
        return current;
    }

    private struct BitmapContextBitmapProperties
    {
        public int Width;
        public int Height;
        public int* Pixels;
        public PixelFormat Format;
        public int BackBufferStride;
    }
}
