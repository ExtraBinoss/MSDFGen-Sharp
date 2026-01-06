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

        /// <summary>
        /// Simple 2D integer coordinate.
        /// </summary>
        public struct Coordinate
        {
            public int X, Y;
        }
    }
}
