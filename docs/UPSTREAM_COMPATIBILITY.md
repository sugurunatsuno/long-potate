# GlassToKey upstream compatibility

## Reference

Behavior was reviewed against AppleMagicTouchstreamLP / GlassToKey at commit:

`c20233c6718afd2734f2a95d509bfe44d7e7f7b0`

Primary reference files:

- [LayoutBuilder.cs](https://github.com/disarmyouwitha/AppleMagicTouchstreamLP/blob/c20233c6718afd2734f2a95d509bfe44d7e7f7b0/windows_linux/GlassToKey.Core/Layout/LayoutBuilder.cs)
- [TrackpadLayoutPreset.cs](https://github.com/disarmyouwitha/AppleMagicTouchstreamLP/blob/c20233c6718afd2734f2a95d509bfe44d7e7f7b0/windows_linux/GlassToKey.Core/Layout/TrackpadLayoutPreset.cs)
- [ColumnLayoutSettings.cs](https://github.com/disarmyouwitha/AppleMagicTouchstreamLP/blob/c20233c6718afd2734f2a95d509bfe44d7e7f7b0/windows_linux/GlassToKey.Core/Layout/ColumnLayoutSettings.cs)
- [KeyLayout.cs](https://github.com/disarmyouwitha/AppleMagicTouchstreamLP/blob/c20233c6718afd2734f2a95d509bfe44d7e7f7b0/windows_linux/GlassToKey.Core/Layout/KeyLayout.cs)
- [RuntimeConfigurationFactory.cs](https://github.com/disarmyouwitha/AppleMagicTouchstreamLP/blob/c20233c6718afd2734f2a95d509bfe44d7e7f7b0/windows_linux/GlassToKey.Core/Runtime/RuntimeConfigurationFactory.cs)
- [KeymapStore.cs](https://github.com/disarmyouwitha/AppleMagicTouchstreamLP/blob/c20233c6718afd2734f2a95d509bfe44d7e7f7b0/windows_linux/GlassToKey.Core/Keymap/KeymapStore.cs)
- [TrackpadSurfaceView.swift](https://github.com/disarmyouwitha/AppleMagicTouchstreamLP/blob/c20233c6718afd2734f2a95d509bfe44d7e7f7b0/mac/GlassToKey/GlassToKey/Render/TrackpadSurfaceView.swift)

## Device constants

Compatibility defaults:

```text
trackpadWidthMm  = 160.0
trackpadHeightMm = 114.9
baseKeyWidthMm   = 18.0
baseKeyHeightMm  = 17.0
```

These should live in a device profile even though the first implementation uses one profile.

## Preset anchors

For the conventional 5/6-column layouts, the upstream implementation defines millimeter anchors for each column. For 6x3 and 6x4 the right-side anchors are:

```text
(35.0, 20.9)
(53.0, 19.2)
(71.0, 17.5)
(89.0, 19.2)
(107.0, 22.6)
(125.0, 22.6)
```

5-column layouts use the first five anchors.

Planck (`mobile-ortho-12x4`) uses 12 anchors and remains part of the normal column-settings path.

The `mobile` preset uses a separate fixed staggered QWERTY path and is not required for the initial MVP.

## Column size

Per column:

```text
widthMm  = baseKeyWidthMm  * ScaleX
heightMm = baseKeyHeightMm * ScaleY
```

`Scale` is a legacy alias. When deserializing, `ScaleX` and `ScaleY` take precedence when present; otherwise the legacy `Scale` value supplies both axes.

## Vertical placement

For row `r`:

```text
spacingScale = clamp(KeyPaddingPercent, 0, 200) / 100
spacingYmm   = heightMm * spacingScale
rowSpacingMm = heightMm * (RowSpacingPercent / 100)

yMm = anchorY + r * (heightMm + rowSpacingMm + spacingYmm)
```

`KeyPaddingPercent` is therefore part of row pitch in the conventional layout builder; it is not implemented as a visual inset of the key rectangle.

## Normalization

Before offsets:

```text
x = anchorX / trackpadWidthMm
y = yMm / trackpadHeightMm
width  = widthMm / trackpadWidthMm
height = heightMm / trackpadHeightMm
```

## Column offsets

Offsets are applied directly in normalized trackpad coordinates:

```text
x += OffsetXPercent / 100
y += OffsetYPercent / 100
```

They are not percentages of key width or key height.

## Column rotation

The configured value is sign-inverted before application.

```text
rotation = -clamp(RotationDegrees, 0, 360)
```

The pivot is:

```text
pivotX = first row key centerX
pivotY = (first row key centerY + last row key centerY) / 2
```

Each key center in the column is rotated around this shared pivot. The rectangle keeps its original width/height plus a rotation value; it is not replaced by its axis-aligned bounding box.

## Per-key geometry overrides

After column layout/offset/rotation, a grid key may apply:

- WidthScale;
- HeightScale;
- RotationDegrees.

Width/height scaling preserves the key center. Individual rotation is around that key center.

## Mirroring for the left side

The upstream model builds the same basic geometry and mirrors the left side horizontally:

```text
x = 1 - x - width
rotation = -rotation
```

The label column order is mirrored as well.

## Custom buttons

Custom buttons carry normalized `Rect` values directly:

```text
X
Y
Width
Height
```

The print application should map them into device millimeters without running them through the column layout algorithm.

They also carry:

- ID;
- side;
- layer;
- primary action;
- optional hold action;
- hold force threshold.

## Drawing model

The macOS upstream display receives already-generated `normalizedKeyRects` and draws those rectangles. This confirms that the print application should calculate geometry once, then reuse it for preview and export.

## Compatibility tests

At minimum, create numeric fixtures for:

- 5x3;
- 5x4;
- 6x3;
- 6x4;
- mobile-ortho-12x4;
- mirrored left side;
- custom buttons;
- rotation;
- per-key geometry overrides.

Tests compare normalized `x`, `y`, `width`, `height`, and rotation values with an explicit tolerance.
