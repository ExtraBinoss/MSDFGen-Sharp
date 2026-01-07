namespace Msdfgen
{
    /// <summary>
    /// Extends <see cref="Projection"/> to include distance mapping information for SDF generation.
    /// </summary>
    public class SDFTransformation : Projection
    {
        /// <summary>
        /// Gets the mapping used to transform distance values from shape space to bitmap space.
        /// </summary>
        public DistanceMapping DistanceMapping { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDFTransformation"/> class with default values.
        /// </summary>
        public SDFTransformation() : base()
        {
            DistanceMapping = new DistanceMapping();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDFTransformation"/> class using an existing projection and distance mapping.
        /// </summary>
        public SDFTransformation(Projection projection, DistanceMapping distanceMapping) : base(projection.Scale, projection.Translate)
        {
            DistanceMapping = distanceMapping;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SDFTransformation"/> class with explicit scale, translation, and distance mapping.
        /// </summary>
        public SDFTransformation(Vector2 scale, Vector2 translate, DistanceMapping distanceMapping) : base(scale, translate)
        {
            DistanceMapping = distanceMapping;
        }
    }
}
