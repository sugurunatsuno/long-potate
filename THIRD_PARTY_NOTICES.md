# Third-party notices

## AppleMagicTouchstreamLP / GlassToKey

This project targets behavioral compatibility with:

- https://github.com/disarmyouwitha/AppleMagicTouchstreamLP
- compatibility reference commit: `c20233c6718afd2734f2a95d509bfe44d7e7f7b0`

The initial version of this repository does **not** vendor or copy upstream source files. Geometry behavior and data formats are documented from inspection of the public implementation.

A MIT license file exists at `mac/LICENSE` in the referenced upstream commit. A repository-root license was not found during the initial review. Therefore, before copying or adapting any upstream implementation, contributors must verify the license that applies to the exact file and preserve all required notices.

Relevant reference files include:

- `windows_linux/GlassToKey.Core/Layout/LayoutBuilder.cs`
- `windows_linux/GlassToKey.Core/Layout/TrackpadLayoutPreset.cs`
- `windows_linux/GlassToKey.Core/Layout/ColumnLayoutSettings.cs`
- `windows_linux/GlassToKey.Core/Layout/KeyLayout.cs`
- `windows_linux/GlassToKey.Core/Runtime/RuntimeConfigurationFactory.cs`
- `windows_linux/GlassToKey.Core/Keymap/KeymapStore.cs`
- `mac/GlassToKey/GlassToKey/Render/TrackpadSurfaceView.swift`

## NuGet dependencies

The application is intended to use open-source dependencies including Avalonia, CommunityToolkit.Mvvm, SkiaSharp, Svg.Skia, xUnit, and Microsoft.NET.Test.Sdk. Their licenses remain governed by their respective projects and packages.
