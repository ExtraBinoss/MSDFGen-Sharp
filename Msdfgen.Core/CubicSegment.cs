using System;
using static Msdfgen.Arithmetics;

namespace Msdfgen
{
    public class CubicSegment : EdgeSegment
    {
        private readonly Vector2[] p = new Vector2[4];

        // Parameters for iterative search of closest point on a cubic Bezier curve. Increase for higher precision.
        private const int MSDFGEN_CUBIC_SEARCH_STARTS = 4;
        private const int MSDFGEN_CUBIC_SEARCH_STEPS = 4;

        public override int Type => 3;

        public override Vector2[] ControlPoints => p;

        /// <summary>
        /// Initializes a new cubic Bezier edge segment with four control points and a color.
        /// </summary>
        public CubicSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, EdgeColor edgeColor = EdgeColor.WHITE) : base(edgeColor)
        {
            p[0] = p0;
            p[1] = p1;
            p[2] = p2;
            p[3] = p3;
        }

        /// <summary>
        /// Creates a copy of the edge segment.
        /// </summary>
        public override EdgeSegment Clone()
        {
            return new CubicSegment(p[0], p[1], p[2], p[3], Color);
        }

        /// <summary>
        /// Returns the point on the edge specified by the parameter (between 0 and 1).
        /// </summary>
        public override Vector2 Point(double param)
        {
            Vector2 p12 = Mix(p[1], p[2], param);
            return Mix(Mix(Mix(p[0], p[1], param), p12, param), Mix(p12, Mix(p[2], p[3], param), param), param);
        }

        /// <summary>
        /// Returns the direction the edge has at the point specified by the parameter.
        /// </summary>
        public override Vector2 Direction(double param)
        {
             Vector2 tangent = Mix(Mix(p[1] - p[0], p[2] - p[1], param), Mix(p[2] - p[1], p[3] - p[2], param), param);
            if (!tangent)
            {
                if (param == 0) return p[2] - p[0];
                if (param == 1) return p[3] - p[1];
            }
            return tangent;
        }

        /// <summary>
        /// Returns the change of direction (second derivative) at the point specified by the parameter.
        /// </summary>
        public override Vector2 DirectionChange(double param)
        {
            return Mix((p[2] - p[1]) - (p[1] - p[0]), (p[3] - p[2]) - (p[2] - p[1]), param);
        }

