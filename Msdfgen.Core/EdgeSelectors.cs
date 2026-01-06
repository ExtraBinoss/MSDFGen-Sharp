using System;

namespace Msdfgen
{
    public interface IEdgeSelector
    {
        object CreateEdgeCache();
        void Reset(Vector2 p);
        void AddEdge(object cache, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge);
    }

    public class TrueDistanceSelector : IEdgeSelector
    {
        public class EdgeCache
        {
            public Vector2 Point;
            public double AbsDistance;
        }

        public object CreateEdgeCache() => new EdgeCache();

        private const double DISTANCE_DELTA_FACTOR = 1.001;
        private Vector2 p;
        private SignedDistance minDistance;

        public TrueDistanceSelector()
        {
            minDistance = SignedDistance.Infinite;
        }

        public void Reset(Vector2 p)
        {
            double delta = DISTANCE_DELTA_FACTOR * (p - this.p).Length();
            minDistance.Distance += Arithmetics.NonZeroSign(minDistance.Distance) * delta;
            this.p = p;
        }

        public void AddEdge(object cacheObj, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge)
        {
            var cache = (EdgeCache)cacheObj;
            double delta = DISTANCE_DELTA_FACTOR * (p - cache.Point).Length();
            if (cache.AbsDistance - delta <= Math.Abs(minDistance.Distance))
            {
                double dummy;
                SignedDistance distance = edge.SignedDistance(p, out dummy);
                if (distance < minDistance)
                    minDistance = distance;
                cache.Point = p;
                cache.AbsDistance = Math.Abs(distance.Distance);
            }
        }

        public void Merge(TrueDistanceSelector other)
        {
            if (other.minDistance < minDistance)
                minDistance = other.minDistance;
        }

        public double Distance()
        {
            return minDistance.Distance;
        }
    }

    public class PerpendicularDistanceSelectorBase
    {
        public class EdgeCache
        {
             public Vector2 Point;
             public double AbsDistance;
             public double ADomainDistance;
             public double BDomainDistance;
             public double APerpendicularDistance;
             public double BPerpendicularDistance;
        }

        protected const double DISTANCE_DELTA_FACTOR = 1.001;

        public static bool GetPerpendicularDistance(ref double distance, Vector2 ep, Vector2 edgeDir)
        {
            // ... (unchanged logic)
            double ts = Vector2.DotProduct(ep, edgeDir);
            if (ts > 0)
            {
                double perpendicularDistance = Vector2.CrossProduct(ep, edgeDir);
                if (Math.Abs(perpendicularDistance) < Math.Abs(distance))
                {
                    distance = perpendicularDistance;
                    return true;
                }
            }
            return false;
        }
        
        protected SignedDistance minTrueDistance;
        protected double minNegativePerpendicularDistance;
        protected double minPositivePerpendicularDistance;
        protected EdgeSegment? nearEdge;
        protected double nearEdgeParam;

        public PerpendicularDistanceSelectorBase()
        {
            minTrueDistance = SignedDistance.Infinite;
            minNegativePerpendicularDistance = -Math.Abs(minTrueDistance.Distance);
            minPositivePerpendicularDistance = Math.Abs(minTrueDistance.Distance);
            nearEdge = null;
        }

        public void Reset(double delta)
        {
            minTrueDistance.Distance += Arithmetics.NonZeroSign(minTrueDistance.Distance) * delta;
            minNegativePerpendicularDistance = -Math.Abs(minTrueDistance.Distance);
            minPositivePerpendicularDistance = Math.Abs(minTrueDistance.Distance);
            nearEdge = null;
            nearEdgeParam = 0;
        }

        public bool IsEdgeRelevant(EdgeCache cache, EdgeSegment edge, Vector2 p)
        {
            double delta = DISTANCE_DELTA_FACTOR * (p - cache.Point).Length();
            return (
                cache.AbsDistance - delta <= Math.Abs(minTrueDistance.Distance) ||
                Math.Abs(cache.ADomainDistance) < delta ||
                Math.Abs(cache.BDomainDistance) < delta ||
                (cache.ADomainDistance > 0 && (cache.APerpendicularDistance < 0 ?
                    cache.APerpendicularDistance + delta >= minNegativePerpendicularDistance :
                    cache.APerpendicularDistance - delta <= minPositivePerpendicularDistance
                )) ||
                (cache.BDomainDistance > 0 && (cache.BPerpendicularDistance < 0 ?
                    cache.BPerpendicularDistance + delta >= minNegativePerpendicularDistance :
                    cache.BPerpendicularDistance - delta <= minPositivePerpendicularDistance
                ))
            );
        }

