using System;

namespace MsdfAtlasGen
{
    public struct Rectangle
    {
        public int X, Y, W, H;
    }

    public struct OrientedRectangle
    {
        public int X, Y, W, H;
        public bool Rotated;
    }
}
