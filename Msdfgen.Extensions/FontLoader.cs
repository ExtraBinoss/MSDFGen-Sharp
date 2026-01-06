using System;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts;
using Msdfgen;

namespace Msdfgen.Extensions
{
    public static class FontLoader
    {
        public static Shape LoadShape(Font font, char character)
        {
            var glyph = font.GetGlyph(character);
            var shape = new Shape();
            
            // SixLabors.Fonts exposes the path via a simpler API in recent versions (GetGlyph().Instance.Path or via renderer)
            // But usually we need to implement IGlyphRenderer to capture the moves.
            
            var renderer = new ShapeRenderer(shape);
            var dpi = 72; // DPI doesn't strictly matter for shape extraction as we normalize/scale, but 72 is standard.
            
            // We need to check exact API for rendering a specific glyph.
            // GlyphInstance has Render method.
            
            var glyphInstance = glyph.Instance;
            TextRenderer.Render(renderer, character.ToString(), new TextOptions(font));

            return shape;
        }

        private class ShapeRenderer : IGlyphRenderer
        {
            private readonly Shape _shape;
            private Contour _currentContour;
            private System.Numerics.Vector2 _startPoint;
            private System.Numerics.Vector2 _currentPoint;

            public ShapeRenderer(Shape shape)
            {
                _shape = shape;
            }

            public void BeginText(FontRectangle rect) { }
            public void EndText() { }
            
            public bool BeginGlyph(FontRectangle rect, GlyphRendererParameters parameters) 
            {
                // We could use this to init, but for single char it's fine.
                return true; 
            }
            
            public void EndGlyph() { }

            public void BeginFigure()
            {
                _currentContour = new Contour();
                _shape.AddContour(_currentContour);
            }

            public void MoveTo(System.Numerics.Vector2 point)
            {
                _startPoint = point;
                _currentPoint = point;
            }

            public void LineTo(System.Numerics.Vector2 point)
            {
                _currentContour.AddEdge(new LinearSegment(ToVector2(_currentPoint), ToVector2(point)));
                _currentPoint = point;
            }

            public void QuadraticBezierTo(System.Numerics.Vector2 secondControlPoint, System.Numerics.Vector2 point)
            {
                _currentContour.AddEdge(new QuadraticSegment(ToVector2(_currentPoint), ToVector2(secondControlPoint), ToVector2(point)));
                _currentPoint = point;
            }

            public void CubicBezierTo(System.Numerics.Vector2 secondControlPoint, System.Numerics.Vector2 thirdControlPoint, System.Numerics.Vector2 point)
            {
                 _currentContour.AddEdge(new CubicSegment(ToVector2(_currentPoint), ToVector2(secondControlPoint), ToVector2(thirdControlPoint), ToVector2(point)));
                 _currentPoint = point;
            }

            public void EndFigure()
            {
                // Implicit close if gap?
                // Msdfgen expects closed.
            }

            public void SetDecoration(TextDecorations decorations, System.Numerics.Vector2 start, System.Numerics.Vector2 end, float thickness)
            {
                // Ignore decorations
            }
            
            private static Msdfgen.Vector2 ToVector2(System.Numerics.Vector2 v)
            {
                // Invert Y? Font coordinates are usually Y-up (mathematically) or Y-down (screen).
                // SixLabors.Fonts uses Y-down (screen coords) relative to baseline usually?
                // Wait, TrueType is Y-Up. SixLabors normalizes to screen coords (Y-down).
                // MSDFGen expects standardized coordinates. 
                // We should probably just pass through and let MSDFGen Transform handle inversion via Config.
                return new Msdfgen.Vector2(v.X, -v.Y); // Flip Y to get standard Cartesian if needed?
                // Let's stick to raw and let user flip with arguments if needed or check later.
                // Standard MSDFGen CLI output usually requires Y up for texture coords or matching expectations.
                // TTF is Y-Up. If SixLabors converts to Y-Down, we might need to flip back.
                
                // Let's check SixLabors behavior.
            }
        }
    }
}