        public void AddEdgeTrueDistance(EdgeSegment edge, SignedDistance distance, double param)
        {
            if (distance < minTrueDistance)
            {
                minTrueDistance = distance;
                nearEdge = edge;
                nearEdgeParam = param;
            }
        }

        public void AddEdgePerpendicularDistance(double distance)
        {
            if (distance <= 0 && distance > minNegativePerpendicularDistance)
                minNegativePerpendicularDistance = distance;
            if (distance >= 0 && distance < minPositivePerpendicularDistance)
                minPositivePerpendicularDistance = distance;
        }

        public void Merge(PerpendicularDistanceSelectorBase other)
        {
            if (other.minTrueDistance < minTrueDistance)
            {
                minTrueDistance = other.minTrueDistance;
                nearEdge = other.nearEdge;
                nearEdgeParam = other.nearEdgeParam;
            }
            if (other.minNegativePerpendicularDistance > minNegativePerpendicularDistance)
                minNegativePerpendicularDistance = other.minNegativePerpendicularDistance;
            if (other.minPositivePerpendicularDistance < minPositivePerpendicularDistance)
                minPositivePerpendicularDistance = other.minPositivePerpendicularDistance;
        }

        public double ComputeDistance(Vector2 p)
        {
            double minDistance = minTrueDistance.Distance < 0 ? minNegativePerpendicularDistance : minPositivePerpendicularDistance;
            if (nearEdge != null)
            {
                SignedDistance distance = minTrueDistance; // Copy
                nearEdge.DistanceToPerpendicularDistance(ref distance, p, nearEdgeParam);
                if (Math.Abs(distance.Distance) < Math.Abs(minDistance))
                    minDistance = distance.Distance;
            }
            return minDistance;
        }

        public SignedDistance TrueDistance()
        {
            return minTrueDistance;
        }
    }
    
    public class PerpendicularDistanceSelector : PerpendicularDistanceSelectorBase, IEdgeSelector
    {
        private Vector2 p;
        
        public object CreateEdgeCache() => new EdgeCache();

        public void Reset(Vector2 p)
        {
            double delta = DISTANCE_DELTA_FACTOR * (p - this.p).Length();
            base.Reset(delta);
            this.p = p;
        }

        public void AddEdge(object cacheObj, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge)
        {
            var cache = (EdgeCache)cacheObj;
            if (IsEdgeRelevant(cache, edge, p))
            {
                double param;
                SignedDistance distance = edge.SignedDistance(p, out param);
                AddEdgeTrueDistance(edge, distance, param);
                cache.Point = p;
                cache.AbsDistance = Math.Abs(distance.Distance);

                Vector2 ap = p - edge.Point(0);
                Vector2 bp = p - edge.Point(1);
                Vector2 aDir = edge.Direction(0).Normalize(true);
                Vector2 bDir = edge.Direction(1).Normalize(true);
                Vector2 prevDir = prevEdge.Direction(1).Normalize(true);
                Vector2 nextDir = nextEdge.Direction(0).Normalize(true);
                double add = Vector2.DotProduct(ap, (prevDir + aDir).Normalize(true));
                double bdd = -Vector2.DotProduct(bp, (bDir + nextDir).Normalize(true));
                if (add > 0)
                {
                    double pd = distance.Distance;
                    if (GetPerpendicularDistance(ref pd, ap, -aDir))
                        AddEdgePerpendicularDistance(pd = -pd);
                    cache.APerpendicularDistance = pd;
                }
                if (bdd > 0)
                {
                    double pd = distance.Distance;
                    if (GetPerpendicularDistance(ref pd, bp, bDir))
                        AddEdgePerpendicularDistance(pd);
                    cache.BPerpendicularDistance = pd;
                }
                cache.ADomainDistance = add;
                cache.BDomainDistance = bdd;
            }
        }
        
        public double Distance()
        {
            return ComputeDistance(p);
        }
    }

    public class MultiDistanceSelector : IEdgeSelector
    {
        public bool hasTrueDistance() => false;
        
        public object CreateEdgeCache() => new PerpendicularDistanceSelectorBase.EdgeCache();

