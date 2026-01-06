using System;
using System.Collections.Generic;
using Msdfgen;

namespace MsdfAtlasGen
{
    public class TightAtlasPacker
    {
        private int _width = -1;
        private int _height = -1;
        private int _spacing = 0;
        private DimensionsConstraint _dimensionsConstraint = DimensionsConstraint.PowerOfTwoSquare;
        private double _scale = -1;
        private double _minScale = 1;
        private Msdfgen.Range _unitRange = new Msdfgen.Range(0);
        private Msdfgen.Range _pxRange = new Msdfgen.Range(0);
        private double _miterLimit = 0;
        private bool _pxAlignOriginX = false;
        private bool _pxAlignOriginY = false;
        private double _scaleMaximizationTolerance = 0.001;
        private Padding _innerUnitPadding;
        private Padding _outerUnitPadding;
        private Padding _innerPxPadding;
        private Padding _outerPxPadding;

        public TightAtlasPacker()
        {
        }

        public int TryPack(GlyphGeometry[] glyphs, DimensionsConstraint dimensionsConstraint, ref int width, ref int height, double scale)
        {
            // Wrap glyphs into boxes
            var rectangles = new List<Rectangle>(glyphs.Length);
            var rectangleGlyphs = new List<GlyphGeometry>(glyphs.Length);
            
            var attribs = new GlyphGeometry.GlyphAttributes();
            attribs.Scale = scale;
            attribs.Range = _unitRange + _pxRange / scale;
            attribs.InnerPadding = _innerUnitPadding + _innerPxPadding / scale;
            attribs.OuterPadding = _outerUnitPadding + _outerPxPadding / scale;
            attribs.MiterLimit = _miterLimit;
            attribs.PxAlignOriginX = _pxAlignOriginX;
            attribs.PxAlignOriginY = _pxAlignOriginY;

            foreach (var glyph in glyphs)
            {
                if (!glyph.IsWhitespace())
                {
                    glyph.WrapBox(attribs);
                    glyph.GetBoxSize(out int w, out int h);
                    if (w > 0 && h > 0)
                    {
                        var rect = new Rectangle { W = w, H = h };
                        rectangles.Add(rect);
                        rectangleGlyphs.Add(glyph);
                    }
                }
            }

            if (rectangles.Count == 0)
            {
                if (width < 0 || height < 0)
                    width = height = 0;
                return 0;
            }

            var rectArray = rectangles.ToArray();

            // Box rectangle packing
            if (width < 0 || height < 0)
            {
                int w_out, h_out;
                switch (dimensionsConstraint)
                {
                    case DimensionsConstraint.PowerOfTwoSquare:
                        RectanglePacking.PackRectangles<SquarePowerOfTwoSizeSelector>(rectArray, _spacing, out w_out, out h_out);
                        break;
                    case DimensionsConstraint.PowerOfTwoRectangle:
                        RectanglePacking.PackRectangles<PowerOfTwoSizeSelector>(rectArray, _spacing, out w_out, out h_out);
                        break;
                    case DimensionsConstraint.MultipleOfFourSquare:
                         // We don't have SquareSizeSelector<4> generic in C# essentially. 
                         // We can instantiate SquareSizeSelector directly via Activator in PackRectangles helper, but we need to pass 'multiple'.
                         // My PackRectangles implementation uses parameterless ctor (or area only).
                         // I need to update RectanglePacking.PackRectangles or creating the selector manually here.
                         
                         // Manually creates selector:
                         {
                             long totalArea = 0;
                             var rectCopy = new Rectangle[rectArray.Length];
                             for(int i=0; i<rectArray.Length; ++i) {
                                 rectCopy[i] = rectArray[i];
                                 rectCopy[i].W += _spacing; rectCopy[i].H += _spacing;
                                 totalArea += (long)rectCopy[i].W * rectCopy[i].H;
                             }
                             var selector = new SquareSizeSelector((int)totalArea, 4);
                             RectanglePacking.PackRectangles(rectArray, rectCopy, selector, _spacing, out w_out, out h_out);
                         }
                        break;
                    case DimensionsConstraint.EvenSquare:
                         {
                             long totalArea = 0;
                             var rectCopy = new Rectangle[rectArray.Length];
                             for(int i=0; i<rectArray.Length; ++i) {
                                 rectCopy[i] = rectArray[i];
                                 rectCopy[i].W += _spacing; rectCopy[i].H += _spacing;
                                 totalArea += (long)rectCopy[i].W * rectCopy[i].H;
                             }
                             var selector = new SquareSizeSelector((int)totalArea, 2);
                             RectanglePacking.PackRectangles(rectArray, rectCopy, selector, _spacing, out w_out, out h_out);
                         }
                        break;
                    case DimensionsConstraint.Square:
                    default:
                         {
                             long totalArea = 0;
                             var rectCopy = new Rectangle[rectArray.Length];
                             for(int i=0; i<rectArray.Length; ++i) {
                                 rectCopy[i] = rectArray[i];
                                 rectCopy[i].W += _spacing; rectCopy[i].H += _spacing;
                                 totalArea += (long)rectCopy[i].W * rectCopy[i].H;
                             }
                             var selector = new SquareSizeSelector((int)totalArea, 1);
                             RectanglePacking.PackRectangles(rectArray, rectCopy, selector, _spacing, out w_out, out h_out);
                         }
                        break;
                }
                
                if (!(w_out > 0 && h_out > 0))
                    return -1;
                width = w_out;
                height = h_out;
            }
            else
            {
                if (RectanglePacking.PackRectangles(rectArray, width, height, _spacing) != 0)
                    return -1; // Or return count
            }

            // Set glyph box placement
            for (int i = 0; i < rectangles.Count; ++i)
                rectangleGlyphs[i].PlaceBox(rectArray[i].X, height - (rectArray[i].Y + rectArray[i].H));
            
            return 0;
        }

