using System.Collections.Generic;
using static Msdfgen.Arithmetics;

namespace Msdfgen
{
    public static class ContourCombinerHelpers
    {
        public static double GetInitialDistance(double example)
        {
            return double.MinValue;
        }

        public static MultiDistance GetInitialDistance(MultiDistance example)
        {
            return new MultiDistance { R = double.MinValue, G = double.MinValue, B = double.MinValue };
        }

        public static MultiAndTrueDistance GetInitialDistance(MultiAndTrueDistance example)
        {
             return new MultiAndTrueDistance { R = double.MinValue, G = double.MinValue, B = double.MinValue, A = double.MinValue };
        }

        public static double ResolveDistance(double distance)
        {
            return distance;
        }

        public static double ResolveDistance(MultiDistance distance)
        {
            return Arithmetics.Median(distance.R, distance.G, distance.B);
        }

        public static double ResolveDistance(MultiAndTrueDistance distance)
        {
             return Arithmetics.Median(distance.R, distance.G, distance.B);
        }
    }

    public class SimpleContourCombiner<TSelector> where TSelector : IEdgeSelector, new()
    {
        private TSelector shapeEdgeSelector;

        public SimpleContourCombiner(Shape shape)
        {
            shapeEdgeSelector = new TSelector();
        }

        public void Reset(Vector2 p)
        {
            shapeEdgeSelector.Reset(p);
        }

        public IEdgeSelector EdgeSelector(int i)
        {
            return shapeEdgeSelector;
        }

        public dynamic Distance()
        {
            // Assuming TSelector types have specific Distance methods not in IEdgeSelector
            // We still need dynamic or a generic interface for Distance?
            // "Distance" return type varies. 
            // So keep dynamic for Distance call, but safe for others.
             return ((dynamic)shapeEdgeSelector).Distance();
        }
    }

    public class OverlappingContourCombiner<TSelector> where TSelector : IEdgeSelector, new()
    {
        private Vector2 p;
        private List<int> windings;
        private List<TSelector> edgeSelectors;

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

        public void Reset(Vector2 p)
        {
            this.p = p;
            foreach (var selector in edgeSelectors)
                selector.Reset(p);
        }

        public TSelector EdgeSelector(int i)
        {
            return edgeSelectors[i];
        }

        public dynamic Distance()
        {
            int contourCount = edgeSelectors.Count;
            // TSelector is constraint to new() so we can create instances
            TSelector shapeEdgeSelector = new TSelector();
            TSelector innerEdgeSelector = new TSelector();
            TSelector outerEdgeSelector = new TSelector();
            
            shapeEdgeSelector.Reset(p);
            innerEdgeSelector.Reset(p);
            outerEdgeSelector.Reset(p);

            for (int i = 0; i < contourCount; ++i)
            {
                dynamic edgeDistance = ((dynamic)edgeSelectors[i]!).Distance();
                ((dynamic)shapeEdgeSelector!).Merge(edgeSelectors[i]); // TSelector must have Merge(TSelector)
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
