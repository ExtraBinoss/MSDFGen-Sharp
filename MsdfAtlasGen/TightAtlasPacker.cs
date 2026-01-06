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

        /// <summary>
        /// Initializes a new tight atlas packer with default settings.
        /// </summary>
        public TightAtlasPacker()
        {
        }

        /// <summary>
        /// Attempts to pack glyphs into a rectangle of specified size or resolves dimensions based on constraints.
        /// </summary>
        public int TryPack(GlyphGeometry[] glyphs, DimensionsConstraint dimensionsConstraint, ref int width, ref int height, double scale)
        {
            // Wrap glyphs into boxes
            var rectangles = new List<Rectangle>(glyphs.Length);
            var rectangleGlyphs = new List<GlyphGeometry>(glyphs.Length);
            
            var attribs = new GlyphGeometry.GlyphAttributes();
            // Pixels-per-font-unit: packer scale * glyph geometry scale.
            attribs.Scale = scale; // will be overridden per glyph below
            attribs.Range = _unitRange; // base; per-glyph addition below
            attribs.InnerPadding = _innerUnitPadding;
            attribs.OuterPadding = _outerUnitPadding;
            attribs.MiterLimit = _miterLimit;
            attribs.PxAlignOriginX = _pxAlignOriginX;
            attribs.PxAlignOriginY = _pxAlignOriginY;

            foreach (var glyph in glyphs)
            {
                if (!glyph.IsWhitespace())
                {
                    double pixelsPerUnit = scale * glyph.GetGeometryScale();
                    attribs.Scale = pixelsPerUnit;
                    attribs.Range = _unitRange + _pxRange / pixelsPerUnit;
                    attribs.InnerPadding = _innerUnitPadding + _innerPxPadding / pixelsPerUnit;
                    attribs.OuterPadding = _outerUnitPadding + _outerPxPadding / pixelsPerUnit;

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

        /// <summary>
        /// Internal helper to iteratively find the maximum scale that still fits within the dimensions.
        /// </summary>
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

        /// <summary>
        /// Performs the complete packing process, resolving scale and dimensions as needed.
        /// </summary>
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
            }
            
            if (_scale <= 0)
                return -1;
                
            return 0;
        }

        /// <summary>
        /// Explicitly sets the atlas dimensions.
        /// </summary>
        public void SetDimensions(int width, int height) { _width = width; _height = height; }

        /// <summary>
        /// Resets the atlas dimensions to unconstrained.
        /// </summary>
        public void UnsetDimensions() { _width = -1; _height = -1; }

        /// <summary>
        /// Sets the dimensions constraint (e.g., Power of Two).
        /// </summary>
        public void SetDimensionsConstraint(DimensionsConstraint c) { _dimensionsConstraint = c; }

        /// <summary>
        /// Sets the spacing between glyphs in the atlas.
        /// </summary>
        public void SetSpacing(int spacing) { _spacing = spacing; }

        /// <summary>
        /// Sets the fixed scale for glyphs.
        /// </summary>
        public void SetScale(double scale) { _scale = scale; }

        /// <summary>
        /// Sets the minimum scale to use during auto-scaling.
        /// </summary>
        public void SetMinimumScale(double minScale) { _minScale = minScale; }

        /// <summary>
        /// Sets the distance field range in font units.
        /// </summary>
        public void SetUnitRange(Msdfgen.Range range) { _unitRange = range; }

        /// <summary>
        /// Sets the distance field range in pixels.
        /// </summary>
        public void SetPixelRange(Msdfgen.Range range) { _pxRange = range; }

        /// <summary>
        /// Sets the miter limit for glyph boundaries.
        /// </summary>
        public void SetMiterLimit(double val) { _miterLimit = val; }

        /// <summary>
        /// Enables or disables pixel alignment for glyph origins.
        /// </summary>
        public void SetOriginPixelAlignment(bool align) { _pxAlignOriginX = _pxAlignOriginY = align; }

        /// <summary>
        /// Enables or disables pixel alignment for glyph origins separately for X and Y axes.
        /// </summary>
        public void SetOriginPixelAlignment(bool alignX, bool alignY) { _pxAlignOriginX = alignX; _pxAlignOriginY = alignY; }

        /// <summary>
        /// Sets the internal padding in font units.
        /// </summary>
        public void SetInnerUnitPadding(Padding padding) { _innerUnitPadding = padding; }

        /// <summary>
        /// Sets the external padding in font units.
        /// </summary>
        public void SetOuterUnitPadding(Padding padding) { _outerUnitPadding = padding; }

        /// <summary>
        /// Sets the internal padding in pixels.
        /// </summary>
        public void SetInnerPixelPadding(Padding padding) { _innerPxPadding = padding; }

        /// <summary>
        /// Sets the external padding in pixels.
        /// </summary>
        public void SetOuterPixelPadding(Padding padding) { _outerPxPadding = padding; }

        /// <summary>
        /// Retrieves the resolved atlas dimensions.
        /// </summary>
        public void GetDimensions(out int width, out int height) { width = _width; height = _height; }

        /// <summary>
        /// Retrieves the resolved scale.
        /// </summary>
        public double GetScale() => _scale;

        /// <summary>
        /// Retrieves the resolved pixel range (combined unit and pixel ranges).
        /// </summary>
        public Msdfgen.Range GetPixelRange() => _pxRange + _scale * _unitRange;
    }
}
