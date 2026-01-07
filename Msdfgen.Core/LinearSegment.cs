using System;

using static Msdfgen.Arithmetics;

namespace Msdfgen
{
    public class LinearSegment : EdgeSegment
    {
        private readonly Vector2[] p = new Vector2[2];

        public override int Type => 1;

        public override Vector2[] ControlPoints => p;

        /// <summary>
        /// Initializes a new linear edge segment with two control points and a color.
        /// </summary>
        public LinearSegment(Vector2 p0, Vector2 p1, EdgeColor edgeColor = EdgeColor.WHITE) : base(edgeColor)
        {
            p[0] = p0;
            p[1] = p1;
        }

        /// <summary>
        /// Creates a copy of the edge segment.
        /// </summary>
        public override EdgeSegment Clone()
        {
            return new LinearSegment(p[0], p[1], Color);
        }

        /// <summary>
        /// Returns the point on the edge specified by the parameter (between 0 and 1).
        /// </summary>
        public override Vector2 Point(double param)
        {
            return Arithmetics.Mix(p[0], p[1], param);
        }

        /// <summary>
        /// Returns the direction the edge has at the point specified by the parameter.
        /// </summary>
        public override Vector2 Direction(double param)
        {
            return p[1] - p[0];
        }

        /// <summary>
        /// Returns the change of direction (second derivative) at the point specified by the parameter.
        /// </summary>
        public override Vector2 DirectionChange(double param)
        {
            return new Vector2(0, 0);
        }

        /// <summary>
        /// Returns the length of the linear segment.
        /// </summary>
        public double Length()
        {
            return (p[1] - p[0]).Length();
        }

        /// <summary>
        /// Returns the minimum signed distance between origin and the edge.
        /// </summary>
        public override SignedDistance SignedDistance(Vector2 origin, out double param)
        {
            Vector2 aq = origin - p[0];
            Vector2 ab = p[1] - p[0];
            param = Vector2.DotProduct(aq, ab) / Vector2.DotProduct(ab, ab);
            Vector2 eq = p[param > 0.5 ? 1 : 0] - origin;
            double endpointDistance = eq.Length();
            if (param > 0 && param < 1)
            {
                double orthoDistance = Vector2.DotProduct(ab.GetOrthonormal(false), aq);
                if (Math.Abs(orthoDistance) < endpointDistance)
                {
                    return new SignedDistance(orthoDistance, 0);
                }
            }
            return new SignedDistance(Arithmetics.NonZeroSign(Vector2.CrossProduct(aq, ab)) * endpointDistance, Math.Abs(Vector2.DotProduct(ab.Normalize(), eq.Normalize())));
        }

        /// <summary>
        /// Outputs a list of (at most three) intersections (their X coordinates) with an infinite horizontal scanline at y and returns how many there are.
        /// </summary>
        public override int ScanlineIntersections(double[] x, int[] dy, double y)
        {
            if ((y >= p[0].Y && y < p[1].Y) || (y >= p[1].Y && y < p[0].Y))
            {
                double param = (y - p[0].Y) / (p[1].Y - p[0].Y);
                x[0] = Arithmetics.Mix(p[0].X, p[1].X, param);
                dy[0] = Arithmetics.Sign(p[1].Y - p[0].Y);
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Adjusts the bounding box to fit the edge segment.
        /// </summary>
        public override void Bound(ref double xMin, ref double yMin, ref double xMax, ref double yMax)
        {
            PointBounds(p[0], ref xMin, ref yMin, ref xMax, ref yMax);
            PointBounds(p[1], ref xMin, ref yMin, ref xMax, ref yMax);
        }

        /// <summary>
        /// Reverses the edge (swaps its start point and end point).
        /// </summary>
        public override void Reverse()
        {
            Vector2 tmp = p[0];
            p[0] = p[1];
            p[1] = tmp;
        }

        /// <summary>
        /// Moves the start point of the edge segment.
        /// </summary>
        public override void MoveStartPoint(Vector2 to)
        {
            p[0] = to;
        }

        /// <summary>
        /// Moves the end point of the edge segment.
        /// </summary>
        public override void MoveEndPoint(Vector2 to)
        {
            p[1] = to;
        }

        /// <summary>
        /// Splits the edge segments into thirds which together represent the original edge.
        /// </summary>
        public override void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2)
        {
            part0 = new LinearSegment(p[0], Point(1.0 / 3.0), Color);
            part1 = new LinearSegment(Point(1.0 / 3.0), Point(2.0 / 3.0), Color);
            part2 = new LinearSegment(Point(2.0 / 3.0), p[1], Color);
        }

        private static void PointBounds(Vector2 p, ref double xMin, ref double yMin, ref double xMax, ref double yMax)
        {
            if (p.X < xMin) xMin = p.X;
            if (p.Y < yMin) yMin = p.Y;
            if (p.X > xMax) xMax = p.X;
            if (p.Y > yMax) yMax = p.Y;
        }
    }
}
