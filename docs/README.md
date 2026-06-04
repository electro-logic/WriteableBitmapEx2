# WriteableBitmapEx — documentation

**WriteableBitmapEx** is a collection of fast, GDI+ like extension methods for the WPF
[`WriteableBitmap`](https://learn.microsoft.com/dotnet/api/system.windows.media.imaging.writeablebitmap).
It manipulates the bitmap's back buffer directly for image processing and procedural 2D drawing.

This folder holds the in-depth documentation. The repository root [`README.md`](../README.md) is the
user-facing landing page with the full feature list and usage examples.

* [ARCHITECTURE.md](ARCHITECTURE.md) — design, the `BitmapContext` model, the source map, and the
  build/test/pack flow.
* [CHANGELOG.md](CHANGELOG.md) — release history, including the 2.0.0 breaking changes and migration notes.

## Requirements

* .NET 10 SDK or later, built with Visual Studio 2026 / the .NET 10 SDK.
* A WPF target: `net10.0-windows` with `<UseWPF>true</UseWPF>`.

Pixels are premultiplied ARGB (`Pbgra32`) 32-bit integers, matching WPF's internal `WriteableBitmap` buffer.

## Install

The package is published to a private feed (`D:\Projects\PrivateNuget`). With the repository's
[`nuget.config`](../nuget.config) in place:

```powershell
dotnet add package WriteableBitmapEx --version 2.0.0
```

## Quick start

```cs
using System.Windows.Media;
using System.Windows.Media.Imaging;

var bmp = BitmapFactory.New(512, 512);
ImageControl.Source = bmp;
using (bmp.GetBitmapContext())
{
    bmp.Clear(Colors.White);
    bmp.DrawLineAa(10, 20, 500, 480, Colors.Blue, 5);
    bmp.FillEllipseCentered(256, 256, 120, 80, Colors.Tomato);
}

// Convert a Color to a premultiplied ARGB pixel value
int pixel = Colors.CornflowerBlue.ToColorInt();
```

See the root [README.md](../README.md) for the complete set of drawing, blitting, filtering and
transformation examples.

## Build, test, pack

```powershell
dotnet build WriteableBitmapEx.sln -c Release                 # build library + tests + benchmarks
dotnet test  WriteableBitmapEx.Tests -c Release               # pixel/correctness tests (STA via Xunit.StaFact)
dotnet run   -c Release --project WriteableBitmapEx.Benchmarks -- --filter *Clear*   # benchmarks
.\pack.cmd                                                     # dotnet pack -> D:\Projects\PrivateNuget
```

## License

[MIT](../LICENSE) — Copyright (c) 2009-2026 Rene Schulte and WriteableBitmapEx Contributors.