        private Vector2 p;
        private PerpendicularDistanceSelectorBase r = new PerpendicularDistanceSelectorBase();
        private PerpendicularDistanceSelectorBase g = new PerpendicularDistanceSelectorBase();
        private PerpendicularDistanceSelectorBase b = new PerpendicularDistanceSelectorBase();
        private const double DISTANCE_DELTA_FACTOR = 1.001;

        public void Reset(Vector2 p)
        {
            double delta = DISTANCE_DELTA_FACTOR * (p - this.p).Length();
            r.Reset(delta);
            g.Reset(delta);
            b.Reset(delta);
            this.p = p;
        }

        public void AddEdge(object cacheObj, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge)
        {
            var cache = (PerpendicularDistanceSelectorBase.EdgeCache)cacheObj;
            if (
                ((edge.Color & EdgeColor.RED) != 0 && r.IsEdgeRelevant(cache, edge, p)) ||
                ((edge.Color & EdgeColor.GREEN) != 0 && g.IsEdgeRelevant(cache, edge, p)) ||
                ((edge.Color & EdgeColor.BLUE) != 0 && b.IsEdgeRelevant(cache, edge, p))
            )
            {
                double param;
                SignedDistance distance = edge.SignedDistance(p, out param);
                if ((edge.Color & EdgeColor.RED) != 0)
                    r.AddEdgeTrueDistance(edge, distance, param);
                if ((edge.Color & EdgeColor.GREEN) != 0)
                    g.AddEdgeTrueDistance(edge, distance, param);
                if ((edge.Color & EdgeColor.BLUE) != 0)
                    b.AddEdgeTrueDistance(edge, distance, param);
                cache.Point = p;
                cache.AbsDistance = Math.Abs(distance.Distance);

                Vector2 ap = p - edge.Point(0);
                Vector2 bp = p - edge.Point(1);
                Vector2 aDir = edge.Direction(0).Normalize(true);
                Vector2 bDir = edge.Direction(1).Normalize(true);
                Vector2 prevDir = prevEdge.Direction(1).Normalize(true);
                Vector2 nextDir = nextEdge.Direction(0).Normalize(true);
                double add = Vector2.DotProduct(ap, (prevDir + aDir).Normalize(true));
                double bdd = -Vector2.DotProduct(bp, (bDir + nextDir).Normalize(true));
                if (add > 0)
                {
                    double pd = distance.Distance;
                    if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, ap, -aDir))
                    {
                         pd = -pd;
                         if ((edge.Color & EdgeColor.RED) != 0) r.AddEdgePerpendicularDistance(pd);
                         if ((edge.Color & EdgeColor.GREEN) != 0) g.AddEdgePerpendicularDistance(pd);
                         if ((edge.Color & EdgeColor.BLUE) != 0) b.AddEdgePerpendicularDistance(pd);
                    }
                    cache.APerpendicularDistance = pd;
                }
                if (bdd > 0)
                {
                    double pd = distance.Distance;
                    if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, bp, bDir))
                    {
                         if ((edge.Color & EdgeColor.RED) != 0) r.AddEdgePerpendicularDistance(pd);
                         if ((edge.Color & EdgeColor.GREEN) != 0) g.AddEdgePerpendicularDistance(pd);
                         if ((edge.Color & EdgeColor.BLUE) != 0) b.AddEdgePerpendicularDistance(pd);
                    }
                    cache.BPerpendicularDistance = pd;
                }
                cache.ADomainDistance = add;
                cache.BDomainDistance = bdd;
            }
        }

        public void Merge(MultiDistanceSelector other)
        {
            r.Merge(other.r);
            g.Merge(other.g);
            b.Merge(other.b);
        }

        public MultiDistance Distance()
        {
            return new MultiDistance
            {
                R = r.ComputeDistance(p),
                G = g.ComputeDistance(p),
                B = b.ComputeDistance(p)
            };
        }

        public SignedDistance TrueDistance()
        {
            SignedDistance distance = r.TrueDistance();
            if (g.TrueDistance() < distance)
                distance = g.TrueDistance();
            if (b.TrueDistance() < distance)
                distance = b.TrueDistance();
            return distance;
        }
    }

    public class MultiAndTrueDistanceSelector : MultiDistanceSelector
    {
        public new MultiAndTrueDistance Distance()
        {
            MultiDistance multiDistance = base.Distance();
            return new MultiAndTrueDistance
            {
                R = multiDistance.R,
                G = multiDistance.G,
                B = multiDistance.B,
                A = TrueDistance().Distance
            };
        }
    }
}
