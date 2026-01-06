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
            var shape = new Shape();
            var renderer = new ShapeRenderer(shape);
            
            TextRenderer.RenderTextTo(renderer, character.ToString(), new TextOptions(font));
            
            // Set Y-axis orientation to match C++ behavior
            shape.SetYAxisOrientation(YAxisOrientation.Upward);
            
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
                _currentContour = new Contour(); // Placeholder, re-assigned in BeginFigure
            }

            public void BeginText(in FontRectangle rect) { }
            public void EndText() { }
            
            public bool BeginGlyph(in FontRectangle rect, in GlyphRendererParameters parameters) 
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
                // Filter degenerate edges (like C++ does)
                if (point != _currentPoint)
                {
                    _currentContour.AddEdge(new LinearSegment(ToVector2(_currentPoint), ToVector2(point)));
                    _currentPoint = point;
                }
            }

            public void QuadraticBezierTo(System.Numerics.Vector2 secondControlPoint, System.Numerics.Vector2 point)
            {
                // Filter degenerate edges
                if (point != _currentPoint)
                {
                    _currentContour.AddEdge(new QuadraticSegment(ToVector2(_currentPoint), ToVector2(secondControlPoint), ToVector2(point)));
                    _currentPoint = point;
                }
            }

            public void CubicBezierTo(System.Numerics.Vector2 secondControlPoint, System.Numerics.Vector2 thirdControlPoint, System.Numerics.Vector2 point)
            {
                // For cubic, also add if control points indicate a real curve (like C++ does)
                var p0 = ToVector2(_currentPoint);
                var p1 = ToVector2(secondControlPoint);
                var p2 = ToVector2(thirdControlPoint);
                var p3 = ToVector2(point);
                
                if (point != _currentPoint || Msdfgen.Vector2.CrossProduct(p1 - p3, p2 - p3) != 0)
                {
                    _currentContour.AddEdge(new CubicSegment(p0, p1, p2, p3));
                    _currentPoint = point;
                }
            }

            public void EndFigure()
            {
                // Remove empty contours (like C++ does at the end)
                if (_currentContour.Edges.Count == 0 && _shape.Contours.Contains(_currentContour))
                {
                    _shape.Contours.Remove(_currentContour);
                }
            }

            public void SetDecoration(TextDecorations decorations, System.Numerics.Vector2 start, System.Numerics.Vector2 end, float thickness)
            {
                // Ignore decorations
            }

            public TextDecorations EnabledDecorations() 
            {
                return TextDecorations.None;
            }
            
            private static Msdfgen.Vector2 ToVector2(System.Numerics.Vector2 v)
            {
                // SixLabors.Fonts uses screen coordinates (Y-down from baseline).
                // TrueType fonts are Y-up, so FreeType gives Y-up coordinates.
                // We need to flip Y to get Y-up coordinates matching FreeType.
                return new Msdfgen.Vector2(v.X, -v.Y);
            }
        }
    }
}

