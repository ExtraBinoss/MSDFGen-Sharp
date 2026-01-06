using System;

namespace Msdfgen
{
    /// <summary>
    /// Represents a signed distance and alignment, which together can be compared to uniquely determine the closest edge segment.
    /// </summary>
    public struct SignedDistance : IComparable<SignedDistance>
    {
        public double Distance;
        public double Dot;

        public SignedDistance(double dist, double d)
        {
            Distance = dist;
            Dot = d;
        }

        public static SignedDistance Infinite => new SignedDistance(-1e240, 0); // using a very small number like -DBL_MAX in C++

        public int CompareTo(SignedDistance other)
        {
            if (Math.Abs(Distance) < Math.Abs(other.Distance)) return -1;
            if (Math.Abs(Distance) > Math.Abs(other.Distance)) return 1;
            return Dot.CompareTo(other.Dot);
        }

        public static bool operator <(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot < b.Dot);
        }

        public static bool operator >(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot > b.Dot);
        }

        public static bool operator <=(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot <= b.Dot);
        }

        public static bool operator >=(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot >= b.Dot);
        }
    }
}
