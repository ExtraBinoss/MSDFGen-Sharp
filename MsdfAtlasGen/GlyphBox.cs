using System;

namespace MsdfAtlasGen
{
    /// <summary>
    /// The glyph box - its bounds in plane and atlas
    /// </summary>
    public struct GlyphBox
    {
        public int Index;
        public double Advance;
        public GlyphBounds Bounds;
        public Rectangle Rect;

        public struct GlyphBounds
        {
            public double L, B, R, T;
        }
    }
}
