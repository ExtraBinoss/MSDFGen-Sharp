# Msdfgen.Extensions

FreeType integration and utilities for glyph loading and metrics.

## Key Types
- FreetypeHandle: wraps FT_Library (initialize with `Initialize()`)
- FontHandle: wraps FT_Face (load with `LoadFont()` or `LoadFontData()`)
- FontLoader: static helpers mirroring the C++ msdfgen import-font
  - `GetFontMetrics(out metrics, font, FontCoordinateScaling.None)`
  - `GetGlyphIndex(out GlyphIndex, font, codepoint)`
  - `LoadGlyph(shape, font, glyphIndex | codepoint, FontCoordinateScaling.None, out advance)`
  - `GetKerning(out double, font, glyphIndex0, glyphIndex1)`

## Example (C#)
```csharp
using Msdfgen;
using Msdfgen.Extensions;

using var ft = FreetypeHandle.Initialize();
using var font = FontHandle.LoadFont(ft, "../test_fonts/Roboto-Regular.ttf");

FontLoader.GetFontMetrics(out var metrics, font);
var shape = new Shape();
FontLoader.LoadGlyph(shape, font, (uint)'b', FontCoordinateScaling.None, out double advance);
shape.Normalize();
```
