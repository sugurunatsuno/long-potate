# Architecture

## Core principle

GlassToKey geometry and print presentation are separate concerns.

The application must never calculate key placement independently in each renderer. A single compatibility geometry pipeline produces normalized key regions, then a print scene maps those regions into physical page coordinates.

```text
GlassToKey export JSON
        |
        v
GlassToKeyExportParser
        |
        v
Compatibility model
        |
        v
GlassToKeyGeometryEngine
        |
        v
Normalized geometry (0..1 + rotation)
        |                       \
        |                        -> Compatibility Preview
        v
PrintSceneBuilder
        |
        v
Print Scene (millimeters)
   |        |        |
   v        v        v
 SVG      PDF      PNG
```

## Projects

### GlassToKey.PrintStudio.Core

No UI or graphics dependencies.

Responsibilities:

- exported JSON parsing;
- nested `KeymapJson` parsing;
- layout preset resolution;
- column settings;
- normalized rectangles and rotation;
- custom buttons;
- key mappings and labels;
- left/right mirroring;
- print-document domain model;
- scene model in physical units.

### GlassToKey.PrintStudio.Rendering

Depends on Core.

Responsibilities:

- SVG export;
- PDF export;
- PNG export;
- bitmap/image loading;
- SVG artwork loading;
- font resolution abstraction;
- renderer diagnostics such as low-resolution artwork warnings.

### GlassToKey.PrintStudio.App

Depends on Core and Rendering.

Responsibilities:

- file open/save;
- three-pane editor UI;
- compatibility/print preview toggle;
- theme and artwork editing;
- export commands;
- user-facing validation errors.

## Data coordinate systems

The application uses three coordinate systems.

### Normalized GlassToKey coordinates

`x`, `y`, `width`, and `height` are normalized to the trackpad area. Rotation remains in degrees.

### Device physical coordinates

Normalized coordinates are mapped to a device profile in millimeters.

Default profile:

- width: 160.0 mm;
- height: 114.9 mm;
- base key width: 18.0 mm;
- base key height: 17.0 mm.

### Page coordinates

The device is placed onto A4, A3, Letter, custom paper, or a device-size page in millimeters.

Only a renderer may convert millimeters to pixels or PDF points.

## Scene model

A Print Scene is ordered data, not drawing commands tied to one framework.

Recommended top-level layers:

```text
background
artwork
device
key-fill
key-border
custom-buttons
label-primary
label-hold
label-secondary
guides
cut-lines
```

Each renderer receives the same scene.

## Compatibility boundary

The compatibility implementation should be covered by numeric regression tests. Any change to geometry formulas requires test fixture updates and a written explanation in the pull request.

Presentation changes must not change compatibility geometry.
