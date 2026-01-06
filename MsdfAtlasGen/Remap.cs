using System;

namespace MsdfAtlasGen
{
    /// <summary>
    /// Represents the repositioning of a subsection of the atlas
    /// </summary>
    public struct Remap
    {
        public int Index;
        public Coordinate Source;
        public Coordinate Target;
        public int Width;
        public int Height;

        public struct Coordinate
        {
            public int X, Y;
        }
    }
}
