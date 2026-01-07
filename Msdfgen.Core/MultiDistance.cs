namespace Msdfgen
{
    /// <summary>
    /// Represents a distance field value with three channels (typically Red, Green, Blue).
    /// </summary>
    public struct MultiDistance
    {
        /// <summary> The distance values for the three channels. </summary>
        public double R, G, B;
    }

    /// <summary>
    /// Represents a distance field value with four channels (typically Red, Green, Blue, and a fourth 'true' distance channel).
    /// </summary>
    public struct MultiAndTrueDistance
    {
        /// <summary> The distance values for the four channels. </summary>
        public double R, G, B, A;
    }
}
