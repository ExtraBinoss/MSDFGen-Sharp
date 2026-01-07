using System;
using System.Collections.Generic;

namespace Msdfgen
{
    public class Scanline
    {
        /// <summary>
        /// Represents an intersection of the scanline with a shape edge.
        /// </summary>
        public struct Intersection
        {
            public double X;
            public int Direction;
        }

        private readonly List<Intersection> intersections = new List<Intersection>();
        private int lastIndex = 0;

        /// <summary>
        /// Computes the total overlapping length of filled regions between two scanlines.
        /// </summary>
        public static double Overlap(Scanline a, Scanline b, double xFrom, double xTo, FillRule fillRule)
        {
            double total = 0;
            bool aInside = false, bInside = false;
            int ai = 0, bi = 0;
            double ax = a.intersections.Count > 0 ? a.intersections[ai].X : xTo;
            double bx = b.intersections.Count > 0 ? b.intersections[bi].X : xTo;

            while (ax < xFrom || bx < xFrom)
            {
                double xNext = Arithmetics.Min(ax, bx);
                if (ax == xNext && ai < a.intersections.Count)
                {
                    aInside = fillRule.Interpret(a.intersections[ai].Direction);
                    ax = ++ai < a.intersections.Count ? a.intersections[ai].X : xTo;
                }
                if (bx == xNext && bi < b.intersections.Count)
                {
                    bInside = fillRule.Interpret(b.intersections[bi].Direction);
                    bx = ++bi < b.intersections.Count ? b.intersections[bi].X : xTo;
                }
            }

            double x = xFrom;
            while (ax < xTo || bx < xTo)
            {
                double xNext = Arithmetics.Min(ax, bx);
                if (aInside == bInside)
                    total += xNext - x;
                if (ax == xNext && ai < a.intersections.Count)
                {
                    aInside = fillRule.Interpret(a.intersections[ai].Direction);
                    ax = ++ai < a.intersections.Count ? a.intersections[ai].X : xTo;
                }
                if (bx == xNext && bi < b.intersections.Count)
                {
                    bInside = fillRule.Interpret(b.intersections[bi].Direction);
                    bx = ++bi < b.intersections.Count ? b.intersections[bi].X : xTo;
                }
                x = xNext;
            }
            if (aInside == bInside)
                total += xTo - x;
            return total;
        }

        /// <summary>
        /// Sets the intersections for the scanline and preprocesses them.
        /// </summary>
        public void SetIntersections(List<Intersection> intersections)
        {
            this.intersections.Clear();
            this.intersections.AddRange(intersections);
            Preprocess();
        }

        /// <summary>
        /// Returns the number of intersections to the left of x.
        /// </summary>
        public int CountIntersections(double x)
        {
            return MoveTo(x) + 1;
        }

        /// <summary>
        /// Returns the sum of intersection directions to the left of x.
        /// </summary>
        public int SumIntersections(double x)
        {
            int index = MoveTo(x);
            if (index >= 0)
                return intersections[index].Direction;
            return 0;
        }

        /// <summary>
        /// Returns whether the scanline is filled at the specified x-coordinate.
        /// </summary>
        public bool Filled(double x, FillRule fillRule)
        {
            return fillRule.Interpret(SumIntersections(x));
        }

        private void Preprocess()
        {
            lastIndex = 0;
            if (intersections.Count > 0)
            {
                intersections.Sort((a, b) => Arithmetics.Sign(a.X - b.X));
                int totalDirection = 0;
                for (int i = 0; i < intersections.Count; i++)
                {
                    totalDirection += intersections[i].Direction;
                    var intersection = intersections[i];
                    intersection.Direction = totalDirection;
                    intersections[i] = intersection;
                }
            }
        }

        private int MoveTo(double x)
        {
            if (intersections.Count == 0)
                return -1;
            int index = lastIndex;
            if (x < intersections[index].X)
            {
                do
                {
                    if (index == 0)
                    {
                        lastIndex = 0;
                        return -1;
                    }
                    --index;
                } while (x < intersections[index].X);
            }
            else
            {
                while (index < intersections.Count - 1 && x >= intersections[index + 1].X)
                    ++index;
            }
            lastIndex = index;
            return index;
        }
    }
}
