namespace Msdfgen
{
    public enum FillRule
    {
        NonZero,
        Odd, // "even-odd"
        Positive,
        Negative
    }

    public static class FillRuleExtensions {
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
