using System;
using System.Collections.Generic;
using Msdfgen;
using Typography.OpenFont;

namespace MsdfAtlasGen
{
    public class GlyphGeometry
    {
        public struct GlyphAttributes
        {
            public double Scale;
            public Msdfgen.Range Range;
            public Padding InnerPadding;
            public Padding OuterPadding;
            public double MiterLimit;
            public bool PxAlignOriginX;
            public bool PxAlignOriginY;
        }

        private int _index;
        private int _codepoint;
        private double _geometryScale;
        private Shape _shape;
        private Shape.Bounds _bounds; // scaled bounds in pixels at target font size
        private double _advance;      // scaled advance in pixels at target font size
        
        // Store original metrics before geometry scaling for FNT export
        private double _advanceUnscaled;   // advance in pixels at target font size (no extra scaling later)
        private Shape.Bounds _boundsUnscaled; // bounds in font-space (fontSize = 1.0) for reference
        
        private struct Box
        {
            public Rectangle Rect;
            public Msdfgen.Range Range;
            public double Scale;
            public Vector2 Translate;
            public Padding OuterPadding;
        }
        private Box _box;

        public GlyphGeometry()
        {
        }

        public bool Load(Typeface typeface, double geometryScale, char character, bool preprocessGeometry = true)
        {
            // Build glyph shape directly from Typography.OpenFont
            _shape = BuildShape(typeface, (int)character);
            if (_shape != null && _shape.Validate())
            {
                _codepoint = character;
                _geometryScale = geometryScale;
                // Resolve glyph index and advance from OpenFont
                ushort glyphIndex = typeface.GetGlyphIndex((int)character);
                _index = glyphIndex;
                double adv = typeface.GetAdvanceWidthFromGlyphIndex(glyphIndex);
                _advanceUnscaled = adv * _geometryScale;
                _advance = _advanceUnscaled;

                if (preprocessGeometry)
                {
                    // C# Shape doesn't implement resolveShapeGeometry (Skia)
                    // But it has Normalize()
                }
                _shape.Normalize();

                // Bounds from the rendered shape (already in target pixel scale for the input font)
                _bounds = _shape.GetBounds();
                _boundsUnscaled = _bounds;

                // Validation/Winding check ignored for now
                
                return true;
            }
            return false;
        }

        private static Shape BuildShape(Typeface typeface, int codepoint)
        {
            ushort glyphIndex = typeface.GetGlyphIndex(codepoint);
            return BuildShapeByGlyphIndex(typeface, glyphIndex);
        }

        private static Shape BuildShapeByGlyphIndex(Typeface typeface, ushort glyphIndex)
        {
            var shape = new Shape();
            var tx = new ShapeGlyphTranslator(shape);

            var glyph = typeface.GetGlyph(glyphIndex);
            if (glyph == null)
                return shape;

            if (glyph.GlyphPoints != null && glyph.EndPoints != null)
            {
                Typography.OpenFont.IGlyphTranslator translator = tx;
                Typography.OpenFont.IGlyphReaderExtensions.Read(translator, glyph.GlyphPoints, glyph.EndPoints, 1f);
            }

            shape.SetYAxisOrientation(YAxisOrientation.Upward);
            return shape;
        }

        private sealed class ShapeGlyphTranslator : Typography.OpenFont.IGlyphTranslator
        {
            private readonly Shape _shape;
            private Contour _current;
            private Msdfgen.Vector2 _currentPoint;
            private Msdfgen.Vector2 _startPoint;
            private bool _contourOpen;

            public ShapeGlyphTranslator(Shape shape)
            {
                _shape = shape;
            }

            public void BeginRead(int contourCount)
            {
                _current = null;
                _contourOpen = false;
            }

            public void EndRead()
            {
            }

            public void MoveTo(float x0, float y0)
            {
                _current = new Contour();
                _shape.AddContour(_current);
                _startPoint = new Msdfgen.Vector2(x0, y0);
                _currentPoint = _startPoint;
                _contourOpen = true;
            }

