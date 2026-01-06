using System;
using System.Collections.Generic;
using Msdfgen;
// using Msdfgen.Extensions; // Referenced project
using SixLabors.Fonts; // Referenced via Extensions or directly if added

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
        private Shape.Bounds _bounds;
        private double _advance;
        
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

        public bool Load(Font font, double geometryScale, char character, bool preprocessGeometry = true)
        {
            // Msdfgen.Extensions.FontLoader must be available.
            // Assuming FontLoader.LoadShape returns a Shape. 
            // We need to handle dependencies.
            // For now, using reflection or assuming the reference is there.
            
            // Note: Msdfgen.Extensions.FontLoader.LoadShape(font, character) returns Shape.
            // We need advance. SixLabors.Fonts can provide it.
            
            _shape = Msdfgen.Extensions.FontLoader.LoadShape(font, character);
            if (_shape != null && _shape.Validate())
            {
                _index = 0; // We don't easily get index from char without more logic, placeholder
                _codepoint = character;
                _geometryScale = geometryScale;

                // Get advance
                if (font.TryGetGlyph(new SixLabors.Fonts.Unicode.CodePoint(character), out var glyph)) 
                {
                    // Advance width in font units? or pixels?
                    // SixLabors returns metrics in em units usually if accessing helper, but Glyph instance might have it.
                    // Actually font.TryGetGlyph returns a GlyphInstance if we render, but the struct Glyph has 'Bounds' etc.
                    // We need to check units.
                    // For now, let's assume standard behavior or fix later.
                    // Shape from FontLoader is already scaled? No, FontLoader uses font size?
                    // FontLoader uses `new TextOptions(font)`.
                    // The shape coordinates depend on font size passed to `font`.
                    // If we want normalized coordinates, we should use a standard size or normalize.
                    // MSDFGen usually works with normalized or consistent units.
                    
                    // C++: loadGlyph(..., FONT_SCALING_NONE, &advance).
                    // FontLoader uses whatever the font is set to.
                    
                    _advance = glyph.Instance.AdvanceWidth; // Need to verify API of SixLabors.Fonts 2.x/3.x
                }
                
                _advance *= geometryScale;

                if (preprocessGeometry)
                {
                    // C# Shape doesn't implement resolveShapeGeometry (Skia)
                    // But it has Normalize()
                }
                _shape.Normalize();
                _bounds = _shape.GetBounds();

                // Validation/Winding check ignored for now or assumed correct by FontLoader
                
                return true;
            }
            return false;
        }

        public void EdgeColoring(EdgeColoringDelegate fn, double angleThreshold, ulong seed)
        {
            fn(_shape, angleThreshold, seed);
        }

        public void WrapBox(GlyphAttributes glyphAttributes)
        {
            double scale = glyphAttributes.Scale * _geometryScale;
            Msdfgen.Range range = glyphAttributes.Range / _geometryScale;
            Padding fullPadding = (glyphAttributes.InnerPadding + glyphAttributes.OuterPadding) / _geometryScale;
            
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
                double invBoxScale = 1 / _box.Scale;
                l = _geometryScale * (-_box.Translate.X + (_box.OuterPadding.L + 0.5) * invBoxScale);
                b = _geometryScale * (-_box.Translate.Y + (_box.OuterPadding.B + 0.5) * invBoxScale);
                r = _geometryScale * (-_box.Translate.X + (-_box.OuterPadding.R + _box.Rect.W - 0.5) * invBoxScale);
                t = _geometryScale * (-_box.Translate.Y + (-_box.OuterPadding.T + _box.Rect.H - 0.5) * invBoxScale);
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
    }
}
