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

        /// <summary>
        /// Initializes a vector with the same value for both components.
        /// </summary>
        public Vector2(double val = 0)
        {
            X = val;
            Y = val;
        }

        /// <summary>
        /// Initializes a vector with the specified X and Y components.
        /// </summary>
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
            return new Vector2(0, allowZero ? 1 : 0);
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

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        public static double DotProduct(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// <summary>
        /// Returns the cross product of two vectors.
        /// </summary>
        public static double CrossProduct(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        /// <summary>
        /// Unary plus operator.
        /// </summary>
        public static Vector2 operator +(Vector2 v)
        {
            return v;
        }

        /// <summary>
        /// Unary minus operator.
        /// </summary>
        public static Vector2 operator -(Vector2 v)
        {
            return new Vector2(-v.X, -v.Y);
        }

        /// <summary>
        /// Implicit boolean conversion (true if non-zero).
        /// </summary>
        public static bool operator true(Vector2 v)
        {
            return v.X != 0 || v.Y != 0;
        }

        /// <summary>
        /// Logical negation operator.
        /// </summary>
        public static bool operator !(Vector2 v)
        {
             return v.X == 0 && v.Y == 0;
        }

        /// <summary>
        /// Implicit boolean conversion (false if zero).
        /// </summary>
        public static bool operator false(Vector2 v)
        {
             return v.X == 0 && v.Y == 0;
        }

        /// <summary>
        /// Vector addition.
        /// </summary>
        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        /// Vector subtraction.
        /// </summary>
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// Component-wise multiplication.
        /// </summary>
        public static Vector2 operator *(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X * b.X, a.Y * b.Y);
        }

        /// <summary>
        /// Component-wise division.
        /// </summary>
        public static Vector2 operator /(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X / b.X, a.Y / b.Y);
        }

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        public static Vector2 operator *(double a, Vector2 b)
        {
            return new Vector2(a * b.X, a * b.Y);
        }

        /// <summary>
        /// Scalar division (scalar / vector).
        /// </summary>
        public static Vector2 operator /(double a, Vector2 b)
        {
            return new Vector2(a / b.X, a / b.Y);
        }

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        public static Vector2 operator *(Vector2 a, double b)
        {
            return new Vector2(a.X * b, a.Y * b);
        }

        /// <summary>
        /// Scalar division (vector / scalar).
        /// </summary>
        public static Vector2 operator /(Vector2 a, double b)
        {
            return new Vector2(a.X / b, a.Y / b);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Vector2 other)
            {
                return Equals(other);
            }
            return false;
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