            public void LineTo(float x1, float y1)
            {
                if (!_contourOpen) return;
                var p1 = new Msdfgen.Vector2(x1, y1);
                if (!ApproximatelyEqual(_currentPoint, p1))
                {
                    _current.AddEdge(new LinearSegment(_currentPoint, p1));
                    _currentPoint = p1;
                }
            }

            public void Curve3(float x1, float y1, float x2, float y2)
            {
                if (!_contourOpen) return;
                var c = new Msdfgen.Vector2(x1, y1);
                var p = new Msdfgen.Vector2(x2, y2);
                if (!ApproximatelyEqual(_currentPoint, p))
                {
                    _current.AddEdge(new QuadraticSegment(_currentPoint, c, p));
                    _currentPoint = p;
                }
            }

            public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                if (!_contourOpen) return;
                var c1 = new Msdfgen.Vector2(x1, y1);
                var c2 = new Msdfgen.Vector2(x2, y2);
                var p = new Msdfgen.Vector2(x3, y3);
                if (!ApproximatelyEqual(_currentPoint, p) || Msdfgen.Vector2.CrossProduct(c1 - p, c2 - p) != 0)
                {
                    _current.AddEdge(new CubicSegment(_currentPoint, c1, c2, p));
                    _currentPoint = p;
                }
            }

            public void CloseContour()
            {
                if (_contourOpen)
                {
                    if (!ApproximatelyEqual(_currentPoint, _startPoint))
                    {
                        _current.AddEdge(new LinearSegment(_currentPoint, _startPoint));
                        _currentPoint = _startPoint;
                    }
                    _contourOpen = false;
                }
            }

            private static bool ApproximatelyEqual(Msdfgen.Vector2 a, Msdfgen.Vector2 b)
            {
                return Math.Abs(a.X - b.X) < 0.0001 && Math.Abs(a.Y - b.Y) < 0.0001;
            }
        }

        public void EdgeColoring(EdgeColoringDelegate fn, double angleThreshold, ulong seed)
        {
            fn(_shape, angleThreshold, seed);
        }

        public void WrapBox(GlyphAttributes glyphAttributes)
        {
            // glyphAttributes.Scale is already pixels-per-font-unit for this glyph.
            double scale = glyphAttributes.Scale;
            Msdfgen.Range range = glyphAttributes.Range;
            Padding fullPadding = glyphAttributes.InnerPadding + glyphAttributes.OuterPadding;
            
            _box.Range = range;
            _box.Scale = scale;

            if (_bounds.L < _bounds.R && _bounds.B < _bounds.T)
            {
                double l = _bounds.L, b = _bounds.B, r = _bounds.R, t = _bounds.T;
                l += range.Lower; b += range.Lower;
                r -= range.Lower; t -= range.Lower;
                
                if (glyphAttributes.MiterLimit > 0)
                    _shape.BoundMiters(ref l, ref b, ref r, ref t, -range.Lower, glyphAttributes.MiterLimit, 1);
                
                l -= fullPadding.L; b -= fullPadding.B;
                r += fullPadding.R; t += fullPadding.T;

                if (glyphAttributes.PxAlignOriginX)
                {
                    int sl = (int)Math.Floor(scale * l - 0.5);
                    int sr = (int)Math.Ceiling(scale * r + 0.5);
                    _box.Rect.W = sr - sl;
                    _box.Translate.X = -sl / scale;
                }
                else
                {
                    double w = scale * (r - l);
                    _box.Rect.W = (int)Math.Ceiling(w) + 1;
                    _box.Translate.X = -l + 0.5 * (_box.Rect.W - w) / scale;
                }

                if (glyphAttributes.PxAlignOriginY)
                {
                    int sb = (int)Math.Floor(scale * b - 0.5);
                    int st = (int)Math.Ceiling(scale * t + 0.5);
                    _box.Rect.H = st - sb;
                    _box.Translate.Y = -sb / scale;
                }
                else
                {
                    double h = scale * (t - b);
                    _box.Rect.H = (int)Math.Ceiling(h) + 1;
                    _box.Translate.Y = -b + 0.5 * (_box.Rect.H - h) / scale;
                }
                _box.OuterPadding = glyphAttributes.Scale * glyphAttributes.OuterPadding;
            }
            else
            {
                _box.Rect.W = 0; _box.Rect.H = 0;
                _box.Translate = new Vector2(0, 0);
            }
        }

