# Changelog

## [Unreleased]

- Update the .NET SDK, Avalonia, ReactiveUI, and build dependencies.
- Replace `ImageSharpStrategy` with `SkiaSharpStrategy`; unsupported formats such as TIFF remain unchanged and report an `OptimizeError`.
- Keep Open XML part I/O serial while transforming independent image streams in parallel.
- Store optimization regression sizes in one shared, non-platform-specific baseline.
- Publish the macOS app for Apple silicon and treat compiler warnings as errors.

## [0.2.0] - 2025-12-26

- Migration from .NET 6 to .NET 10
- Migration to Avalonia 11
- Migration to SixLabors.ImageSharp v2 with support for \*.tiff images
- Migration to Clippit v2 with support of OpenXml v3
- Migration from FAKE to Fun.Build
- Remove Blazor WASM UI

## [0.1.0] - 2021-01-30

- Resize and crop images stored inside PPTX and DOCX
- Configurable target screen size
- Auto recovery for files with invalid URLs
- ReZip after compression
- Parallel image processing
- Deletion of unused parts (masters)
- Replaceable image processing library

