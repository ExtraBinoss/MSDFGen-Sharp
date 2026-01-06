using System;
using Msdfgen;

namespace MsdfAtlasGen
{
    /// <summary>
    /// Configuration and attributes for the atlas generator.
    /// </summary>
    public struct GeneratorAttributes
    {
        public MSDFGeneratorConfig Config;
        public bool ScanlinePass;
    }

    /// <summary>
    /// Delegate for the core glyph image generation logic.
    /// </summary>
    public delegate void GeneratorFunction<T>(Bitmap<T> output, GlyphGeometry glyph, GeneratorAttributes attributes);
}
