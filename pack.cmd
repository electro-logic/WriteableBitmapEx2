@echo off
rem Packs the WriteableBitmapEx library into the local private NuGet feed.
rem Usage: pack.cmd [outputDir]   (defaults to D:\Projects\PrivateNuget)
setlocal
set OUTDIR=%~1
if "%OUTDIR%"=="" set OUTDIR=D:\Projects\PrivateNuget

dotnet pack "%~dp0WriteableBitmapEx.csproj" -c Release -o "%OUTDIR%"
endlocal
