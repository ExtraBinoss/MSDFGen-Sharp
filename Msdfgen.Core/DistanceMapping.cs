namespace Msdfgen
{
    /// <summary>
    /// Represents a mapping for distance values, typically used to transform raw signed distances into a normalized [0, 1] range.
    /// </summary>
    public class DistanceMapping
    {
        /// <summary>
        /// Represents a change in distance, which is mapped differently than an absolute distance value.
        /// </summary>
        public struct Delta
        {
            /// <summary> The delta value. </summary>
            public double Value;
            /// <summary> Initializes a new <see cref="Delta"/>. </summary>
            public Delta(double value) { Value = value; }
            /// <summary> Implicitly converts a <see cref="Delta"/> to a double. </summary>
            public static implicit operator double(Delta d) => d.Value;
        }

        private double scale;
        private double translate;

        /// <summary>
        /// Initializes a default distance mapping (identity).
        /// </summary>
        public DistanceMapping()
        {
            scale = 1;
            translate = 0;
        }

        /// <summary>
        /// Initializes a distance mapping from a specified distance range.
        /// </summary>
        public DistanceMapping(Range range)
        {
            scale = 1 / (range.Upper - range.Lower);
            translate = -range.Lower;
        }

        private DistanceMapping(double scale, double translate)
        {
            this.scale = scale;
            this.translate = translate;
        }

        /// <summary>
        /// Creates a distance mapping that is the inverse of the mapping defined by the given range.
        /// </summary>
        public static DistanceMapping Inverse(Range range)
        {
            double rangeWidth = range.Upper - range.Lower;
            return new DistanceMapping(rangeWidth, range.Lower / (rangeWidth != 0 ? rangeWidth : 1));
        }

        /// <summary>
        /// Maps an absolute distance value.
        /// </summary>
        public double Map(double d)
        {
            return scale * (d + translate);
        }

        /// <summary>
        /// Maps a distance delta value.
        /// </summary>
        public double Map(Delta d)
        {
            return scale * d.Value;
        }

        /// <summary>
        /// Returns the inverse of this distance mapping.
        /// </summary>
        public DistanceMapping Inverse()
        {
            return new DistanceMapping(1 / scale, -scale * translate);
        }
    }
}
