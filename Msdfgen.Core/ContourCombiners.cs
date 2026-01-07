using System.Collections.Generic;
using static Msdfgen.Arithmetics;

namespace Msdfgen
{
    /// <summary>
    /// Helper methods for contour combiners to handle initial distances and distance resolution.
    /// </summary>
    public static class ContourCombinerHelpers
    {
        /// <summary>
        /// Returns the initial distance value for a scalar distance field.
        /// </summary>
        public static double GetInitialDistance(double example)
        {
            return double.MinValue;
        }

        /// <summary>
        /// Returns the initial distance value for a multi-channel distance field.
        /// </summary>
        public static MultiDistance GetInitialDistance(MultiDistance example)
        {
            return new MultiDistance { R = double.MinValue, G = double.MinValue, B = double.MinValue };
        }

        /// <summary>
        /// Returns the initial distance value for a multi-channel and true distance field.
        /// </summary>
        public static MultiAndTrueDistance GetInitialDistance(MultiAndTrueDistance example)
        {
             return new MultiAndTrueDistance { R = double.MinValue, G = double.MinValue, B = double.MinValue, A = double.MinValue };
        }

        /// <summary>
        /// Resolves a scalar distance.
        /// </summary>
        public static double ResolveDistance(double distance)
        {
            return distance;
        }

        /// <summary>
        /// Resolves a multi-channel distance to a single scalar value using the median.
        /// </summary>
        public static double ResolveDistance(MultiDistance distance)
        {
            return Arithmetics.Median(distance.R, distance.G, distance.B);
        }

        /// <summary>
        /// Resolves a multi-channel and true distance to a single scalar value using the median of the RGB channels.
        /// </summary>
        public static double ResolveDistance(MultiAndTrueDistance distance)
        {
             return Arithmetics.Median(distance.R, distance.G, distance.B);
        }
    }

    /// <summary>
    /// A simple contour combiner that calculates the distance to the nearest edge across all contours.
    /// </summary>
    /// <typeparam name="TSelector">The edge selector type to use.</typeparam>
    public class SimpleContourCombiner<TSelector> where TSelector : IEdgeSelector, new()
    {
        private TSelector shapeEdgeSelector;

        /// <summary>
        /// Initializes the combiner with a shape.
        /// </summary>
        public SimpleContourCombiner(Shape shape)
        {
            shapeEdgeSelector = new TSelector();
        }

        /// <summary>
        /// Resets the combiner for a new point.
        /// </summary>
        public void Reset(Vector2 p)
        {
            shapeEdgeSelector.Reset(p);
        }

        /// <summary>
        /// Returns the edge selector for a specific contour.
        /// </summary>
        public IEdgeSelector EdgeSelector(int i)
        {
            return shapeEdgeSelector;
        }

        /// <summary>
        /// Returns the final distance computed by the selector.
        /// </summary>
        public dynamic Distance()
        {
             return ((dynamic)shapeEdgeSelector).Distance();
        }
    }

    /// <summary>
    /// A contour combiner that correctly handles overlapping contours and holes using winding rules.
    /// </summary>
    /// <typeparam name="TSelector">The edge selector type to use.</typeparam>
    public class OverlappingContourCombiner<TSelector> where TSelector : IEdgeSelector, new()
    {
        private Vector2 p;
        private List<int> windings;
        private List<TSelector> edgeSelectors;

        /// <summary>
        /// Initializes the combiner with a shape and calculates initial winding numbers.
        /// </summary>
        public OverlappingContourCombiner(Shape shape)
        {
            windings = new List<int>(shape.Contours.Count);
            edgeSelectors = new List<TSelector>(shape.Contours.Count);
            foreach (var contour in shape.Contours)
            {
                windings.Add(contour.Winding());
                edgeSelectors.Add(new TSelector());
            }
        }

        /// <summary>
        /// Resets the combiner for a new point.
        /// </summary>
        public void Reset(Vector2 p)
        {
            this.p = p;
            foreach (var selector in edgeSelectors)
                selector.Reset(p);
        }

