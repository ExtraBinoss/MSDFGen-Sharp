# MsdfAtlasGen.Cli

Generate multi-channel signed distance field (MSDF) font atlases using FreeType.

## Prerequisites
- .NET SDK 9.0
- Test font available at: ../test_fonts/Roboto-Regular.ttf

## Quick Start (Run from this folder)
```bash
#Best results
dotnet run -c Release -- -font "../test_fonts/Roboto-Regular.ttf" -size 90 -dimensions 1024 1024 -fnt -testrender -spacing 2 -aouterpxpadding 5 5 5 5

dotnet run -c Release -- -font "../test_fonts/Roboto-Regular.ttf" -size 90 -dimensions 1024 1024 -fnt -testrender -spacing 2 -miterlimit 3.0 -coloringstrategy simple -errorcorrection indiscriminate

# Minimal: PNG + FNT (BMFont) outputs
dotnet run -c Release -- -font "../test_fonts/Roboto-Regular.ttf" -size 90 -dimensions 1024 1024 -fnt

# PNG + JSON
dotnet run -c Release -- -font "../test_fonts/Roboto-Regular.ttf" -size 90 -dimensions 1024 1024 -imageout atlas.png -json atlas.json

# Type variants (sdf|psdf|msdf|mtsdf)
dotnet run -c Release -- -font "../test_fonts/Roboto-Regular.ttf" -size 90 -dimensions 1024 1024 -type msdf -fnt
```

## Common Options
```bash
# Add spacing between glyph boxes
-spacing 2

# Outer pixel padding per side (L B R T)
-aouterpxpadding 5 5 5 5

# Distance field range in pixels
-pxrange 4

# Y origin (bottom or top)
-yorigin top

# Threads (0 = auto)
-threads 0
```

## Selecting Glyphs
```bash
# All glyphs in font
-allglyphs

# Inline characters
-chars "abcd0123"

# From charset file
-charset "../test_fonts/charset.txt"
```

## Debug Single Glyph Dump
Dump a single glyph’s raw MSDF and rendered view alongside the atlas:
```bash
# By character
dotnet run -c Release -- -font "../test_fonts/Roboto-Regular.ttf" -size 90 -dimensions 1024 1024 -type msdf -fnt -debugglyph b

# By hex codepoint
dotnet run -c Release -- -font "../test_fonts/Roboto-Regular.ttf" -size 90 -dimensions 1024 1024 -type msdf -fnt -debugglyph 0x0062
```
Outputs are placed in output/GlyphDump/.

## Output Locations
- PNG/FNT: output/Fnt/
- JSON/CSV: output/Json/
- Test renders: output/Renders/
- Glyph dumps: output/GlyphDump/
