# GlassToKey Print Studio

GlassToKey Print Studio is a desktop application for turning exported GlassToKey / Apple Magic TouchstreamLP configuration JSON into print-ready Magic Trackpad keyboard overlays.

The project intentionally separates GlassToKey-compatible geometry from print design. The geometry layer reproduces GlassToKey key placement, while the presentation layer adds backgrounds, illustrations, labels, guides, calibration marks, and export settings.

## Goals

- Import GlassToKey exported JSON, including the nested `KeymapJson` payload.
- Reproduce GlassToKey geometry for supported layouts.
- Provide a compatibility preview for geometry verification.
- Provide a print preview with backgrounds, artwork, key styling, and guides.
- Export SVG, PDF, and PNG.
- Preserve physical dimensions so 100% PDF printing can be overlaid on a Magic Trackpad.

## Initial supported layouts

- Blank
- 5x3
- 5x4
- 6x3
- 6x4
- mobile-ortho-12x4 / Planck

The `mobile` fixed staggered QWERTY layout is intentionally deferred from the MVP because GlassToKey uses a separate layout-generation path for it.

## Technology

- C# 14
- .NET 10 LTS
- Avalonia UI 12.1.2
- CommunityToolkit.Mvvm 8.4.2
- SkiaSharp 4.151.2
- Svg.Skia 5.2.3
- xUnit v3 4.0.0

See [`docs/TECH_STACK.md`](docs/TECH_STACK.md) and [`docs/adr/0001-csharp-avalonia-skia.md`](docs/adr/0001-csharp-avalonia-skia.md) for the reasoning.

## Architecture

```text
GlassToKey export JSON
        |
        v
GlassToKey-compatible parser
        |
        v
GlassToKey-compatible geometry engine
        |
        v
Normalized geometry
   |             |
   v             v
Compatibility   Print scene
preview             |
                    +--> SVG
                    +--> PDF
                    +--> PNG
```

The geometry engine must not know anything about paper sizes, backgrounds, artwork, PDF, or PNG. The renderer must not recalculate key positions.

## Run the desktop app

```bash
dotnet run --project src/GlassToKey.PrintStudio.Desktop/GlassToKey.PrintStudio.Desktop.csproj
```

The UI itself lives in the shared `GlassToKey.PrintStudio.App` project so Desktop and Browser hosts render the same Avalonia view.

## Remote UI over SSH

A Browser/WASM host is included for development on a headless machine reachable only by SSH.

On the SSH host, install the workload once and start the UI:

```bash
dotnet workload install wasm-tools
./scripts/run-remote-ui.sh
```

From the client machine, create a local port forward:

```bash
ssh -L 5180:127.0.0.1:5180 <user>@<ssh-host>
```

Then open `http://127.0.0.1:5180` in the client's browser. The development server binds only to loopback on the SSH host.

See [`docs/REMOTE_UI.md`](docs/REMOTE_UI.md) for details and limitations.

## Documentation

- [`docs/SPECIFICATION.md`](docs/SPECIFICATION.md) — product and implementation specification
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — application architecture
- [`docs/TECH_STACK.md`](docs/TECH_STACK.md) — chosen language and libraries
- [`docs/UPSTREAM_COMPATIBILITY.md`](docs/UPSTREAM_COMPATIBILITY.md) — GlassToKey geometry compatibility notes
- [`docs/REMOTE_UI.md`](docs/REMOTE_UI.md) — Browser/WASM UI over an SSH tunnel
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — implementation phases

## Upstream project

This project is designed around exported configuration data from AppleMagicTouchstreamLP / GlassToKey:

https://github.com/disarmyouwitha/AppleMagicTouchstreamLP

GlassToKey remains the source of truth for key geometry. This application is intended to reproduce that geometry for printing, not to replace GlassToKey's layout editor.

See [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) before copying or adapting upstream source code.

## Repository status

This repository currently contains the architecture, specification, project skeleton, and initial geometry primitives. The next milestone is implementing the GlassToKey export parser and a compatibility-tested `LayoutBuilder` port.
