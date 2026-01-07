namespace Msdfgen
{
    public struct Range
    {
        public double Lower;
        public double Upper;

        /// <summary>
        /// Initializes a symmetrical range around zero.
        /// </summary>
        public Range(double symmetricalWidth = 0)
        {
            Lower = -0.5 * symmetricalWidth;
            Upper = 0.5 * symmetricalWidth;
        }

        /// <summary>
        /// Initializes a range with specific lower and upper bounds.
        /// </summary>
        public Range(double lowerBound, double upperBound)
        {
            Lower = lowerBound;
            Upper = upperBound;
        }

        /// <summary>
        /// Scales the range by a factor.
        /// </summary>
        public static Range operator *(Range range, double factor)
        {
            return new Range(range.Lower * factor, range.Upper * factor);
        }

        /// <summary>
        /// Scales the range by a factor.
        /// </summary>
        public static Range operator *(double factor, Range range)
        {
            return range * factor;
        }

        /// <summary>
        /// Divides the range by a divisor.
        /// </summary>
        public static Range operator /(Range range, double divisor)
        {
            return new Range(range.Lower / divisor, range.Upper / divisor);
        }

        /// <summary>
        /// Offsets the range by a value.
        /// </summary>
        public static Range operator +(Range range, double value)
        {
            return new Range(range.Lower + value, range.Upper + value);
        }

        /// <summary>
        /// Offsets the range by a value.
        /// </summary>
        public static Range operator +(double value, Range range)
        {
            return range + value;
        }

        /// <summary>
        /// Adds two ranges component-wise.
        /// </summary>
        public static Range operator +(Range a, Range b)
        {
            return new Range(a.Lower + b.Lower, a.Upper + b.Upper);
        }
    }
}
