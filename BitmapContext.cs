#region Header
//
//   Project:           WriteableBitmapEx - WriteableBitmap extensions
//   Description:       Collection of extension methods for the WriteableBitmap class.
//
//   Changed by:        $Author: unknown $
//   Changed on:        $Date: 2015-04-17 19:54:47 +0200 (Fr, 17 Apr 2015) $
//   Changed in:        $Revision: 113740 $
//   Project:           $URL: https://writeablebitmapex.svn.codeplex.com/svn/trunk/Source/WriteableBitmapEx/BitmapContext.cs $
//   Id:                $Id: BitmapContext.cs 113740 2015-04-17 17:54:47Z unknown $
//
//
//   Copyright © 2009-2015 Rene Schulte and WriteableBitmapEx Contributors
//
//   This code is open source. Please read the License.txt for details. No worries, we won't sue you! ;)
//
#endregion

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;

namespace System.Windows.Media.Imaging
{
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
    /// A disposable cross-platform wrapper around a WriteableBitmap, allowing a common API for Silverlight + WPF with locking + unlocking if necessary
    /// </summary>
    /// <remarks>Attempting to put as many preprocessor hacks in this file, to keep the rest of the codebase relatively clean</remarks>
    public readonly unsafe struct BitmapContext : IDisposable
    {
        private readonly ReadWriteMode _mode;
        private static readonly IDictionary<WriteableBitmap, int> UpdateCountByBmp = new ConcurrentDictionary<WriteableBitmap, int>();
        private static readonly IDictionary<WriteableBitmap, BitmapContextBitmapProperties> BitmapPropertiesByBmp = new ConcurrentDictionary<WriteableBitmap, BitmapContextBitmapProperties>();
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
        /// <param name="writeableBitmap"></param>
        public BitmapContext(WriteableBitmap writeableBitmap)
            : this(writeableBitmap, ReadWriteMode.ReadWrite)
        {
        }

        /// <summary>
        /// Creates an instance of a BitmapContext, with specified ReadWriteMode
        /// </summary>
        /// <param name="writeableBitmap"></param>
        /// <param name="mode"></param>
        public BitmapContext(WriteableBitmap writeableBitmap, ReadWriteMode mode)
        {
            WriteableBitmap = writeableBitmap;
            _mode = mode;

            //// Check if it's the Pbgra32 pixel format
            //if (writeableBitmap.Format != PixelFormats.Pbgra32)
            //{
            //   throw new ArgumentException("The input WriteableBitmap needs to have the Pbgra32 pixel format. Use the BitmapFactory.ConvertToPbgra32Format method to automatically convert any input BitmapSource to the right format accepted by this class.", "writeableBitmap");
            //}

            BitmapContextBitmapProperties bitmapProperties;

            lock (UpdateCountByBmp)
            {
                // Ensure the bitmap is in the dictionary of mapped Instances
                if (!UpdateCountByBmp.ContainsKey(writeableBitmap))
                {
                    // Set UpdateCount to 1 for this bitmap
                    UpdateCountByBmp.Add(writeableBitmap, 1);

                    // Lock the bitmap
                    writeableBitmap.Lock();

                    bitmapProperties = new BitmapContextBitmapProperties()
                    {
                        BackBufferStride = writeableBitmap.BackBufferStride,
                        Pixels = (int*)writeableBitmap.BackBuffer,
                        Width = writeableBitmap.PixelWidth,
                        Height = writeableBitmap.PixelHeight,
                        Format = writeableBitmap.Format
                    };
                    BitmapPropertiesByBmp.Add(
                        writeableBitmap,
                        bitmapProperties);
                }
                else
                {
                    // For previously contextualized bitmaps increment the update count
                    IncrementRefCount(writeableBitmap);
                    bitmapProperties = BitmapPropertiesByBmp[writeableBitmap];
                }

                _backBufferStride = bitmapProperties.BackBufferStride;
                Width = bitmapProperties.Width;
                Height = bitmapProperties.Height;
                Format = bitmapProperties.Format;
                Pixels = bitmapProperties.Pixels;

                double width = _backBufferStride / WriteableBitmapExtensions.SizeOfArgb;
                Length = (int)(width * Height);
            }
        }

        /// <summary>
        /// The pixels as ARGB integer values, where each channel is 8 bit.
        /// </summary>
        public unsafe int* Pixels
        {
            [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
            get;
        }

        /// <summary>
        /// The pixel format
        /// </summary>
        public PixelFormat Format
        {
            [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
            get;
        }

        /// <summary>
        /// The number of pixels.
        /// </summary>
        public int Length
        {
            [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
            get;
        }

        /// <summary>
        /// Performs a Copy operation from source to destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        public static unsafe void BlockCopy(BitmapContext src, int srcOffset, BitmapContext dest, int destOffset, int count)
        {
            NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, (byte*)dest.Pixels, destOffset, count);
        }

        /// <summary>
        /// Performs a Copy operation from source Array to destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        public static unsafe void BlockCopy(int[] src, int srcOffset, BitmapContext dest, int destOffset, int count)
        {
            fixed (int* srcPtr = src)
            {
                NativeMethods.CopyUnmanagedMemory((byte*)srcPtr, srcOffset, (byte*)dest.Pixels, destOffset, count);
            }
        }

        /// <summary>
        /// Performs a Copy operation from source Array to destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
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
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
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
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        public static unsafe void BlockCopy(BitmapContext src, int srcOffset, int[] dest, int destOffset, int count)
        {
            fixed (int* destPtr = dest)
            {
                NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, (byte*)destPtr, destOffset, count);
            }
        }

        /// <summary>
        /// Clears the BitmapContext, filling the underlying bitmap with zeros
        /// </summary>
        [TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        public void Clear()
        {
            NativeMethods.SetUnmanagedMemory((IntPtr)Pixels, 0, _backBufferStride * Height);
        }

        /// <summary>
        /// Disposes the BitmapContext, unlocking it and invalidating if WPF
        /// </summary>
        public void Dispose()
        {
            // Decrement the update count. If it hits zero
            if (DecrementRefCount(WriteableBitmap) == 0)
            {
                // Remove this bitmap from the update map
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

        private static void IncrementRefCount(WriteableBitmap target)
        {
            UpdateCountByBmp[target]++;
        }

        private static int DecrementRefCount(WriteableBitmap target)
        {
            if (!UpdateCountByBmp.TryGetValue(target, out int current))
            {
                return -1;
            }
            current--;
            UpdateCountByBmp[target] = current;
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
}
