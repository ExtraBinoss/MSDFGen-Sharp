using System;
using System.Collections.Generic;
using System.Linq;

namespace Msdfgen
{
    public class Shape
    {
        /// <summary>
        /// Represents the bounding box of a shape.
        /// </summary>
        public struct Bounds
        {
            public double L, B, R, T;
        }

        public List<Contour> Contours { get; } = new List<Contour>();
        public bool InverseYAxis { get; private set; } = false;

        public const double CORNER_DOT_EPSILON = 0.000001;
        private const double DECONVERGE_OVERSHOOT = 1.11111111111111111;

        /// <summary>
        /// Initializes an empty shape.
        /// </summary>
        public Shape() { }

        /// <summary>
        /// Adds a contour to the shape.
        /// </summary>
        public void AddContour(Contour contour)
        {
            Contours.Add(contour);
        }

        /// <summary>
        /// Validates that all contours are closed and have no null edges.
        /// </summary>
        public bool Validate()
        {
            foreach (var contour in Contours)
            {
                if (contour.Edges.Count > 0)
                {
                    Vector2 corner = contour.Edges.Last().Point(1);
                    foreach (var edge in contour.Edges)
                    {
                        if (edge == null) return false;
                        if (edge.Point(0) != corner) return false;
                        corner = edge.Point(1);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Normalizes the shape by splitting single-edge contours and deconverging sharp corners.
        /// </summary>
        public void Normalize()
        {
            foreach (var contour in Contours)
            {
                if (contour.Edges.Count == 1)
                {
                    contour.Edges[0].SplitInThirds(out var part0, out var part1, out var part2);
                    contour.Edges.Clear();
                    contour.Edges.Add(part0);
                    contour.Edges.Add(part1);
                    contour.Edges.Add(part2);
                }
                else if (contour.Edges.Count > 0)
                {
                    int prevEdgeIndex = contour.Edges.Count - 1;
                    
                    for (int i = 0; i < contour.Edges.Count; ++i) 
                    {
                        var prevEdge = contour.Edges[prevEdgeIndex];
                        var edge = contour.Edges[i];

                        Vector2 prevDir = prevEdge.Direction(1).Normalize();
                        Vector2 curDir = edge.Direction(0).Normalize();

                        if (Vector2.DotProduct(prevDir, curDir) < CORNER_DOT_EPSILON - 1)
                        {
                            double factor = DECONVERGE_OVERSHOOT * Math.Sqrt(1 - (CORNER_DOT_EPSILON - 1) * (CORNER_DOT_EPSILON - 1)) / (CORNER_DOT_EPSILON - 1);
                            Vector2 axis = factor * (curDir - prevDir).Normalize();
                            if (ConvergentCurveOrdering.Find(prevEdge, edge) < 0)
                                axis = -axis;
                                
                            DeconvergeEdge(contour.Edges, prevEdgeIndex, 1, axis.GetOrthogonal(true));
                            DeconvergeEdge(contour.Edges, i, 0, axis.GetOrthogonal(false));
                        }
                        prevEdgeIndex = i;
                    }
                }
            }
        }

        /// <summary>
        /// Slightly modifies an edge to avoid degenerate corner conditions.
        /// </summary>
        private void DeconvergeEdge(List<EdgeSegment> edges, int index, int param, Vector2 vector)
        {
            EdgeSegment edge = edges[index];
            if (edge is QuadraticSegment quad)
            {
                edge = quad.ConvertToCubic();
                edges[index] = edge; // update list
            }
            
            if (edge is CubicSegment cubic)
            {
                Vector2[] p = cubic.ControlPoints;
                switch (param)
                {
                    case 0:
                        p[1] += (p[1] - p[0]).Length() * vector;
                        break;
                    case 1:
                        p[2] += (p[2] - p[3]).Length() * vector;
                        break;
                }
            }
        }

        /// <summary>
        /// Adjusts the bounding box to fit the shape.
        /// </summary>
        public void Bound(ref double xMin, ref double yMin, ref double xMax, ref double yMax)
        {
            foreach (var contour in Contours)
                contour.Bound(ref xMin, ref yMin, ref xMax, ref yMax);
        }

        /// <summary>
        /// Adjusts the bounding box to fit the shape with miters.
        /// </summary>
        public void BoundMiters(ref double xMin, ref double yMin, ref double xMax, ref double yMax, double border, double miterLimit, int polarity)
        {
            foreach (var contour in Contours)
                contour.BoundMiters(ref xMin, ref yMin, ref xMax, ref yMax, border, miterLimit, polarity);
        }

        /// <summary>
        /// Computes and returns the bounding box of the shape.
        /// </summary>
        public Bounds GetBounds(double border = 0, double miterLimit = 0, int polarity = 0)
        {
            double largeValue = 1e240;
            Bounds bounds = new Bounds { L = largeValue, B = largeValue, R = -largeValue, T = -largeValue };
            Bound(ref bounds.L, ref bounds.B, ref bounds.R, ref bounds.T);
            if (border > 0)
            {
                bounds.L -= border; bounds.B -= border;
                bounds.R += border; bounds.T += border;
                if (miterLimit > 0)
                    BoundMiters(ref bounds.L, ref bounds.B, ref bounds.R, ref bounds.T, border, miterLimit, polarity);
            }
            return bounds;
        }

        /// <summary>
        /// Generates a scanline for the shape at the specified y-coordinate.
        /// </summary>
        public void Scanline(Scanline line, double y)
        {
            var intersections = new List<Scanline.Intersection>();
            double[] x = new double[3];
            int[] dy = new int[3];
            
            foreach (var contour in Contours)
            {
                foreach (var edge in contour.Edges)
                {
                    int n = edge.ScanlineIntersections(x, dy, y);
                    for (int i = 0; i < n; ++i)
                    {
                        intersections.Add(new Scanline.Intersection { X = x[i], Direction = dy[i] });
                    }
                }
            }
            line.SetIntersections(intersections);
        }

        /// <summary>
        /// Returns the total number of edges in all contours.
        /// </summary>
        public int EdgeCount()
        {
            return Contours.Sum(c => c.Edges.Count);
        }

        private struct IntersectionInfo : IComparable<IntersectionInfo>
        {
            public double X;
            public int Direction;
            public int ContourIndex;

            public int CompareTo(IntersectionInfo other)
            {
                return Arithmetics.Sign(X - other.X);
            }
        }

        /// <summary>
        /// Ensures all contours have consistent orientation.
        /// </summary>
        public void OrientContours()
        {
            double ratio = 0.5 * (Math.Sqrt(5) - 1);
            int[] orientations = new int[Contours.Count];
            var intersections = new List<IntersectionInfo>();
            
            for (int i = 0; i < Contours.Count; ++i)
            {
                if (orientations[i] == 0 && Contours[i].Edges.Count > 0)
                {
                    double y0 = Contours[i].Edges[0].Point(0).Y;
                    double y1 = y0;
                    int edgeIdx = 0;
                    while (edgeIdx < Contours[i].Edges.Count && y0 == y1) {
                         y1 = Contours[i].Edges[edgeIdx].Point(1).Y;
                         edgeIdx++;
                    }
                    // in case all endpoints are in a horizontal line
                    edgeIdx = 0;
                     while (edgeIdx < Contours[i].Edges.Count && y0 == y1) {
                         y1 = Contours[i].Edges[edgeIdx].Point(ratio).Y;
                         edgeIdx++;
                    }
                    
                    double y = Arithmetics.Mix(y0, y1, ratio);
                    
                    double[] x = new double[3];
                    int[] dy = new int[3];
                    
                    for (int j = 0; j < Contours.Count; ++j)
                    {
                        foreach (var edge in Contours[j].Edges)
                        {
                            int n = edge.ScanlineIntersections(x, dy, y);
                            for (int k = 0; k < n; ++k)
                            {
                                intersections.Add(new IntersectionInfo { X = x[k], Direction = dy[k], ContourIndex = j });
                            }
                        }
                    }
                    
                    if (intersections.Count > 0)
                    {
                        intersections.Sort();
                        
                        // Disqualify multiple intersections
                        for (int j = 1; j < intersections.Count; ++j)
                        {
                            if (intersections[j].X == intersections[j - 1].X)
                            {
                                var i1 = intersections[j]; i1.Direction = 0; intersections[j] = i1;
                                var i2 = intersections[j - 1]; i2.Direction = 0; intersections[j - 1] = i2;
                            }
                        }
                        
                        for (int j = 0; j < intersections.Count; ++j)
                        {
                            if (intersections[j].Direction != 0)
                            {
                                int val = 2 * ((j & 1) ^ (intersections[j].Direction > 0 ? 1 : 0)) - 1;
                                orientations[intersections[j].ContourIndex] += val;
                            }
                        }
                        intersections.Clear();
                    }
                }
            }
            
            for (int i = 0; i < Contours.Count; ++i)
                if (orientations[i] < 0)
                    Contours[i].Reverse();
        }

        /// <summary>
        /// Returns the Y-axis orientation of the shape.
        /// </summary>
        public YAxisOrientation GetYAxisOrientation()
        {
            return InverseYAxis ? YAxisOrientation.Downward : YAxisOrientation.Upward;
        }

        /// <summary>
        /// Sets the Y-axis orientation of the shape.
        /// </summary>
        public void SetYAxisOrientation(YAxisOrientation yAxisOrientation)
        {
            InverseYAxis = yAxisOrientation != YAxisOrientation.Upward;
        }
    }
}
