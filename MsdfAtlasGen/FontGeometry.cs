using System;
using System.Collections.Generic;
using System.Linq;
using Msdfgen;
using Msdfgen.Extensions;

namespace MsdfAtlasGen
{
    public class FontGeometry
    {
        public class GlyphRange
        {
            private readonly List<GlyphGeometry> _glyphs;
            private readonly int _rangeStart;
            private readonly int _rangeEnd;

            /// <summary>
            /// Initializes an empty glyph range.
            /// </summary>
            public GlyphRange()
            {
                _glyphs = new List<GlyphGeometry>();
            }

            /// <summary>
            /// Initializes a glyph range from a list of glyphs and a specified range.
            /// </summary>
            public GlyphRange(List<GlyphGeometry> glyphs, int rangeStart, int rangeEnd)
            {
                _glyphs = glyphs;
                _rangeStart = rangeStart;
                _rangeEnd = rangeEnd;
            }

            public int Size => _rangeEnd - _rangeStart;
            public bool Empty => _rangeStart == _rangeEnd;
            public IEnumerable<GlyphGeometry> Glyphs => _glyphs.Skip(_rangeStart).Take(Size);
        }

        private const double DefaultFontUnitsPerEm = 2048.0;

        private double _geometryScale;
        private FontMetrics _metrics;
        private GlyphIdentifierType _preferredIdentifierType;
        private List<GlyphGeometry> _glyphs;
        private int _rangeStart;
        private int _rangeEnd;
        private readonly Dictionary<int, int> _glyphsByIndex;
        private readonly Dictionary<uint, int> _glyphsByCodepoint;
        private readonly Dictionary<(int, int), double> _kerning;
        private readonly List<GlyphGeometry> _ownGlyphs;
        private string _name = string.Empty;

        /// <summary>
        /// Initializes a new font geometry instance with no glyphs.
        /// </summary>
        public FontGeometry() : this((List<GlyphGeometry>?)null)
        {
        }

        /// <summary>
        /// Initializes a new font geometry instance using a shared glyph storage.
        /// </summary>
        public FontGeometry(List<GlyphGeometry>? glyphStorage)
        {
            _geometryScale = 1;
            _metrics = new FontMetrics();
            _preferredIdentifierType = GlyphIdentifierType.UnicodeCodepoint;
            _ownGlyphs = new List<GlyphGeometry>();
            _glyphs = glyphStorage ?? _ownGlyphs;
            _rangeStart = _glyphs.Count;
            _rangeEnd = _glyphs.Count;
            _glyphsByIndex = new Dictionary<int, int>();
            _glyphsByCodepoint = new Dictionary<uint, int>();
            _kerning = new Dictionary<(int, int), double>();
        }

        /// <summary>
        /// Loads a range of glyphs by their indices from the specified font.
        /// </summary>
        public int LoadGlyphRange(FontHandle font, double fontScale, uint rangeStart, uint rangeEnd, bool preprocessGeometry = true, bool enableKerning = true)
        {
            if (!(_glyphs.Count == _rangeEnd && LoadMetrics(font, fontScale)))
                return -1;

            int loaded = 0;
            for (uint index = rangeStart; index < rangeEnd; ++index)
            {
                var glyph = new GlyphGeometry();
                if (glyph.Load(font, _geometryScale, new GlyphIndex(index), preprocessGeometry))
                {
                    AddGlyph(glyph);
                    ++loaded;
                }
            }

            if (enableKerning)
                LoadKerning(font);

            _preferredIdentifierType = GlyphIdentifierType.GlyphIndex;
            return loaded;
        }

        /// <summary>
        /// Loads a set of glyphs by their indices from the specified font.
        /// </summary>
        public int LoadGlyphset(FontHandle font, double fontScale, Charset glyphset, bool preprocessGeometry = true, bool enableKerning = true)
        {
            if (!(_glyphs.Count == _rangeEnd && LoadMetrics(font, fontScale)))
                return -1;

            int loaded = 0;
            foreach (uint index in glyphset)
            {
                var glyph = new GlyphGeometry();
                if (glyph.Load(font, _geometryScale, new GlyphIndex(index), preprocessGeometry))
                {
                    AddGlyph(glyph);
                    ++loaded;
                }
            }

            if (enableKerning)
                LoadKerning(font);
            _preferredIdentifierType = GlyphIdentifierType.GlyphIndex;
            return loaded;
        }

        /// <summary>
        /// Loads a set of glyphs by their Unicode codepoints from the specified font.
        /// </summary>
        public int LoadCharset(FontHandle font, double fontScale, Charset charset, bool preprocessGeometry = true, bool enableKerning = true)
        {
            if (!(_glyphs.Count == _rangeEnd && LoadMetrics(font, fontScale)))
                return -1;

            int loaded = 0;
            foreach (uint cp in charset)
            {
                var glyph = new GlyphGeometry();
                if (glyph.Load(font, _geometryScale, cp, preprocessGeometry))
                {
                    AddGlyph(glyph);
                    ++loaded;
                }
            }
            if (enableKerning)
                LoadKerning(font);
            _preferredIdentifierType = GlyphIdentifierType.UnicodeCodepoint;
            return loaded;
        }

