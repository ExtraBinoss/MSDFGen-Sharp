using System;
using System.Collections.Generic;
using System.Linq;

namespace Msdfgen
{
    public static class EdgeColoring
    {
        private const int MSDFGEN_EDGE_LENGTH_PRECISION = 4;

        private static int SymmetricalTrichotomy(int position, int n)
        {
             return (int)(3 + 2.875 * position / (n - 1) - 1.4375 + 0.5) - 3;
        }

        private static bool IsCorner(Vector2 aDir, Vector2 bDir, double crossThreshold)
        {
            return Vector2.DotProduct(aDir, bDir) <= 0 || Math.Abs(Vector2.CrossProduct(aDir, bDir)) > crossThreshold;
        }

        private static double EstimateEdgeLength(EdgeSegment edge)
        {
            double len = 0;
            Vector2 prev = edge.Point(0);
            for (int i = 1; i <= MSDFGEN_EDGE_LENGTH_PRECISION; ++i)
            {
                Vector2 cur = edge.Point(1.0 / MSDFGEN_EDGE_LENGTH_PRECISION * i);
                len += (cur - prev).Length();
                prev = cur;
            }
            return len;
        }

        private static int SeedExtract2(ref ulong seed)
        {
            int v = (int)(seed & 1);
            seed >>= 1;
            return v;
        }

        private static int SeedExtract3(ref ulong seed)
        {
            int v = (int)(seed % 3);
            seed /= 3;
            return v;
        }

        private static EdgeColor InitColor(ref ulong seed)
        {
            EdgeColor[] colors = { EdgeColor.CYAN, EdgeColor.MAGENTA, EdgeColor.YELLOW };
            return colors[SeedExtract3(ref seed)];
        }

        private static void SwitchColor(ref EdgeColor color, ref ulong seed)
        {
            int shifted = (int)color << (1 + SeedExtract2(ref seed));
            color = (EdgeColor)((shifted | shifted >> 3) & (int)EdgeColor.WHITE);
        }

        private static void SwitchColor(ref EdgeColor color, ref ulong seed, EdgeColor banned)
        {
            EdgeColor combined = (EdgeColor)((int)color & (int)banned);
            if (combined == EdgeColor.RED || combined == EdgeColor.GREEN || combined == EdgeColor.BLUE)
                color = (EdgeColor)((int)combined ^ (int)EdgeColor.WHITE);
            else
                SwitchColor(ref color, ref seed);
        }

        public static void EdgeColoringSimple(Shape shape, double angleThreshold, ulong seed = 0)
        {
            double crossThreshold = Math.Sin(angleThreshold);
            EdgeColor color = InitColor(ref seed);
            var corners = new List<int>();

            foreach (var contour in shape.Contours)
            {
                if (contour.Edges.Count == 0) continue;

                corners.Clear();
                Vector2 prevDirection = contour.Edges.Last().Direction(1);
                for (int i = 0; i < contour.Edges.Count; ++i)
                {
                    if (IsCorner(prevDirection.Normalize(), contour.Edges[i].Direction(0).Normalize(), crossThreshold))
                        corners.Add(i);
                    prevDirection = contour.Edges[i].Direction(1);
                }

                if (corners.Count == 0)
                {
                    SwitchColor(ref color, ref seed);
                    foreach (var edge in contour.Edges)
                        edge.Color = color;
                }
                else if (corners.Count == 1)
                {
                    EdgeColor[] colors = new EdgeColor[3];
                    SwitchColor(ref color, ref seed);
                    colors[0] = color;
                    colors[1] = EdgeColor.WHITE;
                    SwitchColor(ref color, ref seed);
                    colors[2] = color;
                    int corner = corners[0];

                    if (contour.Edges.Count >= 3)
                    {
                        int m = contour.Edges.Count;
                        for (int i = 0; i < m; ++i)
                            contour.Edges[(corner + i) % m].Color = colors[1 + SymmetricalTrichotomy(i, m)];
                    }
                    else if (contour.Edges.Count >= 1)
                    {
                        EdgeSegment[] parts = new EdgeSegment[7];
                        contour.Edges[0].SplitInThirds(out parts[0 + 3 * corner], out parts[1 + 3 * corner], out parts[2 + 3 * corner]);
                        if (contour.Edges.Count >= 2)
                        {
                            contour.Edges[1].SplitInThirds(out parts[3 - 3 * corner], out parts[4 - 3 * corner], out parts[5 - 3 * corner]);
                            parts[0].Color = parts[1].Color = colors[0];
                            parts[2].Color = parts[3].Color = colors[1];
                            parts[4].Color = parts[5].Color = colors[2];
                        }
                        else
                        {
                            parts[0].Color = colors[0];
                            parts[1].Color = colors[1];
                            parts[2].Color = colors[2];
                        }
                        contour.Edges.Clear();
                        for (int i = 0; i < 7; ++i)
                        {
                            if (parts[i] != null)
                                contour.Edges.Add(parts[i]);
                        }
                    }
                }
                else
                {
                    int cornerCount = corners.Count;
                    int spline = 0;
                    int start = corners[0];
                    int m = contour.Edges.Count;
                    SwitchColor(ref color, ref seed);
                    EdgeColor initialColor = color;
                    for (int i = 0; i < m; ++i)
                    {
                        int index = (start + i) % m;
                        if (spline + 1 < cornerCount && corners[spline + 1] == index)
                        {
                            ++spline;
                            SwitchColor(ref color, ref seed, (EdgeColor)((spline == cornerCount - 1 ? 1 : 0) * (int)initialColor));
                        }
                        contour.Edges[index].Color = color;
                    }
                }
            }
        }

