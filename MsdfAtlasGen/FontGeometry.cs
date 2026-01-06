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
        private string _name = string.Empty;

        public FontGeometry() : this((List<GlyphGeometry>?)null)
        {
        }

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

        public void SetName(string name)
        {
            _name = name;
        }

        public double GetGeometryScale() => _geometryScale;
        public FontMetrics GetMetrics() => _metrics;
        public GlyphIdentifierType GetPreferredIdentifierType() => _preferredIdentifierType;
        public GlyphRange GetGlyphs() => new GlyphRange(_glyphs, _rangeStart, _rangeEnd);

        public GlyphGeometry? GetGlyph(int index)
        {
            if (_glyphsByIndex.TryGetValue(index, out int pos))
                return _glyphs[pos];
            return null;
        }

        public GlyphGeometry? GetGlyph(uint codepoint)
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
