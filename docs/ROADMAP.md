# Roadmap

## Milestone 0: Repository and specification

- [x] Choose C# / .NET / Avalonia / Skia architecture.
- [x] Record product specification.
- [x] Record upstream compatibility formulas.
- [x] Create project skeleton.

## Milestone 1: Compatibility core

- [ ] Outer export parser.
- [ ] Nested KeymapJson parser.
- [ ] Trackpad presets.
- [ ] Column settings compatibility.
- [ ] Conventional layout builder.
- [ ] Key geometry overrides.
- [ ] Left-side mirror.
- [ ] Custom buttons.
- [ ] Numeric regression fixtures.

Exit condition: normalized geometry matches GlassToKey reference output.

## Milestone 2: Compatibility preview

- [ ] Avalonia preview surface.
- [ ] Grid keys and rotations.
- [ ] Custom buttons.
- [ ] Labels.
- [ ] 30 x 22 debug grid.
- [ ] Zoom/pan.

Exit condition: reference screenshots can be compared meaningfully with GlassToKey Config view.

## Milestone 3: Print scene

- [ ] Device-to-mm mapping.
- [ ] Page model.
- [ ] A4/A3/Letter/custom.
- [ ] Trackpad placement.
- [ ] Background color.
- [ ] PNG/JPEG artwork.
- [ ] SVG artwork.
- [ ] Key styling.
- [ ] Calibration guides.

## Milestone 4: Export

- [ ] Editable SVG renderer.
- [ ] PDF renderer.
- [ ] PNG renderer.
- [ ] DPI/resolution warnings.
- [ ] Physical print verification.

## Milestone 5: Project editing

- [ ] `.gtprint.json` project persistence.
- [ ] Artwork inspector.
- [ ] Theme presets.
- [ ] Source JSON reload.
- [ ] Recent files.

## Later

- [ ] fixed mobile QWERTY support;
- [ ] two-trackpad sheet composition;
- [ ] multi-page layer sheets;
- [ ] CLI/batch export;
- [ ] template/theme sharing.
