using System.Collections.Generic;
using System.Linq;

namespace Msdfgen
{
    public class Contour
    {
        public List<EdgeSegment> Edges { get; } = new List<EdgeSegment>();

        public void AddEdge(EdgeSegment edge)
        {
            Edges.Add(edge);
        }

        public void Bound(ref double xMin, ref double yMin, ref double xMax, ref double yMax)
        {
            foreach (var edge in Edges)
            {
                edge.Bound(ref xMin, ref yMin, ref xMax, ref yMax);
            }
        }

        public void BoundMiters(ref double xMin, ref double yMin, ref double xMax, ref double yMax, double border, double miterLimit, int polarity)
        {
            if (Edges.Count == 0)
                return;

            Vector2 prevDir = Edges.Last().Direction(1).Normalize(true);
            foreach (var edge in Edges)
            {
                Vector2 dir = -edge.Direction(0).Normalize(true);
                if (polarity * Vector2.CrossProduct(prevDir, dir) >= 0)
                {
                    double miterLength = miterLimit;
                    double q = 0.5 * (1 - Vector2.DotProduct(prevDir, dir));
                    if (q > 0)
                        miterLength = Arithmetics.Min(1 / System.Math.Sqrt(q), miterLimit);
                    Vector2 miter = edge.Point(0) + border * miterLength * (prevDir + dir).Normalize(true);
                    BoundPoint(ref xMin, ref yMin, ref xMax, ref yMax, miter);
                }
                prevDir = edge.Direction(1).Normalize(true);
            }
        }

        public int Winding()
        {
            if (Edges.Count == 0)
                return 0;
            double total = 0;
            if (Edges.Count == 1)
            {
                Vector2 a = Edges[0].Point(0);
                Vector2 b = Edges[0].Point(1.0 / 3.0);
                Vector2 c = Edges[0].Point(2.0 / 3.0);
                total += Shoelace(a, b);
                total += Shoelace(b, c);
                total += Shoelace(c, a);
            }
            else if (Edges.Count == 2)
            {
                Vector2 a = Edges[0].Point(0);
                Vector2 b = Edges[0].Point(0.5);
                Vector2 c = Edges[1].Point(0);
                Vector2 d = Edges[1].Point(0.5);
                total += Shoelace(a, b);
                total += Shoelace(b, c);
                total += Shoelace(c, d);
                total += Shoelace(d, a);
            }
            else
            {
                Vector2 prev = Edges.Last().Point(0);
                foreach (var edge in Edges)
                {
                    Vector2 cur = edge.Point(0);
                    total += Shoelace(prev, cur);
                    prev = cur;
                }
            }
            return Arithmetics.Sign(total);
        }

        public void Reverse()
        {
             int n = Edges.Count;
             for (int i = 0; i < n / 2; i++)
             {
                 EdgeSegment tmp = Edges[i];
                 Edges[i] = Edges[n - 1 - i];
                 Edges[n - 1 - i] = tmp;
             }
             foreach (var edge in Edges)
             {
                 edge.Reverse();
             }
        }

        private static double Shoelace(Vector2 a, Vector2 b)
        {
            return (b.X - a.X) * (a.Y + b.Y);
        }

        private static void BoundPoint(ref double xMin, ref double yMin, ref double xMax, ref double yMax, Vector2 p)
        {
            if (p.X < xMin) xMin = p.X;
            if (p.Y < yMin) yMin = p.Y;
            if (p.X > xMax) xMax = p.X;
            if (p.Y > yMax) yMax = p.Y;
        }
    }
}
