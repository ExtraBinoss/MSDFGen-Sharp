using System;
using System.Collections.Generic;
using System.Linq;

namespace MsdfAtlasGen
{
    /// <summary>
    /// Guillotine 2D single bin packer
    /// </summary>
    public class RectanglePacker
    {
        private const int WorstFit = int.MaxValue;
        private readonly List<Rectangle> _spaces;

        /// <summary>
        /// Initializes a new rectangle packer with an empty area.
        /// </summary>
        public RectanglePacker() : this(0, 0)
        {
        }

        /// <summary>
        /// Initializes a new rectangle packer with a specified initial area.
        /// </summary>
        public RectanglePacker(int width, int height)
        {
            _spaces = new List<Rectangle>();
            if (width > 0 && height > 0)
                _spaces.Add(new Rectangle { X = 0, Y = 0, W = width, H = height });
        }

        /// <summary>
        /// Expands the packing area - both width and height must be greater or equal to the previous value
        /// </summary>
        public void Expand(int width, int height)
        {
            if (width > 0 && height > 0)
            {
                int oldWidth = 0, oldHeight = 0;
                foreach (var space in _spaces)
                {
                    if (space.X + space.W > oldWidth)
                        oldWidth = space.X + space.W;
                    if (space.Y + space.H > oldHeight)
                        oldHeight = space.Y + space.H;
                }
                _spaces.Add(new Rectangle { X = 0, Y = 0, W = width, H = height });
                SplitSpace(_spaces.Count - 1, oldWidth, oldHeight);
            }
        }

        /// <summary>
        /// Packs the rectangle array, returns how many didn't fit (0 on success)
        /// </summary>
        public int Pack(Rectangle[] rectangles)
        {
            // 1. Create indices and sort them by area/size descending (Best Fit Decreasing)
            // This transforms the algorithm from O(N^3) (Global Best Fit) to O(N^2) (Best Fit Decreasing).
            // N=10000 -> 10^8 ops instead of 10^12.
            var indices = new int[rectangles.Length];
            for (int i = 0; i < rectangles.Length; i++) indices[i] = i;

            Array.Sort(indices, (a, b) => 
            {
                var rA = rectangles[a];
                var rB = rectangles[b];
                int areaA = rA.W * rA.H;
                int areaB = rB.W * rB.H;
                if (areaA != areaB) return areaB.CompareTo(areaA);
                return rB.H.CompareTo(rA.H); // Secondary sort by height
            });

            int failedCount = 0;

            // 2. Iterate through sorted rectangles and find best space for each
            foreach (int rectIndex in indices)
            {
                ref Rectangle rect = ref rectangles[rectIndex]; // ref to modify X/Y directly
                
                int bestFit = WorstFit;
                int bestSpaceIndex = -1;

                // Scan all spaces to find the best fit for this specific rectangle
                for (int i = 0; i < _spaces.Count; ++i)
                {
                    Rectangle space = _spaces[i];
                    
                    // Exact match check
                    if (rect.W == space.W && rect.H == space.H)
                    {
                        bestSpaceIndex = i;
                        break; // Perfect fit, stop searching
                    }
                    
                    if (rect.W <= space.W && rect.H <= space.H)
                    {
                        int fit = RateFit(rect.W, rect.H, space.W, space.H);
                        if (fit < bestFit)
                        {
                            bestSpaceIndex = i;
                            bestFit = fit;
                        }
                    }
                }

                if (bestSpaceIndex != -1)
                {
                    // Place the rectangle
                    rect.X = _spaces[bestSpaceIndex].X;
                    rect.Y = _spaces[bestSpaceIndex].Y;
                    SplitSpace(bestSpaceIndex, rect.W, rect.H);
                }
                else
                {
                    failedCount++;
                }
            }
            
            return failedCount;
        }

        /// <summary>
        /// Packs an array of oriented rectangles, allowing for 90-degree rotations.
        /// </summary>
        public int Pack(OrientedRectangle[] rectangles)
        {
            var remainingRects = new List<int>(rectangles.Length);
            for (int i = 0; i < rectangles.Length; ++i)
                remainingRects.Add(i);

            while (remainingRects.Count > 0)
            {
                int bestFit = WorstFit;
                int bestSpace = -1;
                int bestRect = -1;
                bool bestRotated = false;

                for (int i = 0; i < _spaces.Count; ++i)
                {
                    Rectangle space = _spaces[i];
                    for (int j = 0; j < remainingRects.Count; ++j)
                    {
                        OrientedRectangle rect = rectangles[remainingRects[j]];
                        if (rect.W == space.W && rect.H == space.H)
                        {
                            bestSpace = i;
                            bestRect = j;
                            bestRotated = false;
                            goto BestFitFound;
                        }
                        if (rect.H == space.W && rect.W == space.H)
                        {
                            bestSpace = i;
                            bestRect = j;
                            bestRotated = true;
                            goto BestFitFound;
                        }
                        if (rect.W <= space.W && rect.H <= space.H)
                        {
                            int fit = RateFit(rect.W, rect.H, space.W, space.H);
                            if (fit < bestFit)
                            {
                                bestSpace = i;
                                bestRect = j;
                                bestRotated = false;
                                bestFit = fit;
                            }
                        }
                        if (rect.H <= space.W && rect.W <= space.H)
                        {
                            int fit = RateFit(rect.H, rect.W, space.W, space.H);
                            if (fit < bestFit)
                            {
                                bestSpace = i;
                                bestRect = j;
                                bestRotated = true;
                                bestFit = fit;
                            }
                        }
                    }
                }
                if (bestSpace < 0 || bestRect < 0)
                    break;

                BestFitFound:
                int rectIndex = remainingRects[bestRect];
                rectangles[rectIndex].X = _spaces[bestSpace].X;
                rectangles[rectIndex].Y = _spaces[bestSpace].Y;
                rectangles[rectIndex].Rotated = bestRotated;
                if (bestRotated)
                    SplitSpace(bestSpace, rectangles[rectIndex].H, rectangles[rectIndex].W);
                else
                    SplitSpace(bestSpace, rectangles[rectIndex].W, rectangles[rectIndex].H);
                RemoveFromUnorderedVector(remainingRects, bestRect);
            }
            return remainingRects.Count;
        }

        private static int RateFit(int w, int h, int sw, int sh)
        {
            return (sw * sh) - (w * h);
        }

        private void SplitSpace(int index, int w, int h)
        {
            Rectangle space = _spaces[index];
            RemoveFromUnorderedVector(_spaces, index);
            
            Rectangle a = new Rectangle { X = space.X, Y = space.Y + h, W = w, H = space.H - h };
            Rectangle b = new Rectangle { X = space.X + w, Y = space.Y, W = space.W - w, H = h };

            if (w * (space.H - h) < h * (space.W - w))
                a.W = space.W;
            else
                b.H = space.H;

            if (a.W > 0 && a.H > 0)
                _spaces.Add(a);
            if (b.W > 0 && b.H > 0)
                _spaces.Add(b);
        }

        private static void RemoveFromUnorderedVector<T>(List<T> list, int index)
        {
            if (index != list.Count - 1)
            {
                list[index] = list[list.Count - 1];
            }
            list.RemoveAt(list.Count - 1);
        }
    }
}