        /// <summary>
        /// Returns the minimum signed distance between origin and the edge.
        /// </summary>
        public override SignedDistance SignedDistance(Vector2 origin, out double param)
        {
            Vector2 qa = p[0] - origin;
            Vector2 ab = p[1] - p[0];
            Vector2 br = p[2] - p[1] - ab;
            Vector2 @as = (p[3] - p[2]) - (p[2] - p[1]) - br;

            Vector2 epDir = Direction(0);
            double minDistance = NonZeroSign(Vector2.CrossProduct(epDir, qa)) * qa.Length(); // distance from A
            param = -Vector2.DotProduct(qa, epDir) / Vector2.DotProduct(epDir, epDir);
            {
                double distance = (p[3] - origin).Length(); // distance from B
                if (distance < Math.Abs(minDistance))
                {
                    epDir = Direction(1);
                    minDistance = NonZeroSign(Vector2.CrossProduct(epDir, p[3] - origin)) * distance;
                    param = Vector2.DotProduct(epDir - (p[3] - origin), epDir) / Vector2.DotProduct(epDir, epDir);
                }
            }
            // Iterative minimum distance search
            for (int i = 0; i <= MSDFGEN_CUBIC_SEARCH_STARTS; ++i)
            {
                double t = 1.0 / MSDFGEN_CUBIC_SEARCH_STARTS * i;
                Vector2 qe = qa + 3 * t * ab + 3 * t * t * br + t * t * t * @as;
                Vector2 d1 = 3 * ab + 6 * t * br + 3 * t * t * @as;
                Vector2 d2 = 6 * br + 6 * t * @as;
                double improvedT = t - Vector2.DotProduct(qe, d1) / (Vector2.DotProduct(d1, d1) + Vector2.DotProduct(qe, d2));
                if (improvedT > 0 && improvedT < 1)
                {
                    int remainingSteps = MSDFGEN_CUBIC_SEARCH_STEPS;
                    do
                    {
                        t = improvedT;
                        qe = qa + 3 * t * ab + 3 * t * t * br + t * t * t * @as;
                        d1 = 3 * ab + 6 * t * br + 3 * t * t * @as;
                        if (--remainingSteps == 0)
                            break;
                        d2 = 6 * br + 6 * t * @as;
                        improvedT = t - Vector2.DotProduct(qe, d1) / (Vector2.DotProduct(d1, d1) + Vector2.DotProduct(qe, d2));
                    } while (improvedT > 0 && improvedT < 1);
                    double distance = qe.Length();
                    if (distance < Math.Abs(minDistance))
                    {
                        minDistance = NonZeroSign(Vector2.CrossProduct(d1, qe)) * distance;
                        param = t;
                    }
                }
            }

            if (param >= 0 && param <= 1)
                return new SignedDistance(minDistance, 0);
            if (param < 0.5)
                return new SignedDistance(minDistance, Math.Abs(Vector2.DotProduct(Direction(0).Normalize(), qa.Normalize())));
            else
                return new SignedDistance(minDistance, Math.Abs(Vector2.DotProduct(Direction(1).Normalize(), (p[3] - origin).Normalize())));
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
                if (p[0].Y < p[1].Y || (p[0].Y == p[1].Y && (p[0].Y < p[2].Y || (p[0].Y == p[2].Y && p[0].Y < p[3].Y))))
                    dy[total++] = 1;
                else
                    nextDY = 1;
            }
            {
                Vector2 ab = p[1] - p[0];
                Vector2 br = p[2] - p[1] - ab;
                Vector2 @as = (p[3] - p[2]) - (p[2] - p[1]) - br;
                double[] t = new double[3];
                int solutions = EquationSolver.SolveCubic(t, @as.Y, 3 * br.Y, 3 * ab.Y, p[0].Y - y);
                // Sort solutions
                double tmp;
                if (solutions >= 2)
                {
                    if (t[0] > t[1])
                    {
                        tmp = t[0]; t[0] = t[1]; t[1] = tmp;
                    }
                    if (solutions >= 3 && t[1] > t[2])
                    {
                        tmp = t[1]; t[1] = t[2]; t[2] = tmp;
                        if (t[0] > t[1])
                        {
                            tmp = t[0]; t[0] = t[1]; t[1] = tmp;
                        }
                    }
                }
                for (int i = 0; i < solutions && total < 3; ++i)
                {
                    if (t[i] >= 0 && t[i] <= 1)
                    {
                        x[total] = p[0].X + 3 * t[i] * ab.X + 3 * t[i] * t[i] * br.X + t[i] * t[i] * t[i] * @as.X;
                        if (nextDY * (ab.Y + 2 * t[i] * br.Y + t[i] * t[i] * @as.Y) >= 0)
                        {
                            dy[total++] = nextDY;
                            nextDY = -nextDY;
                        }
                    }
                }
            }
            if (p[3].Y == y)
            {
                if (nextDY > 0 && total > 0)
                {
                    --total;
                    nextDY = -1;
                }
                if ((p[3].Y < p[2].Y || (p[3].Y == p[2].Y && (p[3].Y < p[1].Y || (p[3].Y == p[1].Y && p[3].Y < p[0].Y)))) && total < 3)
                {
                    x[total] = p[3].X;
                    if (nextDY < 0)
                    {
                        dy[total++] = -1;
                        nextDY = 1;
                    }
                }
            }
            if (nextDY != (y >= p[3].Y ? 1 : -1))
            {
                if (total > 0)
                    --total;
                else
                {
                    if (Math.Abs(p[3].Y - y) < Math.Abs(p[0].Y - y))
                        x[total] = p[3].X;
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
            PointBounds(p[3], ref xMin, ref yMin, ref xMax, ref yMax);
            Vector2 a0 = p[1] - p[0];
            Vector2 a1 = 2 * (p[2] - p[1] - a0);
            Vector2 a2 = p[3] - 3 * p[2] + 3 * p[1] - p[0];
            double[] parameters = new double[2];
            int solutions;
            solutions = EquationSolver.SolveQuadratic(parameters, a2.X, a1.X, a0.X);
            for (int i = 0; i < solutions; ++i)
                if (parameters[i] > 0 && parameters[i] < 1)
                    PointBounds(Point(parameters[i]), ref xMin, ref yMin, ref xMax, ref yMax);
            solutions = EquationSolver.SolveQuadratic(parameters, a2.Y, a1.Y, a0.Y);
            for (int i = 0; i < solutions; ++i)
                if (parameters[i] > 0 && parameters[i] < 1)
                    PointBounds(Point(parameters[i]), ref xMin, ref yMin, ref xMax, ref yMax);
        }

        /// <summary>
        /// Reverses the edge (swaps its start point and end point).
        /// </summary>
        public override void Reverse()
        {
            Vector2 tmp = p[0];
            p[0] = p[3];
            p[3] = tmp;
            tmp = p[1];
            p[1] = p[2];
            p[2] = tmp;
        }

        /// <summary>
        /// Moves the start point of the edge segment.
        /// </summary>
        public override void MoveStartPoint(Vector2 to)
        {
            p[1] += to - p[0];
            p[0] = to;
        }

        /// <summary>
        /// Moves the end point of the edge segment.
        /// </summary>
        public override void MoveEndPoint(Vector2 to)
        {
            p[2] += to - p[3];
            p[3] = to;
        }

        /// <summary>
        /// Splits the edge segments into thirds which together represent the original edge.
        /// </summary>
        public override void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2)
        {
            part0 = new CubicSegment(p[0], p[0] == p[1] ? p[0] : Mix(p[0], p[1], 1.0 / 3.0), Mix(Mix(p[0], p[1], 1.0 / 3.0), Mix(p[1], p[2], 1.0 / 3.0), 1.0 / 3.0), Point(1.0 / 3.0), Color);
            
            part1 = new CubicSegment(Point(1.0 / 3.0),
                Mix(Mix(Mix(p[0], p[1], 1.0 / 3.0), Mix(p[1], p[2], 1.0 / 3.0), 1.0 / 3.0), Mix(Mix(p[1], p[2], 1.0 / 3.0), Mix(p[2], p[3], 1.0 / 3.0), 1.0 / 3.0), 2.0 / 3.0),
                Mix(Mix(Mix(p[0], p[1], 2.0 / 3.0), Mix(p[1], p[2], 2.0 / 3.0), 2.0 / 3.0), Mix(Mix(p[1], p[2], 2.0 / 3.0), Mix(p[2], p[3], 2.0 / 3.0), 2.0 / 3.0), 1.0 / 3.0),
                Point(2.0 / 3.0), Color);
            
            part2 = new CubicSegment(Point(2.0 / 3.0), Mix(Mix(p[1], p[2], 2.0 / 3.0), Mix(p[2], p[3], 2.0 / 3.0), 2.0 / 3.0), p[2] == p[3] ? p[3] : Mix(p[2], p[3], 2.0 / 3.0), p[3], Color);
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
