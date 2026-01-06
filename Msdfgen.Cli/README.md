# Msdfgen.Cli

Render single-glyph distance fields (SDF/PSDF/MSDF/MTSDF) directly via FreeType.

## Prerequisites
- .NET SDK 9.0
- Test font available at: ../test_fonts/Roboto-Regular.ttf

## Modes
- sdf, psdf, msdf, mtsdf

## Quick Start (Run from this folder)
```bash
# Single-glyph MSDF for 'b' with autoframe and test render
dotnet run -c Release -- msdf -font "../test_fonts/Roboto-Regular.ttf" 'b' -dimensions 128 128 -pxrange 4 -autoframe -testrender

# SDF for 'd' and save to a specific file
dotnet run -c Release -- sdf -font "../test_fonts/Roboto-Regular.ttf" 'd' -dimensions 128 128 -pxrange 4 -autoframe -o raw_msdf_d.png

# PSDF (single channel) for 'k'
dotnet run -c Release -- psdf -font "../test_fonts/Roboto-Regular.ttf" 'k' -dimensions 128 128 -pxrange 4 -autoframe -testrender
```

## Useful Options
```bash
# Output file
-o output.png

# Output dimensions
-dimensions 128 128

# Pixel range (bigger = softer edge)
-pxrange 4

# Autoframe to fit glyph inside the image
-autoframe

# Render a preview image of the distance field
-testrender [render.png] [W H]

# Corner angle threshold (degrees: append D)
-angle 30D

# Print metrics and configuration
-printmetrics
```

## Output Locations
- Raw MSDF: Msdfgen.Cli/RawMsdf/
- Rendered views: Msdfgen.Cli/Render/
