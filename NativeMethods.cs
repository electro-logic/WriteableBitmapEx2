using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Imaging
{
    internal static partial class NativeMethods
    {
        [TargetedPatchingOptOut("Internal method only, inlined across NGen boundaries for performance reasons")]
        internal static unsafe void CopyUnmanagedMemory(byte* srcPtr, int srcOffset, byte* dstPtr, int dstOffset, int count)
        {
            srcPtr += srcOffset;
            dstPtr += dstOffset;

            memcpy(dstPtr, srcPtr, count);
        }

        [TargetedPatchingOptOut("Internal method only, inlined across NGen boundaries for performance reasons")]
        internal static void SetUnmanagedMemory(IntPtr dst, int filler, int count)
        {
            memset(dst, filler, count);
        }

#if NET7_0_OR_GREATER
        // Win32 memory copy function
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        [LibraryImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false)]
        [UnmanagedCallConv(CallConvs = [typeof(Runtime.CompilerServices.CallConvCdecl)])]
        private static unsafe partial byte* memcpy(
            byte* dst,
            byte* src,
            int count);

        // Win32 memory set function
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        [LibraryImport("msvcrt.dll", EntryPoint = "memset", SetLastError = false)]
        [UnmanagedCallConv(CallConvs = [typeof(Runtime.CompilerServices.CallConvCdecl)])]
        private static partial void memset(
            IntPtr dst,
            int filler,
            int count);
#else
        // Win32 memory copy function
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private static extern unsafe byte* memcpy(
            byte* dst,
            byte* src,
            int count);

        // Win32 memory set function
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private static extern void memset(
            IntPtr dst,
            int filler,
            int count);
#endif

    }
}
