using System;
using Msdfgen;

namespace MsdfAtlasGen
{
    public struct GeneratorAttributes
    {
        public MSDFGeneratorConfig Config;
        public bool ScanlinePass;
    }

    public delegate void GeneratorFunction<T>(Bitmap<T> output, GlyphGeometry glyph, GeneratorAttributes attributes);
}
