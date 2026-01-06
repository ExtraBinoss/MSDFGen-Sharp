# MsdfAtlasGen

Library for packing glyphs and generating MSDF/MTSDF atlases.

## Key Types
- `FontGeometry`: loads glyphs/metrics/kerning via FreeType
- `GlyphGeometry`: holds glyph shape, advance, bounds, box
- `TightAtlasPacker`/`RectanglePacker`: pack glyph boxes into atlas
- `BitmapBlit`: fast blit with clipping into atlas bitmap
- Exporters: `FntExporter`, `JsonExporter`, `CsvExporter`

## Typical Usage
1. Load font via Msdfgen.Extensions `FontHandle`
2. Use `FontGeometry.LoadCharset(font, Charset.BasicLatin)`
3. Pack with `TightAtlasPacker`
4. Generate atlas bitmaps via `ImmediateAtlasGenerator`
5. Save outputs via MsdfAtlasGen.Cli `AtlasSaver`

## Notes
- Uses FreeTypeSharp for identical outline decomposition as C++
- Winding and normalization are handled per glyph before coloring
- Blit includes strict clipping to avoid bleed artifacts
