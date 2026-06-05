// WriteableBitmapEx - Collection of extension methods for the WriteableBitmap class.
// Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
// Licensed under the MIT License. See the LICENSE file in the project root.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Imaging;

internal static partial class NativeMethods
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void CopyUnmanagedMemory(byte* srcPtr, int srcOffset, byte* dstPtr, int dstOffset, int count)
    {
        srcPtr += srcOffset;
        dstPtr += dstOffset;

        memcpy(dstPtr, srcPtr, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetUnmanagedMemory(IntPtr dst, int filler, int count)
    {
        memset(dst, filler, count);
    }

    // Win32 memory copy function
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [LibraryImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial byte* memcpy(byte* dst, byte* src, int count);

    // Win32 memory set function
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [LibraryImport("msvcrt.dll", EntryPoint = "memset", SetLastError = false)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void memset(IntPtr dst, int filler, int count);
}
