using System;

namespace Msdfgen
{
    /// <summary>
    /// Represents a signed distance and alignment, which together can be compared to uniquely determine the closest edge segment.
    /// </summary>
    /// <summary>
    /// Represents a signed distance and alignment (dot product), which together can be compared to uniquely determine the closest edge segment.
    /// </summary>
    public struct SignedDistance : IComparable<SignedDistance>
    {
        /// <summary>
        /// The signed distance value.
        /// </summary>
        public double Distance;

        /// <summary>
        /// The alignment (dot product) used for resolving ties and determining the final distance field value.
        /// </summary>
        public double Dot;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignedDistance"/> struct.
        /// </summary>
        /// <param name="dist">The signed distance.</param>
        /// <param name="d">The alignment/dot product.</param>
        public SignedDistance(double dist, double d)
        {
            Distance = dist;
            Dot = d;
        }

        /// <summary>
        /// Gets a value representing an infinite signed distance.
        /// </summary>
        public static SignedDistance Infinite => new SignedDistance(-1e240, 0);

        /// <summary>
        /// Compares this instance with another <see cref="SignedDistance"/> instance.
        /// </summary>
        public int CompareTo(SignedDistance other)
        {
            if (Math.Abs(Distance) < Math.Abs(other.Distance)) return -1;
            if (Math.Abs(Distance) > Math.Abs(other.Distance)) return 1;
            return Dot.CompareTo(other.Dot);
        }

        /// <summary>
        /// Less-than operator for two <see cref="SignedDistance"/> instances.
        /// </summary>
        public static bool operator <(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot < b.Dot);
        }

        /// <summary>
        /// Greater-than operator for two <see cref="SignedDistance"/> instances.
        /// </summary>
        public static bool operator >(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot > b.Dot);
        }

        /// <summary>
        /// Less-than-or-equal-to operator for two <see cref="SignedDistance"/> instances.
        /// </summary>
        public static bool operator <=(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot <= b.Dot);
        }

        /// <summary>
        /// Greater-than-or-equal-to operator for two <see cref="SignedDistance"/> instances.
        /// </summary>
        public static bool operator >=(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot >= b.Dot);
        }
    }
}
