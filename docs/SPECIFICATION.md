# Product specification

## Product name

GlassToKey Print Studio

## Purpose

GlassToKey Print Studio converts exported GlassToKey settings into physical-size print templates for Apple Magic Trackpad overlays.

The user should be able to print a keyboard guide directly, or build a styled overlay containing artwork, background imagery, labels, guides, and cut lines.

The application is not a replacement keymap editor. GlassToKey remains the source of truth for typing geometry and key mappings.

## Supported input

The primary input is a GlassToKey exported JSON document containing settings and a nested serialized `KeymapJson` payload.

The importer must:

- parse the outer JSON;
- validate version and required sections;
- parse `KeymapJson` as JSON a second time;
- preserve unknown fields where practical or ignore them safely;
- return actionable validation messages instead of generic parse errors.

## Initial supported layouts

Required for MVP:

- Blank;
- 5x3;
- 5x4;
- 6x3;
- 6x4;
- mobile-ortho-12x4 / Planck.

Deferred:

- mobile fixed staggered QWERTY.

## Layout and geometry requirements

Geometry must reproduce GlassToKey behavior documented in `UPSTREAM_COMPATIBILITY.md`.

The implementation must support:

- layout preset anchors;
- Scale / ScaleX / ScaleY compatibility;
- OffsetXPercent / OffsetYPercent;
- RowSpacingPercent;
- KeyPaddingPercentByLayout;
- column RotationDegrees;
- per-key WidthScale / HeightScale / RotationDegrees;
- left/right mirroring;
- custom normalized button rectangles;
- layers;
- primary and hold labels.

The geometry engine returns normalized values and does not depend on UI or rendering libraries.

## Physical device model

Default device profile:

```text
Width            160.0 mm
Height           114.9 mm
Base key width    18.0 mm
Base key height   17.0 mm
```

The profile must be modeled explicitly to allow future hardware variants.

## Coordinate systems

### Compatibility coordinates

Normalized trackpad coordinates from 0..1 plus rotation.

### Device coordinates

Millimeters relative to the selected device profile.

### Page coordinates

Millimeters relative to the selected output page.

Pixel and PDF-point conversion occurs only in renderers.

## Compatibility Preview

A non-decorated preview for comparing the application against GlassToKey.

Display options:

- trackpad outline;
- grid keys;
- custom buttons;
- primary labels;
- hold labels;
- row/column identifiers;
- left/right side;
- rotation;
- optional 30 x 22 diagnostic grid.

No user artwork is required in this mode.

## Print Preview

Uses the exact same compatibility geometry but adds presentation.

Supported visual layers:

- background;
- artwork;
- device outline;
- key fill;
- key border;
- custom buttons;
- primary labels;
- hold labels;
- secondary-layer labels;
- guides;
- cut lines.

## Backgrounds and artwork

MVP must support:

- solid background color;
- PNG;
- JPEG;
- SVG artwork.

Artwork properties:

- X in mm;
- Y in mm;
- width in mm;
- height in mm;
- rotation;
- opacity;
- z-order;
- clipping to trackpad area on/off.

Image placement modes should include Fit, Fill, Center, Stretch, and Original Size where applicable.

The application should warn when raster artwork does not provide enough pixels for the requested physical size at the selected export DPI.

## Key styling

Configurable properties:

- border visibility;
- border width in mm;
- border color;
- fill color;
- fill opacity;
- corner radius;
- primary text color;
- primary text size;
- secondary/hold text size;
- label positions.

Geometry edits are not part of the print editor in the MVP.

## Layers and labels

Users must be able to select a source layer.

Display modes:

- single layer;
- primary layer plus one secondary layer;
- separate pages per layer is a post-MVP feature.

Default label placement:

- Primary: center;
- Hold: lower secondary line;
- Secondary layer: top-right.

Long labels should shrink within an allowed range rather than overflow the key.

## Page setup

Supported page sizes:

- A4;
- A3;
- Letter;
- custom;
- exact device-size page.

The device is centered by default but can be positioned in millimeters.

## Print calibration

Optional calibration content:

- 10 mm bar;
- 50 mm bar;
- 100 mm bar;
- 10 mm square;
- device outline;
- center marks;
- crop/cut marks;
- `Print at 100% / Do not scale to fit` note.

## Export formats

### SVG

Editable master output.

Requirements:

- physical width/height in mm;
- viewBox;
- grouped scene layers;
- vector key rectangles and rotations;
- text remains editable when possible;
- configurable embedded vs linked artwork policy, with embedded as the safe default.

### PDF

Canonical print output.

Requirements:

- page dimensions preserved exactly;
- conversion from mm to points only at the renderer boundary;
- vector keys and text where supported;
- raster images placed at requested physical size;
- no implicit fit-to-page scaling.

### PNG

Preview/share output.

Selectable resolution:

- 150 DPI;
- 300 DPI default;
- 600 DPI.

## Project files

The editor should eventually save a Print Studio project separate from the GlassToKey export.

Suggested extension:

`.gtprint.json`

Project data should include:

- source export path and/or source hash;
- layout;
- layer selection;
- device profile;
- page setup;
- theme;
- artwork references;
- output settings.

When the source JSON is reloaded, print presentation should remain intact where references are still valid.

## UI

Three-pane desktop editor.

### Left pane

Document settings:

- source JSON;
- layout;
- layer;
- device;
- page;
- background;
- artwork;
- key style;
- guides;
- export.

### Center pane

Preview with mode switch:

- Compatibility;
- Print.

Navigation:

- zoom in/out;
- 100%;
- fit page;
- fit trackpad;
- pan.

### Right pane

Inspector for selected presentation objects.

Artwork inspector:

- position;
- size;
- rotation;
- opacity;
- z-order.

Source key geometry is read-only in MVP.

## Error handling

User-facing errors must distinguish at least:

- invalid outer JSON;
- missing KeymapJson;
- invalid nested KeymapJson;
- missing selected layout;
- unsupported layout mode;
- missing artwork file;
- unreadable image;
- PDF/SVG/PNG export failure;
- font resolution failure.

Unknown JSON fields should not make import fail by themselves.

## Testing requirements

### Geometry unit tests

Compare normalized values for known fixtures.

### Mirror tests

Verify horizontal geometry and rotation mirroring.

### Parser tests

Validate nested JSON, compatibility aliases, and unknown fields.

### Renderer tests

Verify page dimensions and that all expected scene layers are emitted.

### Visual regression

Create reference images for conventional layouts and Planck after the geometry engine is confirmed against GlassToKey.

### Physical print test

A 100% PDF print must align with the physical device outline and calibration measurements.

## MVP completion criteria

MVP is complete when:

- exported GlassToKey JSON opens successfully;
- required layouts reproduce GlassToKey geometry;
- custom buttons and per-key geometry are honored;
- Compatibility Preview and Print Preview share one geometry model;
- background/artwork can be placed;
- SVG, PDF, and PNG export from one Print Scene;
- PDF prints at true physical size;
- calibration elements can be added;
- geometry regression tests pass on macOS, Windows, and Linux CI.

## Out of scope for MVP

- live Magic Trackpad input;
- gestures;
- haptics;
- snap-radius behavior;
- typing intent detection;
- editing GlassToKey mappings;
- editing GlassToKey column geometry;
- fixed mobile QWERTY preset;
- advanced multi-page composition;
- arbitrary vector illustration editing.
