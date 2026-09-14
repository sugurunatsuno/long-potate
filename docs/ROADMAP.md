# Roadmap

## Milestone 0: Repository and specification

- [x] Choose C# / .NET / Avalonia / Skia architecture.
- [x] Record product specification.
- [x] Record upstream compatibility formulas.
- [x] Create project skeleton.

## Milestone 1: Compatibility core

- [x] Outer export parser.
- [x] Nested KeymapJson parser.
- [x] Trackpad presets.
- [x] Column settings compatibility.
- [x] Conventional layout builder.
- [ ] Key geometry overrides.
- [x] Left-side mirror.
- [x] Custom buttons.
- [ ] Numeric regression fixtures.

Exit condition: normalized geometry matches GlassToKey reference output.

## Milestone 2: Compatibility preview

- [x] Avalonia preview surface.
- [x] Grid keys and rotations.
- [x] Custom buttons.
- [x] Labels.
- [x] 30 x 22 debug grid.
- [x] Zoom/pan.

Exit condition: reference screenshots can be compared meaningfully with GlassToKey Config view.

## Milestone 3: Print scene

- [x] Device-to-mm mapping.
- [x] Page model.
- [x] A4/A3/Letter/custom.
- [x] Trackpad placement.
- [x] Background color.
- [x] PNG/JPEG artwork.
- [x] SVG artwork.
- [x] Key styling.
- [x] Calibration guides.

## Milestone 4: Export

- [x] Editable SVG renderer.
- [x] PDF renderer.
- [x] PNG renderer.
- [x] DPI/resolution warnings.
- [ ] Physical print verification.

## Milestone 5: Project editing

- [x] `.gtprint.json` project persistence.
- [x] Artwork inspector.
- [x] Theme presets.
- [x] Source JSON reload.
- [x] Recent files.

## Later

- [ ] fixed mobile QWERTY support;
- [ ] two-trackpad sheet composition;
- [ ] multi-page layer sheets;
- [ ] CLI/batch export;
- [ ] template/theme sharing.
