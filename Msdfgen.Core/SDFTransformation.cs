namespace Msdfgen
{
    public class SDFTransformation : Projection
    {
        public DistanceMapping DistanceMapping { get; }

        public SDFTransformation() : base()
        {
            DistanceMapping = new DistanceMapping();
        }

        public SDFTransformation(Projection projection, DistanceMapping distanceMapping) : base(projection.Scale, projection.Translate)
        {
            DistanceMapping = distanceMapping;
        }

        public SDFTransformation(Vector2 scale, Vector2 translate, DistanceMapping distanceMapping) : base(scale, translate)
        {
            DistanceMapping = distanceMapping;
        }
    }
}
