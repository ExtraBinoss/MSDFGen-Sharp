using System.Collections.Generic;

namespace Msdfgen
{
    public class ShapeDistanceFinder<TContourCombiner>
        where TContourCombiner : class
    {
        private Shape shape;
        private TContourCombiner contourCombiner;
        
        // We need to cache edge selectors' internal state (EdgeCache).
        // In C++, shapeEdgeCache is vector<EdgeCache>.
        // EdgeCache type depends on TSelector.
        // TSelector depends on TContourCombiner.
        // I can assume TContourCombiner provides access to selector type via reflection or dynamic?
        // Or I can store 'object' caches.
        
        private List<dynamic> shapeEdgeCache;

        public ShapeDistanceFinder(Shape shape, TContourCombiner contourCombiner)
        {
            this.shape = shape;
            this.contourCombiner = contourCombiner;
            this.shapeEdgeCache = new List<dynamic>();
            
            for (int i = 0; i < shape.Contours.Count; ++i)
            {
                Contour contour = shape.Contours[i];
                // Although contourCombiner might be generic, we expect EdgeSelector to return an IEdgeSelector
                IEdgeSelector edgeSelector = (IEdgeSelector)((dynamic)contourCombiner).EdgeSelector(i);
                for (int j = 0; j < contour.Edges.Count; ++j)
                {
                    shapeEdgeCache.Add(edgeSelector.CreateEdgeCache());
                }
            }
        }
        
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
                    
                    // Use the persistent cache object
                    object cache = shapeEdgeCache[edgeIndex++];
                    edgeSelector.AddEdge(cache, prevEdge, edge, nextEdge);
                    
                    prevEdge = edge;
                }
            }
            return ((dynamic)contourCombiner).Distance();
        }
    }
}
