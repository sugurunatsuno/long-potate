# ADR 0001: C# / Avalonia / Skia architecture

- Status: Accepted
- Date: 2026-09-14

## Context

The application must reproduce GlassToKey geometry precisely, provide an interactive desktop preview, and export physical-size SVG, PDF, and PNG files. The upstream repository already includes a C# implementation of its layout model.

## Decision

Use:

- C# 14 / .NET 10 LTS;
- Avalonia UI for the desktop shell;
- a UI-independent Core library for GlassToKey import and geometry;
- a separate Rendering library using SkiaSharp for PDF/PNG;
- an internal SVG writer for editable SVG export;
- Svg.Skia only for importing user-supplied SVG artwork.

## Consequences

Positive:

- less translation risk from upstream C# geometry behavior;
- cross-platform desktop support;
- shared scene model across preview and all export formats;
- physical units remain explicit;
- renderer dependencies stay outside the compatibility core.

Trade-offs:

- Avalonia and Svg.Skia share a graphics dependency chain that must be regression-tested during upgrades;
- macOS-native UI appearance will be close to, but not identical to, an AppKit-only application;
- editable SVG export requires maintaining a small custom writer.

## Rejected alternatives

### Swift/AppKit

Excellent for macOS, but would make Windows/Linux support expensive and would require translating the upstream C# geometry model anyway.

### TypeScript + Tauri

Strong editor tooling, but adds a Rust/web boundary and requires a full geometry port away from the upstream C# implementation.

### WPF

Simple C# desktop development but Windows-only.
