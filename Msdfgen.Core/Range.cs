namespace Msdfgen
{
    public struct Range
    {
        public double Lower;
        public double Upper;

        public Range(double symmetricalWidth = 0)
        {
            Lower = -0.5 * symmetricalWidth;
            Upper = 0.5 * symmetricalWidth;
        }

        public Range(double lowerBound, double upperBound)
        {
            Lower = lowerBound;
            Upper = upperBound;
        }

        public static Range operator *(Range range, double factor)
        {
            return new Range(range.Lower * factor, range.Upper * factor);
        }

        public static Range operator *(double factor, Range range)
        {
            return range * factor;
        }

        public static Range operator /(Range range, double divisor)
        {
            return new Range(range.Lower / divisor, range.Upper / divisor);
        }
    }
}
