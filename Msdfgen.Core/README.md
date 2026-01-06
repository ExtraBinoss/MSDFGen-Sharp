# Msdfgen.Core

Core multi-channel signed distance field (MSDF/MTSDF) primitives and generators.

## Modules
- Geometry: `Shape`, `Contour`, `EdgeSegment` variants
- Coloring: `EdgeColoring`, `EdgeSelectors`
- Generation: `MsdfGenerator`, `MultiDistance`, `SdfRenderer`
- Math & Utils: `Vector2`, `Range`, `Projection`, `EquationSolver`

## Typical Flow
1. Build `Shape` from font outline (via Msdfgen.Extensions)
2. `shape.Normalize()` and optionally `shape.OrientContours()`
3. Choose `Range` and `Projection` for target bitmap
4. Generate MSDF/MTSDF with `MsdfGenerator` into `Bitmap<float>`
5. Optional: render preview with `SdfRenderer`

## Example (C#)
```csharp
var shape = new Shape();
// ... populate shape from font ...
shape.Normalize();

var range = new Range(2.0);
var projection = Projection.FromBox(box, yOrientation: YAxisOrientation.Up);

var bmp = new Bitmap<float>(width, height, 3);
MsdfGenerator.GenerateMsdf(bmp, shape, projection, range);
```
