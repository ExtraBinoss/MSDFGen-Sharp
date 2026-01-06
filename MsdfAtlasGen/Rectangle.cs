using System;

namespace MsdfAtlasGen
{
    /// <summary>
    /// Represents a simple integer-based rectangle.
    /// </summary>
    public struct Rectangle
    {
        public int X, Y, W, H;
    }

    /// <summary>
    /// Represents a rectangle with orientation information.
    /// </summary>
    public struct OrientedRectangle
    {
        public int X, Y, W, H;
        public bool Rotated;
    }
}