        private struct InkTrapCorner
        {
            public int Index;
            public double PrevEdgeLengthEstimate;
            public bool Minor;
            public EdgeColor Color;
        }

        public static void EdgeColoringInkTrap(Shape shape, double angleThreshold, ulong seed = 0)
        {
            double crossThreshold = Math.Sin(angleThreshold);
            EdgeColor color = InitColor(ref seed);
            var corners = new List<InkTrapCorner>();

            foreach (var contour in shape.Contours)
            {
                if (contour.Edges.Count == 0) continue;
                double splineLength = 0;
                
                corners.Clear();
                Vector2 prevDirection = contour.Edges.Last().Direction(1);
                for (int i = 0; i < contour.Edges.Count; ++i)
                {
                    if (IsCorner(prevDirection.Normalize(), contour.Edges[i].Direction(0).Normalize(), crossThreshold))
                    {
                        corners.Add(new InkTrapCorner { Index = i, PrevEdgeLengthEstimate = splineLength });
                        splineLength = 0;
                    }
                    splineLength += EstimateEdgeLength(contour.Edges[i]);
                    prevDirection = contour.Edges[i].Direction(1);
                }

                if (corners.Count == 0)
                {
                    SwitchColor(ref color, ref seed);
                    foreach (var edge in contour.Edges)
                        edge.Color = color;
                }
                else if (corners.Count == 1)
                {
                   // Same as Simple
                    EdgeColor[] colors = new EdgeColor[3];
                    SwitchColor(ref color, ref seed);
                    colors[0] = color;
                    colors[1] = EdgeColor.WHITE;
                    SwitchColor(ref color, ref seed);
                    colors[2] = color;
                    int corner = corners[0].Index;

                    if (contour.Edges.Count >= 3)
                    {
                        int m = contour.Edges.Count;
                        for (int i = 0; i < m; ++i)
                            contour.Edges[(corner + i) % m].Color = colors[1 + SymmetricalTrichotomy(i, m)];
                    }
                    else if (contour.Edges.Count >= 1)
                    {
                        EdgeSegment[] parts = new EdgeSegment[7];
                        contour.Edges[0].SplitInThirds(out parts[0 + 3 * corner], out parts[1 + 3 * corner], out parts[2 + 3 * corner]);
                        if (contour.Edges.Count >= 2)
                        {
                            contour.Edges[1].SplitInThirds(out parts[3 - 3 * corner], out parts[4 - 3 * corner], out parts[5 - 3 * corner]);
                            parts[0].Color = parts[1].Color = colors[0];
                            parts[2].Color = parts[3].Color = colors[1];
                            parts[4].Color = parts[5].Color = colors[2];
                        }
                        else
                        {
                            parts[0].Color = colors[0];
                            parts[1].Color = colors[1];
                            parts[2].Color = colors[2];
                        }
                        contour.Edges.Clear();
                        for (int i = 0; i < 7; ++i)
                        {
                            if (parts[i] != null)
                                contour.Edges.Add(parts[i]);
                        }
                    }
                }
                else
                {
                    int cornerCount = corners.Count;
                    int majorCornerCount = cornerCount;

                    if (cornerCount > 3)
                    {
                         // Update first corner's prev length with last spline length
                         var first = corners[0];
                         first.PrevEdgeLengthEstimate += splineLength;
                         corners[0] = first;

                         for (int i = 0; i < cornerCount; ++i)
                         {
                             if (corners[i].PrevEdgeLengthEstimate > corners[(i + 1) % cornerCount].PrevEdgeLengthEstimate &&
                                 corners[(i + 1) % cornerCount].PrevEdgeLengthEstimate < corners[(i + 2) % cornerCount].PrevEdgeLengthEstimate)
                             {
                                 var c = corners[i];
                                 c.Minor = true;
                                 corners[i] = c;
                                 --majorCornerCount;
                             }
                         }
                    }

                    EdgeColor initialColor = EdgeColor.BLACK;
                    for (int i = 0; i < cornerCount; ++i)
                    {
                        if (!corners[i].Minor)
                        {
                            --majorCornerCount;
                            SwitchColor(ref color, ref seed, (EdgeColor)((majorCornerCount == 0 ? 0 : 1) * (int)initialColor)); // !majorCornerCount -> if 0 then 1 else 0. Wait. !0 is 1. !nonzero is 0.
                            // C++: !majorCornerCount. if count is 0, then 1. else 0.
                            var c = corners[i];
                            c.Color = color;
                            corners[i] = c;
                            if (initialColor == EdgeColor.BLACK)
                                initialColor = color;
                        }
                    }

                    for (int i = 0; i < cornerCount; ++i)
                    {
                        if (corners[i].Minor)
                        {
                            EdgeColor nextColor = corners[(i + 1) % cornerCount].Color;
                            var c = corners[i];
                            c.Color = (EdgeColor)(((int)color & (int)nextColor) ^ (int)EdgeColor.WHITE);
                            corners[i] = c;
                        }
                        else
                        {
                            color = corners[i].Color;
                        }
                    }

                    int spline = 0;
                    int start = corners[0].Index;
                    color = corners[0].Color;
                    int m = contour.Edges.Count;
                    for (int i = 0; i < m; ++i)
                    {
                        int index = (start + i) % m;
                        if (spline + 1 < cornerCount && corners[spline + 1].Index == index)
                        {
                            color = corners[++spline].Color;
                        }
                        contour.Edges[index].Color = color;
                    }
                }
            }
        }
    }
}
