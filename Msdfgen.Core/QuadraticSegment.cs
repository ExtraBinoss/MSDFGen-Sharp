using System;
using static Msdfgen.Arithmetics;

namespace Msdfgen
{
    public class QuadraticSegment : EdgeSegment
    {
        private readonly Vector2[] p = new Vector2[3];

        public override int Type => 2;

        public override Vector2[] ControlPoints => p;

        /// <summary>
        /// Initializes a new quadratic Bezier edge segment with three control points and a color.
        /// </summary>
        public QuadraticSegment(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor edgeColor = EdgeColor.WHITE) : base(edgeColor)
        {
            p[0] = p0;
            p[1] = p1;
            p[2] = p2;
        }

        /// <summary>
        /// Creates a copy of the edge segment.
        /// </summary>
        public override EdgeSegment Clone()
        {
            return new QuadraticSegment(p[0], p[1], p[2], Color);
        }

        /// <summary>
        /// Returns the point on the edge specified by the parameter (between 0 and 1).
        /// </summary>
        public override Vector2 Point(double param)
        {
            return Mix(Mix(p[0], p[1], param), Mix(p[1], p[2], param), param);
        }

        /// <summary>
        /// Returns the direction the edge has at the point specified by the parameter.
        /// </summary>
        public override Vector2 Direction(double param)
        {
            Vector2 tangent = Mix(p[1] - p[0], p[2] - p[1], param);
            if (!tangent)
                return p[2] - p[0];
            return tangent;
        }

        /// <summary>
        /// Returns the change of direction (second derivative) at the point specified by the parameter.
        /// </summary>
        public override Vector2 DirectionChange(double param)
        {
            return (p[2] - p[1]) - (p[1] - p[0]);
        }

        /// <summary>
        /// Returns the length of the quadratic segment.
        /// </summary>
        public double Length()
        {
            Vector2 ab = p[1] - p[0];
            Vector2 br = p[2] - p[1] - ab;
            double abab = Vector2.DotProduct(ab, ab);
            double abbr = Vector2.DotProduct(ab, br);
            double brbr = Vector2.DotProduct(br, br);
            double abLen = Math.Sqrt(abab);
            double brLen = Math.Sqrt(brbr);
            double crs = Vector2.CrossProduct(ab, br);
            double h = Math.Sqrt(abab + abbr + abbr + brbr);
            
            if (brbr == 0 || brLen == 0) return 2 * abLen;

            return (
                brLen * ((abbr + brbr) * h - abbr * abLen) +
                crs * crs * Math.Log((brLen * h + abbr + brbr) / (brLen * abLen + abbr))
            ) / (brbr * brLen);
        }

        /// <summary>
        /// Returns the minimum signed distance between origin and the edge.
        /// </summary>
        public override SignedDistance SignedDistance(Vector2 origin, out double param)
        {
            Vector2 qa = p[0] - origin;
            Vector2 ab = p[1] - p[0];
            Vector2 br = p[2] - p[1] - ab;
            double a = Vector2.DotProduct(br, br);
            double b = 3 * Vector2.DotProduct(ab, br);
            double c = 2 * Vector2.DotProduct(ab, ab) + Vector2.DotProduct(qa, br);
            double d = Vector2.DotProduct(qa, ab);
            double[] t = new double[3];
            int solutions = EquationSolver.SolveCubic(t, a, b, c, d);

            Vector2 epDir = Direction(0);
            double minDistance = NonZeroSign(Vector2.CrossProduct(epDir, qa)) * qa.Length(); // distance from A
            param = -Vector2.DotProduct(qa, epDir) / Vector2.DotProduct(epDir, epDir);
            {
                double distance = (p[2] - origin).Length(); // distance from B
                if (distance < Math.Abs(minDistance))
                {
                    epDir = Direction(1);
                    minDistance = NonZeroSign(Vector2.CrossProduct(epDir, p[2] - origin)) * distance;
                    param = Vector2.DotProduct(origin - p[1], epDir) / Vector2.DotProduct(epDir, epDir);
                }
            }
            for (int i = 0; i < solutions; ++i)
            {
                if (t[i] > 0 && t[i] < 1)
                {
                    Vector2 qe = qa + 2 * t[i] * ab + t[i] * t[i] * br;
                    double distance = qe.Length();
                    if (distance <= Math.Abs(minDistance))
                    {
                        minDistance = NonZeroSign(Vector2.CrossProduct(ab + t[i] * br, qe)) * distance;
                        param = t[i];
                    }
                }
            }

            if (param >= 0 && param <= 1)
                return new SignedDistance(minDistance, 0);
            if (param < 0.5)
                return new SignedDistance(minDistance, Math.Abs(Vector2.DotProduct(Direction(0).Normalize(), qa.Normalize())));
            else
                return new SignedDistance(minDistance, Math.Abs(Vector2.DotProduct(Direction(1).Normalize(), (p[2] - origin).Normalize())));
        }