        /// <summary>
        /// Returns the edge selector for a specific contour.
        /// </summary>
        public TSelector EdgeSelector(int i)
        {
            return edgeSelectors[i];
        }

        /// <summary>
        /// Returns the final combined distance, taking into account overlapping contours and their winding orders.
        /// </summary>
        public dynamic Distance()
        {
            int contourCount = edgeSelectors.Count;
            TSelector shapeEdgeSelector = new TSelector();
            TSelector innerEdgeSelector = new TSelector();
            TSelector outerEdgeSelector = new TSelector();
            
            shapeEdgeSelector.Reset(p);
            innerEdgeSelector.Reset(p);
            outerEdgeSelector.Reset(p);

            for (int i = 0; i < contourCount; ++i)
            {
                dynamic edgeDistance = ((dynamic)edgeSelectors[i]!).Distance();
                ((dynamic)shapeEdgeSelector!).Merge(edgeSelectors[i]);
                if (windings[i] > 0 && ContourCombinerHelpers.ResolveDistance(edgeDistance) >= 0)
                    ((dynamic)innerEdgeSelector!).Merge(edgeSelectors[i]);
                if (windings[i] < 0 && ContourCombinerHelpers.ResolveDistance(edgeDistance) <= 0)
                    ((dynamic)outerEdgeSelector!).Merge(edgeSelectors[i]);
            }

            dynamic shapeDistance = ((dynamic)shapeEdgeSelector).Distance();
            dynamic innerDistance = ((dynamic)innerEdgeSelector).Distance();
            dynamic outerDistance = ((dynamic)outerEdgeSelector).Distance();
            double innerScalarDistance = ContourCombinerHelpers.ResolveDistance(innerDistance);
            double outerScalarDistance = ContourCombinerHelpers.ResolveDistance(outerDistance);
            dynamic distance = ContourCombinerHelpers.GetInitialDistance(shapeDistance);

            int winding = 0;
            if (innerScalarDistance >= 0 && Math.Abs(innerScalarDistance) <= Math.Abs(outerScalarDistance))
            {
                distance = innerDistance;
                winding = 1;
                for (int i = 0; i < contourCount; ++i)
                    if (windings[i] > 0)
                    {
                        dynamic contourDistance = ((dynamic)edgeSelectors[i]).Distance();
                        if (Math.Abs(ContourCombinerHelpers.ResolveDistance(contourDistance)) < Math.Abs(outerScalarDistance) && ContourCombinerHelpers.ResolveDistance(contourDistance) > ContourCombinerHelpers.ResolveDistance(distance))
                            distance = contourDistance;
                    }
            }
            else if (outerScalarDistance <= 0 && Math.Abs(outerScalarDistance) < Math.Abs(innerScalarDistance))
            {
                distance = outerDistance;
                winding = -1;
                for (int i = 0; i < contourCount; ++i)
                    if (windings[i] < 0)
                    {
                        dynamic contourDistance = ((dynamic)edgeSelectors[i]).Distance();
                        if (Math.Abs(ContourCombinerHelpers.ResolveDistance(contourDistance)) < Math.Abs(innerScalarDistance) && ContourCombinerHelpers.ResolveDistance(contourDistance) < ContourCombinerHelpers.ResolveDistance(distance))
                            distance = contourDistance;
                    }
            }
            else
                return shapeDistance;

            for (int i = 0; i < contourCount; ++i)
                if (windings[i] != winding)
                {
                    dynamic contourDistance = ((dynamic)edgeSelectors[i]).Distance();
                    if (ContourCombinerHelpers.ResolveDistance(contourDistance) * ContourCombinerHelpers.ResolveDistance(distance) >= 0 && Math.Abs(ContourCombinerHelpers.ResolveDistance(contourDistance)) < Math.Abs(ContourCombinerHelpers.ResolveDistance(distance)))
                        distance = contourDistance;
                }
            
            if (ContourCombinerHelpers.ResolveDistance(distance) == ContourCombinerHelpers.ResolveDistance(shapeDistance))
                distance = shapeDistance;
            
            return distance;
        }
    }
}
