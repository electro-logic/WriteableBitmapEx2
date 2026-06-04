# WriteableBitmapEx v2

**WriteableBitmapEx** is a collection of fast, GDI+ like extension methods for the WPF
[`WriteableBitmap`](https://learn.microsoft.com/dotnet/api/system.windows.media.imaging.writeablebitmap).
It manipulates the bitmap's back buffer directly for image processing and procedural 2D drawing — pixels,
lines, shapes, fills, splines, text, blitting, filtering, transformations and conversions.

Project based on https://github.com/reneschulte/WriteableBitmapEx

## What's changed in v2

- **.NET 10 / Visual Studio 2026** support (WPF only)
- `RotateFree` rewritten from scratch (~10× faster)
- New `Binning`, `SetRow` and `CropRelative` methods
- `ConvertColor` refactored into a `Color` extension method and renamed **`ToColorInt()`**
- **`ReadOnlySpan<int>` inputs** on the point/polygon/curve methods and `SetRow` (pass `stackalloc`
  buffers or array slices with no allocation; existing `int[]` calls keep working)
- **Trim / Native AOT ready** (`IsTrimmable`/`IsAotCompatible`, analyzers pass clean) with a reflection-free
  `BitmapFactory.FromResource(Assembly, string)` overload
- Removed legacy Silverlight, Windows Phone and UWP support; removed sample projects and legacy unit tests
- Added an xUnit test suite, a BenchmarkDotNet harness, documentation and SDK-style NuGet packaging

See [docs/CHANGELOG.md](docs/CHANGELOG.md) for the full history and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
for the design and the `BitmapContext` model.

## Requirements

- .NET 10 SDK or later, built with Visual Studio 2026 / the .NET 10 SDK
- A WPF target: `net10.0-windows` with `<UseWPF>true</UseWPF>`

Pixels are premultiplied ARGB (`Pbgra32`) 32-bit integers, matching WPF's internal `WriteableBitmap` buffer.

## Install

The package is published to a private feed (`D:\Projects\PrivateNuget`). With the repository's
[`nuget.config`](nuget.config) in place:

```powershell
dotnet add package WriteableBitmapEx --version 2.0.0
```

## Features

- **Base** — `Color` support (alpha is premultiplied) plus faster `int32`-color overloads (already
  premultiplied); `SetPixel`/`GetPixel`; fast `Clear`; fast `Clone`; `ForEach`; `SetRow`; `ToColorInt()`.
- **Transformation** — `Crop`/`CropRelative`, `Resize` (bilinear & nearest neighbor), `Rotate` (90° steps)
  and `RotateFree` (arbitrary angle), `Flip` (vertical/horizontal), `Binning`.
- **Shapes** — fast line algorithms incl. several anti-aliased variants; variable stroke thickness, dotted
  and penned/stamp lines; ellipse, polyline, quad, rectangle and triangle.
- **Splines** — cubic Bézier, Cardinal spline and closed curves.
- **Filled shapes** — fast ellipse and rectangle fills; triangle, quad, simple & complex polygons; Bézier
  and Cardinal spline curves.
- **Text** — fill and draw the outline of text strings via WPF `FormattedText` (any text WPF can render,
  with full control over `FontWeight`, `FlowDirection`, etc.).
- **Blitting** — alpha, additive, subtractive, multiply, mask and none blend modes; an optimized fast path
  for non-blended blits; `BlitRender` for affine transforms with bilinear interpolation.
- **Filtering** — convolution, blur; brightness, contrast and gamma adjustments; gray/brightness and invert.
- **Conversion** — `WriteableBitmap` ⇄ byte array; create from an application resource/content or any
  platform-supported image stream; write as a TGA image.

## Usage examples

```cs
// Initialize the WriteableBitmap with size 512x512 and set it as source of an Image control
WriteableBitmap writeableBmp = BitmapFactory.New(512, 512);
ImageControl.Source = writeableBmp;
using (writeableBmp.GetBitmapContext())
{
   // Load an image from the calling Assembly's resources via the relative path
   writeableBmp = BitmapFactory.New(1, 1).FromResource("Data/flower2.png");

   // Clear the WriteableBitmap with white color
   writeableBmp.Clear(Colors.White);

   // Set the pixel at P(10, 13) to black
   writeableBmp.SetPixel(10, 13, Colors.Black);

   // Get the color of the pixel at P(30, 43)
   Color color = writeableBmp.GetPixel(30, 43);

   // Green line from P1(1, 2) to P2(30, 40)
   writeableBmp.DrawLine(1, 2, 30, 40, Colors.Green);

   // Blue anti-aliased line from P1(10, 20) to P2(50, 70) with a stroke of 5
   writeableBmp.DrawLineAa(10, 20, 50, 70, Colors.Blue, 5);

   // Fill text on the bitmap; font, size, weight and almost any option is changeable
   var formattedText = new FormattedText("Test String", CultureInfo.GetCultureInfo("en-us"),
       FlowDirection.LeftToRight,
       new Typeface(new FontFamily("Sans MS"), FontStyles.Normal, FontWeights.Medium, FontStretches.Normal),
       80.0, Brushes.Black, 1.0);
   writeableBmp.FillText(formattedText, 100, 100, Colors.Blue, 5);

   // Black triangle with the points P1(10, 5), P2(20, 40) and P3(30, 10)
   writeableBmp.DrawTriangle(10, 5, 20, 40, 30, 10, Colors.Black);

   // Red rectangle from the point P1(2, 4) that is 10px wide and 6px high
   writeableBmp.DrawRectangle(2, 4, 12, 10, Colors.Red);

   // Filled blue ellipse with the center point P1(2, 2) that is 8px wide and 5px high
   writeableBmp.FillEllipseCentered(2, 2, 8, 5, Colors.Blue);

   // Closed green polyline. The point arrays accept ReadOnlySpan<int>, so a stackalloc buffer
   // (or an array slice) can be passed without allocating an int[].
   Span<int> p = stackalloc int[] { 10, 5, 20, 40, 30, 30, 7, 8, 10, 5 };
   writeableBmp.DrawPolyline(p, Colors.Green);

   // Cubic Bézier curve from P1(5, 5) to P4(20, 7) with control points P2(10, 15) and P3(15, 0)
   writeableBmp.DrawBezier(5, 5, 10, 15, 15, 0, 20, 7, Colors.Purple);

   // Cardinal spline with a tension of 0.5 through P1(10, 5), P2(20, 40) and P3(30, 30)
   int[] pts = { 10, 5, 20, 40, 30, 30 };
   writeableBmp.DrawCurve(pts, 0.5, Colors.Yellow);
   writeableBmp.FillCurveClosed(pts, 0.5, Colors.Green);

   // Blit a bitmap using the additive blend mode at P1(10, 10)
   writeableBmp.Blit(new Point(10, 10), bitmap, sourceRect, Colors.White, WriteableBitmapExtensions.BlendMode.Additive);

   // Override all pixels with a function that changes the color based on the coordinate
   writeableBmp.ForEach((x, y, color) => Color.FromArgb(color.A, (byte)(color.R / 2), (byte)(x * y), 100));
} // Invalidate and present in the Dispose call

// Convert a Color to a premultiplied ARGB pixel value
int packed = Colors.CornflowerBlue.ToColorInt();

// Take a snapshot
var clone = writeableBmp.Clone();

// Save to a TGA image stream (file for example)
writeableBmp.WriteTga(stream);

// Crop to a region starting at P1(5, 8), 10px wide and 10px high
var cropped = writeableBmp.Crop(5, 8, 10, 10);

// Rotate a copy 90° clockwise
var rotated = writeableBmp.Rotate(90);

// Flip a copy around the horizontal axis
var flipped = writeableBmp.Flip(WriteableBitmapExtensions.FlipMode.Horizontal);

// Resize using bilinear interpolation
var resized = writeableBmp.Resize(200, 300, WriteableBitmapExtensions.Interpolation.Bilinear);
```

## Build, test, pack

```powershell
dotnet build WriteableBitmapEx.sln -c Release                 # build library + tests + benchmarks
dotnet test  WriteableBitmapEx.Tests -c Release               # pixel/correctness tests (STA via Xunit.StaFact)
dotnet run   -c Release --project WriteableBitmapEx.Benchmarks -- --filter *Clear*   # benchmarks
.\pack.cmd                                                     # dotnet pack -> D:\Projects\PrivateNuget
```

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — design, the `BitmapContext` lock/ref-count/Pbgra32 model,
  the source map and the build/test/pack flow.
- [docs/CHANGELOG.md](docs/CHANGELOG.md) — release history and migration notes.
- Original blog posts: https://kodierer.blogspot.com/search/label/WriteableBitmapEx

## License

[MIT](LICENSE) — Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
