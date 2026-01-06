# MSDFGen-Sharp

A comprehensive C# port of the original [msdfgen](https://github.com/Chilly7/msdfgen) and [msdf-atlas-gen](https://github.com/Chilly7/msdf-atlas-gen) projects. This library provides high-quality Multi-channel Signed Distance Field (MSDF) generation for fonts and vector graphics.

## Project Structure

- **[MsdfAtlasGen.Cli](file:///c:/Users/binos/Documents/Personal_project/MSDFGen-Sharp/MsdfAtlasGen.Cli/README.md)**: The primary tool for generating font atlases (texture sheets) and metadata (FNT/JSON).
- **[Msdfgen.Cli](file:///c:/Users/binos/Documents/Personal_project/MSDFGen-Sharp/Msdfgen.Cli/README.md)**: A diagnostic tool for generating single-glyph MSDFs and testing core features.
- **Msdfgen.Core**: The core MSDF generation library (math, edge coloring, etc.).
- **MsdfAtlasGen**: The library for packing and atlas generation logic.

## Key Features

- **Multi-channel Signed Distance Fields (MSDF)**: Superior sharpness for text rendering at any scale.
- **Font Atlas Generation**: Efficiently pack multiple glyphs into a single texture.
- **Advanced Error Correction**: Integrated "Indiscriminate" and "Auto" modes to eliminate artifacts.
- **Overlap Support**: Properly handles complex glyphs with self-intersecting contours.
- **Multi-threaded**: Built-in support for parallel glyph generation.
- **Compatibility**: Generates standard BMFont (`.fnt`), JSON, and CSV metadata.

## 🛠 Building and Running

This project requires **.NET 9.0 SDK**.

```bash
# Clone the repository
git clone https://github.com/ExtraBinoss/MSDFGen-Sharp.git
cd MSDFGen-Sharp

# Run the Atlas Generator
cd MsdfAtlasGen.Cli
dotnet run -- -font "../test_fonts/Roboto-Regular.ttf" -allglyphs -fnt
```

---

For detailed usage, please see the READMEs in the respective CLI folders.
