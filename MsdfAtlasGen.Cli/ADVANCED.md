# MSDF Atlas Gen - Advanced Technical Guide

This document provides deep-dives into the complex features of the MSDF Atlas Generator to help you achieve the highest quality results.

## 📐 Spacing vs. Padding

### Spacing (`-spacing <px>`)
Controlled by the **Packer**.
- **What it is**: The forced gap between glyph "boxes" during packing.
- **Why use it**: To prevent bleed between glyphs in the final texture when using bi-linear filtering or mipmaps. 
- **Effect**: If you set `-spacing 2`, every glyph will have at least 2 pixels of empty space between it and its neighbors.

### Padding (`-aouterpxpadding <L B R T>`)
Controlled by the **Generator**.
- **What it is**: Extra distance field area generated *beyond* the glyph bounds.
- **Why use it**: Some game engines expect a specific "margin" around the glyph inside its assigned rectangle.
- **Effect**: Unlike spacing, padding contains valid (negative) distance field data. This is useful for shaders that need to "glow" or "outline" the text.

---

## 🛠 Error Correction Strategies

Distance field generation sometimes creates "artifacts" (phantom lines or sharp corners that look clipped).

### `disabled`
No error correction. Fast but prone to "stray lines" on complex fonts.

### `auto` (Default)
Attempts to detect and fix artifacts only where typical patterns (like diagonal crossings) are found. Good for most standard fonts.

### `indiscriminate`
The most aggressive mode. It checks every pixel and corrects any potential multi-channel inconsistency. 
> [!TIP]
> **Use this if you see any "jagged" artifacts on letters like 'R' or 'A'.**

---

## 🔄 Overlap Support

Some fonts are designed with "self-intersecting" contours (e.g., a loop where the lines physically cross).
- **Default**: `Enabled`
- **Mechanism**: The tool calculates the winding number of every contour. It uses an `OverlappingContourCombiner` to ensure that even if two parts of a glyph overlap, the resulting distance field remains mathematically correct.
- **Impact**: Turning this off with `-nooverlap` can be slightly faster but will cause "hollow" or "filled" artifacts in script or complex display fonts.

---

## 📐 Miter Limit (`-miterlimit`)

Mitering affects how the bounding box of a glyph is calculated when it has sharp acute angles.
- **Value 1.0**: Highly conservative. Sharp inner corners might be clipped by the generator.
- **Value 3.0+**: Allows sharper corners to be included in the generation area.
- **Recommendation**: If your font has very "pointy" features (like Serifs or Calligraphy), use `-miterlimit 3.0`.

---

## ⌨️ Y-Origin and Coordinate Systems

Different engines use different coordinate systems for font rendering:
- **`bottom` (Upward)**: Standard for OpenGL, C++, and many 3D engines. Y increases upwards from the baseline.
- **`top` (Downward)**: Standard for Web, DirectX, and many UI frameworks (Unity, WPF). Y increases downwards from the top.

> [!NOTE]
> This affects both the `.fnt` (BMFont) metadata `yoffset` and the vertical orientation of the glyphs in the atlas if you are using specific packing settings.

---

## 🔍 Debugging with `-debugglyph`

If a specific character (e.g., `#`) looks broken in the atlas:
1. Run with `-debugglyph #`.
2. Check `output/GlyphDump/`.
3. You will find:
    - `raw_msdf_#.png`: The raw multi-channel field for just that glyph.
    - `render_#.png`: How that field looks when rendered.
    
This helps determine if the issue is in the **Packing** (placement) or the **Generation** (the math).
