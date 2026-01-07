using System.Collections.Generic;

namespace Msdfgen
{
    /// <summary>
    /// Component that finds the distance of a point from a shape.
    /// </summary>
    /// <typeparam name="TContourCombiner">The combiner used to merge contour distances.</typeparam>
    public class ShapeDistanceFinder<TContourCombiner>
        where TContourCombiner : class
    {
        private Shape shape;
        private TContourCombiner contourCombiner;
        private List<dynamic> shapeEdgeCache;

        /// <summary>
        /// Initializes the distance finder with a shape and a contour combiner.
        /// </summary>
        public ShapeDistanceFinder(Shape shape, TContourCombiner contourCombiner)
        {
            this.shape = shape;
            this.contourCombiner = contourCombiner;
            this.shapeEdgeCache = new List<dynamic>();
            
            for (int i = 0; i < shape.Contours.Count; ++i)
            {
                Contour contour = shape.Contours[i];
                IEdgeSelector edgeSelector = (IEdgeSelector)((dynamic)contourCombiner).EdgeSelector(i);
                for (int j = 0; j < contour.Edges.Count; ++j)
                {
                    shapeEdgeCache.Add(edgeSelector.CreateEdgeCache());
                }
            }
        }
        
        /// <summary>
        /// Computes the distance of the shape from the specified origin.
        /// </summary>
        public dynamic Distance(Vector2 origin)
        {
            ((dynamic)contourCombiner).Reset(origin);
            int edgeIndex = 0;
            for (int i = 0; i < shape.Contours.Count; ++i)
            {
                Contour contour = shape.Contours[i];
                if (contour.Edges.Count == 0) continue;

                IEdgeSelector edgeSelector = (IEdgeSelector)((dynamic)contourCombiner).EdgeSelector(i);
                EdgeSegment prevEdge = contour.Edges[contour.Edges.Count - 1];
                
                for (int j = 0; j < contour.Edges.Count; ++j)
                {
                    EdgeSegment edge = contour.Edges[j];
                    EdgeSegment nextEdge = contour.Edges[(j + 1) % contour.Edges.Count];
                    
                    object cache = shapeEdgeCache[edgeIndex++];
                    edgeSelector.AddEdge(cache, prevEdge, edge, nextEdge);
                    
                    prevEdge = edge;
                }
            }
            return ((dynamic)contourCombiner).Distance();
        }
    }
}
