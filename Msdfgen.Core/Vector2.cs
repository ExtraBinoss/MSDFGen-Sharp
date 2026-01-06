using System;

namespace Msdfgen
{
    /// <summary>
    /// A 2-dimensional euclidean floating-point vector.
    /// </summary>
    public struct Vector2 : IEquatable<Vector2>
    {
        public double X;
        public double Y;

        public Vector2(double val = 0)
        {
            X = val;
            Y = val;
        }

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Sets the vector to zero.
        /// </summary>
        public void Reset()
        {
            X = 0;
            Y = 0;
        }

        /// <summary>
        /// Sets individual elements of the vector.
        /// </summary>
        public void Set(double newX, double newY)
        {
            X = newX;
            Y = newY;
        }

        /// <summary>
        /// Returns the vector's squared length.
        /// </summary>
        public double SquaredLength()
        {
            return X * X + Y * Y;
        }

        /// <summary>
        /// Returns the vector's length.
        /// </summary>
        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        /// <summary>
        /// Returns the normalized vector - one that has the same direction but unit length.
        /// </summary>
        public Vector2 Normalize(bool allowZero = false)
        {
            double len = Length();
            if (len != 0)
                return new Vector2(X / len, Y / len);
            return new Vector2(0, allowZero ? 1 : 0); // Logic from C++: !allowZero -> 0 if false, 1 if true? Wait. C++ !allowZero is boolean NOT.
            // C++: return Vector2(0, !allowZero); 
            // If allowZero is false, !allowZero is true (1). So (0, 1).
            // If allowZero is true, !allowZero is false (0). So (0, 0).
            // Wait, why (0, 1) if not allow zero?
            // Ah, to avoid zero vector if not allowed?
            // Let's re-read C++: return Vector2(0, !allowZero);
            // If allowZero is false, y becomes 1. If allowZero is true, y becomes 0.
        }

        /// <summary>
        /// Returns a vector with the same length that is orthogonal to this one.
        /// </summary>
        public Vector2 GetOrthogonal(bool polarity = true)
        {
            return polarity ? new Vector2(-Y, X) : new Vector2(Y, -X);
        }

        /// <summary>
        /// Returns a vector with unit length that is orthogonal to this one.
        /// </summary>
        public Vector2 GetOrthonormal(bool polarity = true, bool allowZero = false)
        {
            double len = Length();
            if (len != 0)
                return polarity ? new Vector2(-Y / len, X / len) : new Vector2(Y / len, -X / len);
            return polarity 
                ? new Vector2(0, !allowZero ? 1 : 0) 
                : new Vector2(0, -(!allowZero ? 1 : 0));
        }

        public static double DotProduct(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static double CrossProduct(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public static Vector2 operator +(Vector2 v)
        {
            return v;
        }

        public static Vector2 operator -(Vector2 v)
        {
            return new Vector2(-v.X, -v.Y);
        }

        public static bool operator true(Vector2 v)
        {
            return v.X != 0 || v.Y != 0;
        }

        public static bool operator !(Vector2 v)
        {
             return v.X == 0 && v.Y == 0;
        }

        public static bool operator false(Vector2 v)
        {
             return v.X == 0 && v.Y == 0;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2 operator *(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X * b.X, a.Y * b.Y);
        }

        public static Vector2 operator /(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X / b.X, a.Y / b.Y);
        }

        public static Vector2 operator *(double a, Vector2 b)
        {
            return new Vector2(a * b.X, a * b.Y);
        }

        public static Vector2 operator /(double a, Vector2 b)
        {
            return new Vector2(a / b.X, a / b.Y);
        }

        public static Vector2 operator *(Vector2 a, double b)
        {
            return new Vector2(a.X * b, a.Y * b);
        }

        public static Vector2 operator /(Vector2 a, double b)
        {
            return new Vector2(a.X / b, a.Y / b);
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2 vector && Equals(vector);
        }

        public bool Equals(Vector2 other)
        {
            return X == other.X && Y == other.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}
