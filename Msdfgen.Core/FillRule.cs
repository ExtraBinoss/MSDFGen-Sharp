namespace Msdfgen
{
    /// <summary>
    /// Specifies the rule used to determine the "inside" of a shape.
    /// </summary>
    public enum FillRule
    {
        /// <summary> Inside points have a non-zero winding number. </summary>
        NonZero,
        /// <summary> Inside points have an odd winding number (even-odd rule). </summary>
        Odd, 
        /// <summary> Inside points have a positive winding number. </summary>
        Positive,
        /// <summary> Inside points have a negative winding number. </summary>
        Negative
    }

    /// <summary>
    /// Extension methods for the <see cref="FillRule"/> enum.
    /// </summary>
    public static class FillRuleExtensions {
        /// <summary>
        /// Interprets the winding number according to the specified fill rule.
        /// </summary>
        /// <param name="fillRule">The fill rule to use.</param>
        /// <param name="intersections">The winding number (number of intersections).</param>
        /// <returns>True if the point is considered "inside" the shape.</returns>
        public static bool Interpret(this FillRule fillRule, int intersections) {
            switch (fillRule) {
                case FillRule.NonZero:
                    return intersections != 0;
                case FillRule.Odd:
                    return (intersections & 1) != 0;
                case FillRule.Positive:
                    return intersections > 0;
                case FillRule.Negative:
                    return intersections < 0;
                default:
                    return false;
            }
        }
    }
}
