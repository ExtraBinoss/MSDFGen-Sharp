using System;
using System.Linq;

namespace MsdfAtlasGen
{
    public static class RectanglePacking
    {
        private static void CopyRectanglePlacement(ref Rectangle dst, Rectangle src)
        {
            dst.X = src.X;
            dst.Y = src.Y;
        }

        // Overload for when we want to pass a specific ISizeSelector implementation logic
        /// <summary>
        /// Packs rectangles into an area of resolved size based on constraints.
        /// </summary>
        public static void PackRectangles<TSelector>(Rectangle[] rectangles, int spacing, out int width, out int height) where TSelector : ISizeSelector
        {   
            long totalArea = 0;
            var rectanglesCopy = new Rectangle[rectangles.Length];
            for (int i = 0; i < rectangles.Length; ++i)
            {
                rectanglesCopy[i] = rectangles[i]; // struct copy
                rectanglesCopy[i].W += spacing;
                rectanglesCopy[i].H += spacing;
                totalArea += (long)rectangles[i].W * rectangles[i].H;
            }
            ISizeSelector sizeSelector = (ISizeSelector)Activator.CreateInstance(typeof(TSelector), (int)totalArea)!;
            PackRectangles(rectangles, rectanglesCopy, sizeSelector, spacing, out width, out height);
        }

        public static void PackRectangles(Rectangle[] rectangles, Rectangle[] rectanglesCopy, ISizeSelector sizeSelector, int spacing, out int width, out int height)
        {
            width = 0;
            height = 0;
            
            int curW, curH;
            while (sizeSelector.GetDimensions(out curW, out curH))
            {
                var packer = new RectanglePacker(curW + spacing, curH + spacing);
                
                if (packer.Pack(rectanglesCopy) == 0)
                {
                    width = curW;
                    height = curH;
                    
                    for (int i = 0; i < rectangles.Length; ++i)
                    {
                        rectangles[i].X = rectanglesCopy[i].X;
                        rectangles[i].Y = rectanglesCopy[i].Y;
                    }
                    sizeSelector.Previous();
                }
                else
                {
                    sizeSelector.Next();
                }
            }
        }

        // Helper for fixed size packing
        /// <summary>
        /// Packs rectangles into a fixed-size area. Returns non-zero if some didn't fit.
        /// </summary>
        public static int PackRectangles(Rectangle[] rectangles, int width, int height, int spacing)
        {
             if (spacing != 0)
            {
                for (int i = 0; i < rectangles.Length; ++i)
                {
                    rectangles[i].W += spacing;
                    rectangles[i].H += spacing;
                }
            }
            
            var packer = new RectanglePacker(width + spacing, height + spacing);
            int result = packer.Pack(rectangles);
            
            if (spacing != 0)
            {
                for (int i = 0; i < rectangles.Length; ++i)
                {
                    rectangles[i].W -= spacing;
                    rectangles[i].H -= spacing;
                }
            }
            return result;
        }
    }
}
