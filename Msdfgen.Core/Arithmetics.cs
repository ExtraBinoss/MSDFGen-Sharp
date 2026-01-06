using System;

namespace Msdfgen
{
    public static class Arithmetics
    {
        public static T Min<T>(T a, T b) where T : IComparable<T>
        {
            return b.CompareTo(a) < 0 ? b : a;
        }

        public static T Max<T>(T a, T b) where T : IComparable<T>
        {
            return a.CompareTo(b) < 0 ? b : a;
        }

        public static T Median<T>(T a, T b, T c) where T : IComparable<T>
        {
            return Max(Min(a, b), Min(Max(a, b), c));
        }

        public static double Mix(double a, double b, double weight)
        {
            return (1 - weight) * a + weight * b;
        }

        public static float Mix(float a, float b, float weight)
        {
            return (1 - weight) * a + weight * b;
        }

        public static Vector2 Mix(Vector2 a, Vector2 b, double weight)
        {
            return (1 - weight) * a + weight * b;
        }

        public static int Sign(double n)
        {
            return (0 < n ? 1 : 0) - (n < 0 ? 1 : 0);
        }

        public static int NonZeroSign(double n)
        {
            return 2 * (n > 0 ? 1 : 0) - 1;
        }
        
        public static double Clamp(double n, double min, double max)
        {
             return n >= min && n <= max ? n : (n < min ? min : max); 
        }

        public static double Clamp(double n, double max) {
            return Clamp(n, 0, max);
        }
    }
}