        /// <summary>
        /// Outputs a list of (at most three) intersections (their X coordinates) with an infinite horizontal scanline at y and returns how many there are.
        /// </summary>
        public override int ScanlineIntersections(double[] x, int[] dy, double y)
        {
            int total = 0;
            int nextDY = y > p[0].Y ? 1 : -1;
            x[total] = p[0].X;
            if (p[0].Y == y)
            {
                if (p[0].Y < p[1].Y || (p[0].Y == p[1].Y && p[0].Y < p[2].Y))
                    dy[total++] = 1;
                else
                    nextDY = 1;
            }
            {
                Vector2 ab = p[1] - p[0];
                Vector2 br = p[2] - p[1] - ab;
                double[] t = new double[2];
                int solutions = EquationSolver.SolveQuadratic(t, br.Y, 2 * ab.Y, p[0].Y - y);
                
                double tmp;
                if (solutions >= 2 && t[0] > t[1])
                {
                    tmp = t[0]; t[0] = t[1]; t[1] = tmp;
                }
                for (int i = 0; i < solutions && total < 2; ++i)
                {
                    if (t[i] >= 0 && t[i] <= 1)
                    {
                        x[total] = p[0].X + 2 * t[i] * ab.X + t[i] * t[i] * br.X;
                        if (nextDY * (ab.Y + t[i] * br.Y) >= 0)
                        {
                            dy[total++] = nextDY;
                            nextDY = -nextDY;
                        }
                    }
                }
            }
            if (p[2].Y == y)
            {
                if (nextDY > 0 && total > 0)
                {
                    --total;
                    nextDY = -1;
                }
                if ((p[2].Y < p[1].Y || (p[2].Y == p[1].Y && p[2].Y < p[0].Y)) && total < 2)
                {
                    x[total] = p[2].X;
                    if (nextDY < 0)
                    {
                        dy[total++] = -1;
                        nextDY = 1;
                    }
                }
            }
            if (nextDY != (y >= p[2].Y ? 1 : -1))
            {
                if (total > 0)
                    --total;
                else
                {
                    if (Math.Abs(p[2].Y - y) < Math.Abs(p[0].Y - y))
                        x[total] = p[2].X;
                    dy[total++] = nextDY;
                }
            }
            return total;
        }

        /// <summary>
        /// Adjusts the bounding box to fit the edge segment.
        /// </summary>
        public override void Bound(ref double xMin, ref double yMin, ref double xMax, ref double yMax)
        {
            PointBounds(p[0], ref xMin, ref yMin, ref xMax, ref yMax);
            PointBounds(p[2], ref xMin, ref yMin, ref xMax, ref yMax);
            Vector2 bot = (p[1] - p[0]) - (p[2] - p[1]);
            if (bot.X != 0)
            {
                double param = (p[1].X - p[0].X) / bot.X;
                if (param > 0 && param < 1)
                    PointBounds(Point(param), ref xMin, ref yMin, ref xMax, ref yMax);
            }
            if (bot.Y != 0)
            {
                double param = (p[1].Y - p[0].Y) / bot.Y;
                if (param > 0 && param < 1)
                    PointBounds(Point(param), ref xMin, ref yMin, ref xMax, ref yMax);
            }
        }

        /// <summary>
        /// Reverses the edge (swaps its start point and end point).
        /// </summary>
        public override void Reverse()
        {
            Vector2 tmp = p[0];
            p[0] = p[2];
            p[2] = tmp;
        }

        /// <summary>
        /// Moves the start point of the edge segment.
        /// </summary>
        public override void MoveStartPoint(Vector2 to)
        {
            Vector2 origSDir = p[0] - p[1];
            Vector2 origP1 = p[1];
            p[1] += Vector2.CrossProduct(p[0] - p[1], to - p[0]) / Vector2.CrossProduct(p[0] - p[1], p[2] - p[1]) * (p[2] - p[1]);
            p[0] = to;
            if (Vector2.DotProduct(origSDir, p[0] - p[1]) < 0)
                p[1] = origP1;
        }

        /// <summary>
        /// Moves the end point of the edge segment.
        /// </summary>
        public override void MoveEndPoint(Vector2 to)
        {
            Vector2 origEDir = p[2] - p[1];
            Vector2 origP1 = p[1];
            p[1] += Vector2.CrossProduct(p[2] - p[1], to - p[2]) / Vector2.CrossProduct(p[2] - p[1], p[0] - p[1]) * (p[0] - p[1]);
            p[2] = to;
            if (Vector2.DotProduct(origEDir, p[2] - p[1]) < 0)
                p[1] = origP1;
        }

        /// <summary>
        /// Splits the edge segments into thirds which together represent the original edge.
        /// </summary>
        public override void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2)
        {
            part0 = new QuadraticSegment(p[0], Mix(p[0], p[1], 1.0 / 3.0), Point(1.0 / 3.0), Color);
            part1 = new QuadraticSegment(Point(1.0 / 3.0), Mix(Mix(p[0], p[1], 5.0 / 9.0), Mix(p[1], p[2], 4.0 / 9.0), 0.5), Point(2.0 / 3.0), Color);
            part2 = new QuadraticSegment(Point(2.0 / 3.0), Mix(p[1], p[2], 2.0 / 3.0), p[2], Color);
        }

        /// <summary>
        /// Converts the quadratic segment to a cubic Bezier segment.
        /// </summary>
        public CubicSegment ConvertToCubic()
        {
             return new CubicSegment(p[0], Mix(p[0], p[1], 2.0/3.0), Mix(p[1], p[2], 1.0/3.0), p[2], Color);
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
