# Architecture

This document explains how WriteableBitmapEx is structured, the key design decisions, and the build/test/
pack flow. It is aimed at contributors and maintainers.

## Overview

WriteableBitmapEx is a thin, high-performance extension layer over WPF's
[`WriteableBitmap`](https://learn.microsoft.com/dotnet/api/system.windows.media.imaging.writeablebitmap).
It adds GDI+ like 2D drawing, image processing, blitting, filtering, transformation and conversion methods
that operate **directly on the bitmap's back buffer**. There is no scene graph and no retained state — every
method reads/writes raw pixels and returns.

```
Consumer (WPF app)
      │  using System.Windows.Media.Imaging;
      ▼
WriteableBitmapExtensions  (partial static class, the public API)
      │  bmp.GetBitmapContext()
      ▼
BitmapContext  (unsafe struct: Lock + int* back buffer + ref-counted Invalidate)
      │  NativeMethods.CopyUnmanagedMemory / SetUnmanagedMemory
      ▼
WPF WriteableBitmap back buffer (premultiplied BGRA, Pbgra32)
```

## Key design decisions

### 1. Extension methods injected into `System.Windows.Media.Imaging`

All public methods live on `public static partial class WriteableBitmapExtensions`, declared in the
**`System.Windows.Media.Imaging`** namespace — the same namespace as `WriteableBitmap`. Consumers who
already have `using System.Windows.Media.Imaging;` see the extension methods with no extra `using`. Nested
public enums (`BlendMode`, `Interpolation`, `FlipMode`) are scoped under the same class.

### 2. One class, many files (partial class)

The API is split across topic files at the repository root (see the map below) but compiles into a single
`WriteableBitmapExtensions` type. This keeps related methods together and lets consumers who don't want the
NuGet binary copy just the source files they need.

### 3. `BitmapContext` — lock once, mutate, invalidate once

`BitmapContext` ([BitmapContext.cs](../BitmapContext.cs)) is an `unsafe readonly struct` that wraps the
lifecycle of touching the back buffer:

- On construction it `Lock()`s the bitmap and captures `BackBuffer` as an `int*`, plus width/height/stride/
  format.
- It is **reference-counted per bitmap** via two process-wide `ConcurrentDictionary` maps guarded by a lock.
  Nested `GetBitmapContext()` calls on the same bitmap share one lock and one back-buffer pointer; only the
  outermost `Dispose()` calls `AddDirtyRect()` + `Unlock()`.
- The idiomatic pattern therefore wraps a batch of drawing calls in a single
  `using (bmp.GetBitmapContext()) { ... }` so the expensive lock/invalidate happens once for the whole block.

`ReadWriteMode.ReadOnly` skips the `AddDirtyRect` on dispose (used by read paths like `GetPixel`, `Clone`,
`Crop`).

### 4. Pixel format and the premultiplied-ARGB integer model

The buffer is `Pbgra32`: 32 bits per pixel, **premultiplied** alpha, read as `0xAARRGGBB` in a little-endian
`int`. Two consequences run through the codebase:

- **Color → pixel** conversion premultiplies once via **`ToColorInt`** (a `Color` extension method in
  [BaseExtensions.cs](../BaseExtensions.cs)), using an `a + 1` integer trick so the channel multiply is a
  shift, not a divide. The `int`-color overloads assume the caller already premultiplied — they are the fast
  path. (`ToColorInt` was previously named `ConvertColor`.)
- **pixel → Color** (`GetPixel`) reverses the premultiplication with a reciprocal-alpha shift.

### 5. Native memory for bulk moves

Bulk operations (`Clear`, `Clone`, blits, block fills) go through [NativeMethods.cs](../NativeMethods.cs),
which P/Invokes `memcpy`/`memset` from `msvcrt.dll`. `BitmapContext.BlockCopy`/`Clear` wrap these. `Clear`
fills the first scanline, then doubles the filled region with successive `BlockCopy`s so the whole bitmap is
filled in `O(log height)` copies.

## Source map

The library project [`WriteableBitmapEx.csproj`](../WriteableBitmapEx.csproj) sits at the repository root and
compiles the sibling `*.cs` files (the test/benchmark sub-folders are excluded):

| File | Responsibility |
|------|----------------|
| `BitmapContext.cs` | Lock/ref-count/back-buffer wrapper and unmanaged `BlockCopy`/`Clear` helpers. |
| `BitmapFactory.cs` | Create `WriteableBitmap`s (`New`, `FromResource`, `FromContent`, `FromStream`) and convert to `Pbgra32`. |
| `BaseExtensions.cs` | `ToColorInt`, `Clear`, `Clone`, `ForEach`, `Get/SetPixel(i)`, brightness, `SetRow`. |
| `ContextExtensions.cs` | `GetBitmapContext` entry points. |
| `LineExtensions.cs` | Bresenham/DDA lines, dotted/penned lines, Cohen–Sutherland clipping. |
| `AntialiasingExtensions.cs` | Wu / anti-aliased line variants. |
| `ShapeExtensions.cs` | Outlined ellipse, polyline, quad, rectangle, triangle. |
| `FillExtensions.cs` | Filled ellipse/rectangle/triangle/quad and simple & complex polygons. |
| `SplineExtensions.cs` | Cubic Bézier and Cardinal spline (draw + fill). |
| `TextExtensions.cs` | Draw/fill `FormattedText` outlines (WPF text). |
| `BlitExtensions.cs` | Blit with Alpha/Additive/Subtractive/Mask/Multiply/ColorKeying/None modes + `BlitRender`. |
| `FilterExtensions.cs` | Convolution, blur, brightness/contrast/gamma, gray/invert. |
| `TransformationExtensions.cs` | `Crop`, `CropRelative`, `Resize`, `Rotate`, `RotateFree`, `Flip`, `Binning`. |
| `ConvertExtensions.cs` | `WriteableBitmap` ⇄ byte[] and TGA writing. |

## Projects

| Project | TFM | Purpose |
|---------|-----|---------|
| `WriteableBitmapEx` (root) | `net10.0-windows` | The library; the only packable project. |
| `WriteableBitmapEx.Tests` | `net10.0-windows` | xUnit v3 pixel-correctness tests (STA via `Xunit.StaFact`). |
| `WriteableBitmapEx.Benchmarks` | `net10.0-windows` | BenchmarkDotNet performance harness. |

## Threading

`WriteableBitmap` is thread-affine: a bitmap must be created and used on one thread. It does **not** require
an STA apartment for off-screen pixel work. The tests pin each test to an STA thread for fidelity; the
benchmarks run in-process on the default (MTA) thread, which is sufficient for raw pixel manipulation.

## Build, test and pack

```powershell
dotnet build WriteableBitmapEx.sln -c Release                 # build all three projects
dotnet test  WriteableBitmapEx.Tests -c Release               # pixel/correctness tests
dotnet run   -c Release --project WriteableBitmapEx.Benchmarks -- --filter *Clear*   # benchmarks
.\pack.cmd                                                     # dotnet pack -> D:\Projects\PrivateNuget
```

Packaging is driven by the SDK from `WriteableBitmapEx.csproj` (metadata, readme, XML docs and a `.snupkg`
symbol package). The output lands in the local feed at `D:\Projects\PrivateNuget`, registered as a package
source in [`nuget.config`](../nuget.config).
