using System;

namespace Msdfgen
{
    /// <summary>
    /// An abstract edge segment.
    /// </summary>
    public abstract class EdgeSegment
    {
        public EdgeColor Color;

        /// <summary>
        /// Initializes the edge segment with a color.
        /// </summary>
        protected EdgeSegment(EdgeColor edgeColor = EdgeColor.WHITE)
        {
            Color = edgeColor;
        }

        /// <summary>
        /// Creates a new linear edge segment.
        /// </summary>
        public static EdgeSegment Create(Vector2 p0, Vector2 p1, EdgeColor edgeColor = EdgeColor.WHITE)
        {
            return new LinearSegment(p0, p1, edgeColor);
        }

        /// <summary>
        /// Creates a new quadratic Bezier edge segment.
        /// </summary>
        public static EdgeSegment Create(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor edgeColor = EdgeColor.WHITE)
        {
            return new QuadraticSegment(p0, p1, p2, edgeColor);
        }

        /// <summary>
        /// Creates a new cubic Bezier edge segment.
        /// </summary>
        public static EdgeSegment Create(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, EdgeColor edgeColor = EdgeColor.WHITE)
        {
            return new CubicSegment(p0, p1, p2, p3, edgeColor);
        }

        /// <summary>
        /// Creates a copy of the edge segment.
        /// </summary>
        public abstract EdgeSegment Clone();

        /// <summary>
        /// Returns the numeric code of the edge segment's type.
        /// </summary>
        public abstract int Type { get; }

        /// <summary>
        /// Returns the array of control points.
        /// </summary>
        public abstract Vector2[] ControlPoints { get; }

        /// <summary>
        /// Returns the point on the edge specified by the parameter (between 0 and 1).
        /// </summary>
        public abstract Vector2 Point(double param);

        /// <summary>
        /// Returns the direction the edge has at the point specified by the parameter.
        /// </summary>
        public abstract Vector2 Direction(double param);

        /// <summary>
        /// Returns the change of direction (second derivative) at the point specified by the parameter.
        /// </summary>
        public abstract Vector2 DirectionChange(double param);

        /// <summary>
        /// Returns the minimum signed distance between origin and the edge.
        /// </summary>
        public abstract SignedDistance SignedDistance(Vector2 origin, out double param);

        /// <summary>
        /// Converts a previously retrieved signed distance from origin to perpendicular distance.
        /// </summary>
        public virtual void DistanceToPerpendicularDistance(ref SignedDistance distance, Vector2 origin, double param)
        {
            if (param < 0)
            {
                Vector2 dir = Direction(0).Normalize();
                Vector2 aq = origin - Point(0);
                double ts = Vector2.DotProduct(aq, dir);
                if (ts < 0)
                {
                    double perpendicularDistance = Vector2.CrossProduct(aq, dir);
                    if (Math.Abs(perpendicularDistance) <= Math.Abs(distance.Distance))
                    {
                        distance.Distance = perpendicularDistance;
                        distance.Dot = 0;
                    }
                }
            }
            else if (param > 1)
            {
                Vector2 dir = Direction(1).Normalize();
                Vector2 bq = origin - Point(1);
                double ts = Vector2.DotProduct(bq, dir);
                if (ts > 0)
                {
                    double perpendicularDistance = Vector2.CrossProduct(bq, dir);
                    if (Math.Abs(perpendicularDistance) <= Math.Abs(distance.Distance))
                    {
                        distance.Distance = perpendicularDistance;
                        distance.Dot = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Outputs a list of (at most three) intersections (their X coordinates) with an infinite horizontal scanline at y and returns how many there are.
        /// </summary>
        public abstract int ScanlineIntersections(double[] x, int[] dy, double y);

        /// <summary>
        /// Adjusts the bounding box to fit the edge segment.
        /// </summary>
        public abstract void Bound(ref double xMin, ref double yMin, ref double xMax, ref double yMax);

        /// <summary>
        /// Reverses the edge (swaps its start point and end point).
        /// </summary>
        public abstract void Reverse();

        /// <summary>
        /// Moves the start point of the edge segment.
        /// </summary>
        public abstract void MoveStartPoint(Vector2 to);

        /// <summary>
        /// Moves the end point of the edge segment.
        /// </summary>
        public abstract void MoveEndPoint(Vector2 to);

        /// <summary>
        /// Splits the edge segments into thirds which together represent the original edge.
        /// </summary>
        public abstract void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2);
    }
}
