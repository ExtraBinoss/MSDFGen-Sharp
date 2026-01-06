namespace Msdfgen
{
    public class DistanceMapping
    {
        public struct Delta
        {
            public double Value;
            public Delta(double value) { Value = value; }
            public static implicit operator double(Delta d) => d.Value;
        }

        private double scale;
        private double translate;

        public DistanceMapping()
        {
            scale = 1;
            translate = 0;
        }

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

        public static DistanceMapping Inverse(Range range)
        {
            double rangeWidth = range.Upper - range.Lower;
            return new DistanceMapping(rangeWidth, range.Lower / (rangeWidth != 0 ? rangeWidth : 1));
        }

        public double Map(double d)
        {
            return scale * (d + translate);
        }

        public double Map(Delta d)
        {
            return scale * d.Value;
        }

        public DistanceMapping Inverse()
        {
            return new DistanceMapping(1 / scale, -scale * translate);
        }
    }
}
