using System;

namespace Msdfgen
{
    public static class Arithmetics
    {
        /// <summary>
        /// Returns the minimum of two values.
        /// </summary>
        public static T Min<T>(T a, T b) where T : IComparable<T>
        {
            return b.CompareTo(a) < 0 ? b : a;
        }

        /// <summary>
        /// Returns the maximum of two values.
        /// </summary>
        public static T Max<T>(T a, T b) where T : IComparable<T>
        {
            return a.CompareTo(b) < 0 ? b : a;
        }

        /// <summary>
        /// Returns the median of three values.
        /// </summary>
        public static T Median<T>(T a, T b, T c) where T : IComparable<T>
        {
            return Max(Min(a, b), Min(Max(a, b), c));
        }

        /// <summary>
        /// Linearly interpolates between two doubles.
        /// </summary>
        public static double Mix(double a, double b, double weight)
        {
            return (1 - weight) * a + weight * b;
        }

        /// <summary>
        /// Linearly interpolates between two floats.
        /// </summary>
        public static float Mix(float a, float b, float weight)
        {
            return (1 - weight) * a + weight * b;
        }

        /// <summary>
        /// Linearly interpolates between two vectors.
        /// </summary>
        public static Vector2 Mix(Vector2 a, Vector2 b, double weight)
        {
            return (1 - weight) * a + weight * b;
        }

        /// <summary>
        /// Returns the sign of a number (-1, 0, or 1).
        /// </summary>
        public static int Sign(double n)
        {
            return (0 < n ? 1 : 0) - (n < 0 ? 1 : 0);
        }

        /// <summary>
        /// Returns the sign of a number, ensuring it's either -1 or 1 (never 0).
        /// </summary>
        public static int NonZeroSign(double n)
        {
            return 2 * (n > 0 ? 1 : 0) - 1;
        }
        
        /// <summary>
        /// Clamps a value between a minimum and a maximum.
        /// </summary>
        public static double Clamp(double n, double min, double max)
        {
             return n >= min && n <= max ? n : (n < min ? min : max); 
        }

        /// <summary>
        /// Clamps a value between 0 and a maximum.
        /// </summary>
        public static double Clamp(double n, double max) {
            return Clamp(n, 0, max);
        }
    }
}
