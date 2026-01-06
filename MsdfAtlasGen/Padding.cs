using System;
using Msdfgen;

namespace MsdfAtlasGen
{
    /// <summary>
    /// Represents padding around a glyph box.
    /// </summary>
    public struct Padding
    {
        public double L, B, R, T;

        /// <summary>
        /// Initializes uniform padding.
        /// </summary>
        public Padding(double uniformPadding = 0)
        {
            L = B = R = T = uniformPadding;
        }

        /// <summary>
        /// Initializes specific padding for each side.
        /// </summary>
        public Padding(double l, double b, double r, double t)
        {
            L = l;
            B = b;
            R = r;
            T = t;
        }

        /// <summary>
        /// Creates a uniform padding instance.
        /// </summary>
        public static Padding Create(Shape.Bounds bounds, double padding)
        {
            // Assuming Shape.Bounds has L, B, R, T
            return new Padding
            {
                L = padding,
                B = padding,
                R = padding,
                T = padding
            };
        }

        /// <summary>
        /// Applies padding to a bounding box.
        /// </summary>
        public static void Pad(ref Shape.Bounds bounds, Padding padding)
        {
            bounds.L -= padding.L;
            bounds.B -= padding.B;
            bounds.R += padding.R;
            bounds.T += padding.T;
        }

        public static Padding operator -(Padding padding)
        {
            return new Padding(-padding.L, -padding.B, -padding.R, -padding.T);
        }

        public static Padding operator +(Padding a, Padding b)
        {
            return new Padding(a.L + b.L, a.B + b.B, a.R + b.R, a.T + b.T);
        }

        public static Padding operator -(Padding a, Padding b)
        {
            return new Padding(a.L - b.L, a.B - b.B, a.R - b.R, a.T - b.T);
        }

        public static Padding operator *(double factor, Padding padding)
        {
            return new Padding(factor * padding.L, factor * padding.B, factor * padding.R, factor * padding.T);
        }

        public static Padding operator *(Padding padding, double factor)
        {
            return new Padding(padding.L * factor, padding.B * factor, padding.R * factor, padding.T * factor);
        }

        public static Padding operator /(Padding padding, double divisor)
        {
            return new Padding(padding.L / divisor, padding.B / divisor, padding.R / divisor, padding.T / divisor);
        }
    }
}