        public void PlaceBox(int x, int y)
        {
            _box.Rect.X = x;
            _box.Rect.Y = y;
        }

        public int GetIndex() => _index;
        public int GetCodepoint() => _codepoint;
        public double GetGeometryScale() => _geometryScale;
        public Shape GetShape() => _shape;
        public Shape.Bounds GetShapeBounds() => _bounds;
        public double GetAdvance() => _advance;
        
        public Rectangle GetBoxRect() => _box.Rect;
        public void GetBoxRect(out int x, out int y, out int w, out int h)
        {
            x = _box.Rect.X; y = _box.Rect.Y;
            w = _box.Rect.W; h = _box.Rect.H;
        }
        
        public Msdfgen.Range GetBoxRange() => _box.Range;
        public Projection GetBoxProjection() => new Projection(new Vector2(_box.Scale, _box.Scale), _box.Translate);
        public double GetBoxScale() => _box.Scale;
        public Vector2 GetBoxTranslate() => _box.Translate;

        public void GetQuadPlaneBounds(out double l, out double b, out double r, out double t)
        {
            if (_box.Rect.W > 0 && _box.Rect.H > 0)
            {
                // Convert back to pixel space: translate is stored in font units, Scale is pixels-per-unit.
                double s = _box.Scale;
                l = -_box.Translate.X * s + _box.OuterPadding.L + 0.5;
                b = -_box.Translate.Y * s + _box.OuterPadding.B + 0.5;
                r = -_box.Translate.X * s + (-_box.OuterPadding.R + _box.Rect.W - 0.5);
                t = -_box.Translate.Y * s + (-_box.OuterPadding.T + _box.Rect.H - 0.5);
            }
            else
            {
                l = b = r = t = 0;
            }
        }

        public void GetQuadAtlasBounds(out double l, out double b, out double r, out double t)
        {
             if (_box.Rect.W > 0 && _box.Rect.H > 0)
            {
                l = _box.Rect.X + _box.OuterPadding.L + 0.5;
                b = _box.Rect.Y + _box.OuterPadding.B + 0.5;
                r = _box.Rect.X - _box.OuterPadding.R + _box.Rect.W - 0.5;
                t = _box.Rect.Y - _box.OuterPadding.T + _box.Rect.H - 0.5;
            }
            else
            {
                l = b = r = t = 0;
            }
        }

        public bool IsWhitespace() => _shape.Contours.Count == 0;
        
        public void GetBoxSize(out int w, out int h)
        {
            w = _box.Rect.W;
            h = _box.Rect.H;
        }

        public GlyphBox ToGlyphBox() 
        {
             var box = new GlyphBox();
             box.Index = _index;
             box.Advance = _advance;
             // GlyphBounds is a struct, access fields directly on the field
             box.Bounds = new GlyphBox.GlyphBounds(); 
             GetQuadPlaneBounds(out box.Bounds.L, out box.Bounds.B, out box.Bounds.R, out box.Bounds.T);
             box.Rect = _box.Rect;
             return box;
        }

        // Accessors for original unscaled metrics (for FNT export)
        public double GetAdvanceUnscaled() => _advanceUnscaled; // already in pixels at target size
        public Shape.Bounds GetBoundsUnscaled() => _boundsUnscaled; // font-space (FontSize=1)
        public Shape.Bounds GetBoundsScaled() => _bounds; // pixel-space at target size
    }
}