        /// <summary>
        /// Loads font-wide metrics (ascender, descender, line height) from the specified font.
        /// </summary>
        public bool LoadMetrics(FontHandle font, double fontScale)
        {
            if (font == null) return false;

            if (!FontLoader.GetFontMetrics(out Msdfgen.Extensions.FontMetrics ftMetrics, font, FontCoordinateScaling.None))
                return false;

            _metrics = new FontMetrics
            {
                EmSize = ftMetrics.EmSize,
                AscenderY = ftMetrics.AscenderY,
                DescenderY = ftMetrics.DescenderY,
                LineHeight = ftMetrics.LineHeight,
                UnderlineY = ftMetrics.UnderlineY,
                UnderlineThickness = ftMetrics.UnderlineThickness
            };

            if (_metrics.EmSize <= 0)
                _metrics.EmSize = DefaultFontUnitsPerEm;

            _geometryScale = fontScale / _metrics.EmSize;

            _metrics.EmSize *= _geometryScale;
            _metrics.AscenderY *= _geometryScale;
            _metrics.DescenderY *= _geometryScale;
            _metrics.LineHeight *= _geometryScale;
            _metrics.UnderlineY *= _geometryScale;
            _metrics.UnderlineThickness *= _geometryScale;

            return true;
        }
        
        /// <summary>
        /// Manually adds a single glyph to the geometry.
        /// </summary>
        public bool AddGlyph(GlyphGeometry glyph)
        {
            if (_glyphs.Count != _rangeEnd)
                return false;
            
            if (!_glyphsByIndex.ContainsKey(glyph.GetIndex()))
                _glyphsByIndex[glyph.GetIndex()] = _rangeEnd;
            
            if (glyph.GetCodepoint() != 0)
            {
                uint cp = (uint)glyph.GetCodepoint();
                if (!_glyphsByCodepoint.ContainsKey(cp))
                    _glyphsByCodepoint[cp] = _rangeEnd;
            }
            
            _glyphs.Add(glyph);
            ++_rangeEnd;
            return true;
        }

        /// <summary>
        /// Loads kerning information for all glyphs currently in the geometry.
        /// </summary>
        public int LoadKerning(FontHandle font)
        {
            _kerning.Clear();
            int loaded = 0;
            for (int i = _rangeStart; i < _rangeEnd; ++i)
            {
                for (int j = _rangeStart; j < _rangeEnd; ++j)
                {
                    if (FontLoader.GetKerning(out double kern, font, new GlyphIndex((uint)_glyphs[i].GetIndex()), new GlyphIndex((uint)_glyphs[j].GetIndex()), FontCoordinateScaling.None) && kern != 0)
                    {
                        _kerning[( _glyphs[i].GetIndex(), _glyphs[j].GetIndex() )] = _geometryScale * kern;
                        ++loaded;
                    }
                }
            }
            return loaded;
        }

        /// <summary>
        /// Retrieves the kerning dictionary.
        /// </summary>
        public Dictionary<(int, int), double> GetKernings() => _kerning;

        /// <summary>
        /// Sets the name of the font.
        /// </summary>
        public void SetName(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Returns the scale from font units to geometry pixels.
        /// </summary>
        public double GetGeometryScale() => _geometryScale;

        /// <summary>
        /// Returns the font-wide metrics.
        /// </summary>
        public FontMetrics GetMetrics() => _metrics;

        /// <summary>
        /// Returns the preferred identifier type (index or codepoint) based on how glyphs were loaded.
        /// </summary>
        public GlyphIdentifierType GetPreferredIdentifierType() => _preferredIdentifierType;

        /// <summary>
        /// Returns the range of glyphs managed by this instance.
        /// </summary>
        public GlyphRange GetGlyphs() => new GlyphRange(_glyphs, _rangeStart, _rangeEnd);

        /// <summary>
        /// Retrieves a glyph by its index.
        /// </summary>
        public GlyphGeometry? GetGlyph(int index)
        {
            if (_glyphsByIndex.TryGetValue(index, out int pos))
                return _glyphs[pos];
            return null;
        }

        /// <summary>
        /// Retrieves a glyph by its Unicode codepoint.
        /// </summary>
        public GlyphGeometry? GetGlyph(uint codepoint)
        {
            if (_glyphsByCodepoint.TryGetValue(codepoint, out int pos))
                return _glyphs[pos];
             return null;
        }
        
        /// <summary>
        /// Returns the font name.
        /// </summary>
        public string GetName() => _name;
        
        /// <summary>
        /// Returns the original Units Per Em before geometry scaling.
        /// </summary>
        public double GetUnitsPerEm() => (_metrics.EmSize > 0) ? _metrics.EmSize / _geometryScale : DefaultFontUnitsPerEm;
    }
}
