using System;
using System.Collections.Generic;
using System.Linq;
using Msdfgen;
using SixLabors.Fonts;

namespace MsdfAtlasGen
{
    public class FontGeometry
    {
        public class GlyphRange
        {
            private readonly List<GlyphGeometry> _glyphs;
            private readonly int _rangeStart;
            private readonly int _rangeEnd;

            public GlyphRange()
            {
                _glyphs = new List<GlyphGeometry>();
            }

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
        private string _name;

        public FontGeometry() : this(null)
        {
        }

        public FontGeometry(List<GlyphGeometry> glyphStorage)
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

        public int LoadGlyphRange(Font font, double fontScale, uint rangeStart, uint rangeEnd, bool preprocessGeometry = true, bool enableKerning = true)
        {
            // Note: Loading by index is not fully supported by the underlying FontLoader extensions yet (assumes char).
            // We will attempt to cast index to char, which works for unicode codepoints, but actual glyph index loading requires updating FontLoader.
            // For now, assuming rangeStart/End are Codepoints if used here, or this might fail for high indices.
            
            if (!(_glyphs.Count == _rangeEnd && LoadMetrics(font, fontScale)))
                return -1;
            
            // _glyphs.Capacity = _glyphs.Count + (int)(rangeEnd - rangeStart);
            int loaded = 0;
            for (uint index = rangeStart; index < rangeEnd; ++index)
            {
                var glyph = new GlyphGeometry();
                // Trying to load as char (codepoint)
                if (glyph.Load(font, _geometryScale, (char)index, preprocessGeometry))
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

        public int LoadGlyphset(Font font, double fontScale, Charset glyphset, bool preprocessGeometry = true, bool enableKerning = true)
        {
            if (!(_glyphs.Count == _rangeEnd && LoadMetrics(font, fontScale)))
                return -1;

            int loaded = 0;
            foreach (uint index in glyphset)
            {
                var glyph = new GlyphGeometry();
                 // Assuming index is Codepoint in our Charset implementation usually, but here it says 'glyphset'.
                 // If glyphset means glyph indices, we have the same issue.
                if (glyph.Load(font, _geometryScale, (char)index, preprocessGeometry))
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

        public int LoadCharset(Font font, double fontScale, Charset charset, bool preprocessGeometry = true, bool enableKerning = true)
        {
             if (!(_glyphs.Count == _rangeEnd && LoadMetrics(font, fontScale)))
                return -1;

            int loaded = 0;
            foreach (uint cp in charset)
            {
                var glyph = new GlyphGeometry();
                if (glyph.Load(font, _geometryScale, (char)cp, preprocessGeometry))
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

        public bool LoadMetrics(Font font, double fontScale)
        {
            if (font == null) return false;

            // Use the actual font metrics from SixLabors instead of heuristics so offsets/line height
            // align with the source font (fixes squished / misaligned text in exported FNT).
            var metrics = font.FontMetrics;

            double unitsPerEm = metrics.UnitsPerEm <= 0 ? DefaultFontUnitsPerEm : metrics.UnitsPerEm;
            _geometryScale = fontScale / unitsPerEm; // scale to requested pixel size

            _metrics.EmSize = unitsPerEm * _geometryScale;

            // Pull vertical metrics from the horizontal set (most atlas users render horizontally).
            var hm = metrics.HorizontalMetrics;
            _metrics.AscenderY = hm.Ascender * _geometryScale;
            _metrics.DescenderY = hm.Descender * _geometryScale; // typically negative
            _metrics.LineHeight = hm.LineHeight * _geometryScale;

            // Underline info is rarely needed for atlas export; keep zeroed if unavailable.
            _metrics.UnderlineY = metrics.UnderlinePosition * _geometryScale;
            _metrics.UnderlineThickness = metrics.UnderlineThickness * _geometryScale;

            return true;
        }

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

        public int LoadKerning(Font font)
        {
            // Kerning loading is complex with SixLabors.Fonts.
            // Simplified: skipping for now or stubbing.
            // C++ uses msdfgen::getKerning which likely calls FT_Get_Kerning.
            return 0; 
        }

        public void SetName(string name)
        {
            _name = name;
        }

        public double GetGeometryScale() => _geometryScale;
        public FontMetrics GetMetrics() => _metrics;
        public GlyphIdentifierType GetPreferredIdentifierType() => _preferredIdentifierType;
        public GlyphRange GetGlyphs() => new GlyphRange(_glyphs, _rangeStart, _rangeEnd);

        public GlyphGeometry GetGlyph(int index)
        {
            if (_glyphsByIndex.TryGetValue(index, out int pos))
                return _glyphs[pos];
            return null;
        }

        public GlyphGeometry GetGlyph(uint codepoint)
        {
            if (_glyphsByCodepoint.TryGetValue(codepoint, out int pos))
                return _glyphs[pos];
             return null;
        }
        
        public string GetName() => _name;
        
        // Get original UnitsPerEm before scaling (for FNT export calculations)
        public double GetUnitsPerEm() => (_metrics.EmSize > 0) ? _metrics.EmSize / _geometryScale : DefaultFontUnitsPerEm;
    }
}