        private double PackAndScale(GlyphGeometry[] glyphs)
        {
            bool lastResult = false;
            int w = _width, h = _height;
            
            bool TryPackLocal(double s)
            {
                int lw = w, lh = h;
                bool res = TryPack(glyphs, default, ref lw, ref lh, s) == 0;
                lastResult = res;
                return res;
            }

            // Macro usage in C++: TRY_PACK(scale) -> lastResult = !tryPack(...) [returns 0 on success, so !0 is true]
            // My TryPack returns 0 on success.
            // C++ code: if (TRY_PACK(1)) ... means if success.
            
            double minScale = 1, maxScale = 1;
            if (TryPackLocal(1))
            {
                while (maxScale < 1e+32)
                {
                    maxScale = 2 * minScale;
                    if (TryPackLocal(maxScale))
                        minScale = maxScale;
                    else
                        break;
                }
            }
            else
            {
                while (minScale > 1e-32)
                {
                     minScale = 0.5 * maxScale;
                     if (!TryPackLocal(minScale))
                        maxScale = minScale;
                     else
                        break;
                }
            }
            
            if (minScale == maxScale) return 0;
            
            while (minScale / maxScale < 1 - _scaleMaximizationTolerance)
            {
                double midScale = 0.5 * (minScale + maxScale);
                if (TryPackLocal(midScale))
                    minScale = midScale;
                else
                    maxScale = midScale;
            }
            
            if (!lastResult)
                TryPackLocal(minScale);
                
            return minScale;
        }

