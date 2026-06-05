# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2026-06-04

A performance and quality release on top of 2.0.0.

### Added
- **SIMD-accelerated filters**: `Invert` (portable `Vector<int>`) and `AdjustBrightness` (AVX2
  saturating add/subtract), proven bit-identical to the scalar paths by tests.
- `BitmapContext.AsSpan()` for allocation-free, delegate-free pixel iteration; `ReadOnlySpan<int>`/
  `Span<int>` `BlockCopy` overloads; `FillPolygonsEvenOdd(ReadOnlySpan<int[]>)`.
- **Multi-targeting**: the package now ships **net8.0-windows** and **net10.0-windows**.
- Nullable reference types, .NET analyzers (`AnalysisLevel=latest`) and warnings-as-errors.
- Deterministic builds + Source Link, coverlet code coverage, and a GitHub Actions CI workflow.
- Expanded tests (Binning, CropRelative, RotateFree, ToByteArray/FromByteArray, WriteTga, Blit,
  Gray, the span overloads and the SIMD filters).

### Changed
- `BitmapContext` reference-count maps are now `[ThreadStatic]`, removing the global lock and
  `ConcurrentDictionary` from every `GetBitmapContext()` (WriteableBitmap is thread-affine).
- Mechanical modernization of the core: file-scoped namespaces, dead SVN headers removed,
  `[MethodImpl(AggressiveInlining)]` instead of the obsolete `[TargetedPatchingOptOut]`, and
  `Clear(Color)`/`FillPolygon` use `Span.Fill`/`stackalloc` to avoid per-call allocations.
- `CropRelative` and `Binning` throw `ArgumentNullException` on a null bitmap instead of returning
  null; `CropRelative` throws `ArgumentException` for a relative region that maps to an empty area.

### Fixed
- `WriteTga` no longer disposes the caller's stream.

## [2.0.0] - 2026-06-04

A modernization release that retargets the library to **.NET 10 (WPF)** and removes the legacy
multi-platform flavors. This is a **breaking** release — see *Changed* and *Removed*.

### Added
- Single, modern target framework: `net10.0-windows` (built with the .NET 10 SDK / Visual Studio 2026),
  following the earlier .NET 9 (WPF) port.
- New API surface: **`Binning`**, **`SetRow`** and **`CropRelative`** methods.
- **xUnit (v3) test project** (`WriteableBitmapEx.Tests`) with pixel-level correctness tests that run on
  STA threads via `Xunit.StaFact`.
- **BenchmarkDotNet project** (`WriteableBitmapEx.Benchmarks`) covering the hot drawing, blitting and
  transformation paths.
- SDK-style NuGet packaging from the library project (XML docs, symbol package `.snupkg`, readme), plus a
  `nuget.config` registering the local private feed, a `pack.cmd` wrapper, an `.editorconfig`, and this
  `docs/` folder.
- **`ReadOnlySpan<int>` support** across the point/polygon/curve drawing methods (`DrawPolyline`,
  `DrawPolylineAa`, `DrawBeziers`, `DrawCurve`/`DrawCurveClosed`, `FillPolygon`, `FillBeziers`,
  `FillCurve`/`FillCurveClosed`) and `SetRow`, so callers can pass `stackalloc` buffers or array slices
  without allocating an `int[]`. Existing `int[]` calls keep working via implicit conversion.
- **Trim / Native AOT readiness:** `IsTrimmable` and `IsAotCompatible` are enabled and the trim/AOT
  analyzers pass clean. `BitmapFactory.FromResource(string)` is marked `[MethodImpl(NoInlining)]` to keep
  `Assembly.GetCallingAssembly` correct under aggressive inlining, and a reflection-free
  `FromResource(Assembly, string)` overload was added for AOT-robust resource loading.

### Changed
- **`ConvertColor` was refactored into a `Color` extension method and renamed `ToColorInt()`**
  (alpha-opacity overload: `color.ToColorInt(opacity)`).
- **`RotateFree` was rewritten from scratch (~10× faster).**
- Core sources were flattened to the repository root and renamed
  (e.g. `WriteableBitmapBaseExtensions.cs` → `BaseExtensions.cs`).

### Removed
- All legacy platform projects: Silverlight, Windows Phone and UWP.
- The sample projects and the legacy unit tests.

### Migration notes
- This release targets WPF on .NET 10 only. Projects still on Silverlight, Windows Phone or UWP must stay on
  the 1.6.x line.
- Replace `WriteableBitmapExtensions.ConvertColor(color)` with `color.ToColorInt()` and
  `ConvertColor(opacity, color)` with `color.ToColorInt(opacity)`.

## [1.6.8] - 2021

Last release of the multi-platform 1.x line. Supported WPF (.NET Framework 4.0 and .NET Core 3.0),
Silverlight, Windows Phone, WinRT/Windows Store XAML and UWP, with the full GDI+ like drawing surface:
pixels, lines (incl. anti-aliased, dotted and penned), shapes, fills, Bézier/Cardinal splines, text
(`FormattedText`), blitting with multiple blend modes, filtering, transformations and conversions.