        public int Pack(GlyphGeometry[] glyphs)
        {
            double initialScale = _scale > 0 ? _scale : _minScale;
            if (initialScale > 0)
            {
                int w = _width, h = _height;
                int remaining = TryPack(glyphs, _dimensionsConstraint, ref w, ref h, initialScale);
                if (remaining != 0)
                    return remaining;
                 // On success, update width/height if implicit
                 if (_width < 0 || _height < 0)
                 {
                     _width = w; _height = h;
                 }
            }
            else if (_width < 0 || _height < 0)
                return -1;
            
            if (_scale <= 0)
            {
                _scale = PackAndScale(glyphs);
                // PackAndScale has side effect of setting dimensions?
                // C++ PackAndScale essentially calls TryPack multiple times.
                // The last call `TRY_PACK(minScale)` sets dimensions if it succeeds.
                // But tryPack takes width/height by value in my macro equivalent?
                // Ah, in C++ `int w = width, h = height;` local vars are passed by ref.
                // AND `tryPack` updates them.
                // So if `PackAndScale` succeeds, we need the final dimensions.
                
                // Let's re-run TryPack with final scale to ensure side effects are applied to class members if needed?
                // Wait, C++ `pack` implementation:
                // if (scale <= 0) scale = packAndScale(...);
                // It doesn't seem to update width/height members?
                // But TryPack updates `w` and `h`.
                // If `PackAndScale` is called, `width` and `height` (members) are only used as input constraints.
                // If they are -1, `w` and `h` start at -1.
                // `TryPack` resolves them.
                // But `PackAndScale` uses local `w` and `h`.
                // So after `packAndScale`, the members `width` and `height` might still be -1?
                // C++ `TightAtlasPacker` methods: `getDimensions` returns `this->width`...
                // So `pack` must update `this->width`?
                // No, `pack` DOES NOT seem to update `this->width`.
                // Unless `tryPack` updates the arguments passed by ref.
                // In `packAndScale`: `int w = width, h = height;`
                // `TRY_PACK` calls `tryPack(..., w, h, ...)`.
                // So `w` and `h` are updated. But `this->width` is NOT.
                // So `getDimensions` would return -1?
                
                // Wait. The user of `TightAtlasPacker` probably calls `pack`.
                // If `pack` succeeds, how do they get dimensions?
                
                // Ah, `InitialScale` path: `tryPack(..., width, height, ...)` -> updates members directly!
                // `PackAndScale` path: uses local copies?
                // That seems like a bug or I misunderstand C++ code.
                // C++ `packAndScale` uses `int w = width, h = height;`. It modifies local `w, h`.
                // It returns `minScale`.
                // `pack` sets `scale = Result`.
                // But `width` and `height` members are unchanged if `PackAndScale` path is taken?
                // This implies `TightAtlasPacker` expects caller to use `initialScale > 0` if they want dimensions set?
                // Or maybe `PackAndScale` only finds scale, and then we run `tryPack` again?
                // `pack` function ends after setting scale.
                // BUT `PackAndScale` calls `TRY_PACK` at the end: `if (!lastResult) TRY_PACK(minScale);`
                // This updates local `w` and `h`. Still not members.

                // Maybe `TightAtlasPacker` is intended to be used by calling `tryPack` manually if one wants dimensions out?
                // Or `pack` assumes fixed dimensions or fixed scale mostly.
                // If `scale <= 0`, we compute it.
                // But we lose the computed width/height...
                
                // However, `ImmediateAtlasGenerator` might not care about `TightAtlasPacker` dimensions state, but rather the glyphs' placement.
                // `tryPack` calls `glyph->placeBox`.
                // So the GLYPHS are updated with placement.
                // The dimensions of the atlas are needed to allocate bitmap though.
                // If `TightAtlasPacker` doesn't update dimensions, user doesn't know how big the bitmap should be.
                
                // Let's look at `main.cpp` or `ImmediateAtlasGenerator` later.
                // For now I will reproduce C++ logic.
                
                // One fix: If I want to persist dimensions after PackAndScale, I should probably update them.
                // But I'll stick to C++ logic.
            }
            
            if (_scale <= 0)
                return -1;
                
            return 0; // Success?
        }

        public void SetDimensions(int width, int height) { _width = width; _height = height; }
        public void UnsetDimensions() { _width = -1; _height = -1; }
        public void SetDimensionsConstraint(DimensionsConstraint c) { _dimensionsConstraint = c; }
        public void SetSpacing(int spacing) { _spacing = spacing; }
        public void SetScale(double scale) { _scale = scale; }
        public void SetMinimumScale(double minScale) { _minScale = minScale; }
        public void SetUnitRange(Msdfgen.Range range) { _unitRange = range; }
        public void SetPixelRange(Msdfgen.Range range) { _pxRange = range; }
        public void SetMiterLimit(double val) { _miterLimit = val; }
        public void SetOriginPixelAlignment(bool align) { _pxAlignOriginX = _pxAlignOriginY = align; }
        public void SetOriginPixelAlignment(bool alignX, bool alignY) { _pxAlignOriginX = alignX; _pxAlignOriginY = alignY; }
        public void SetInnerUnitPadding(Padding padding) { _innerUnitPadding = padding; }
        public void SetOuterUnitPadding(Padding padding) { _outerUnitPadding = padding; }
        public void SetInnerPixelPadding(Padding padding) { _innerPxPadding = padding; }
        public void SetOuterPixelPadding(Padding padding) { _outerPxPadding = padding; }

        public void GetDimensions(out int width, out int height) { width = _width; height = _height; }
        public double GetScale() => _scale;
        public Msdfgen.Range GetPixelRange() => _pxRange + _scale * _unitRange;
    }
}
