namespace Msdfgen
{
    /// <summary>
    /// Specifies which color channels (Red, Green, Blue) an edge segment belongs to in a multi-channel distance field.
    /// Bitwise values are used: RED=1, GREEN=2, BLUE=4.
    /// </summary>
    public enum EdgeColor
    {
        /// <summary> No color channels (000). </summary>
        BLACK = 0,
        /// <summary> Red channel only (001). </summary>
        RED = 1,
        /// <summary> Green channel only (010). </summary>
        GREEN = 2,
        /// <summary> Red and Green (Yellow) channels (011). </summary>
        YELLOW = 3,
        /// <summary> Blue channel only (100). </summary>
        BLUE = 4,
        /// <summary> Red and Blue (Magenta) channels (101). </summary>
        MAGENTA = 5,
        /// <summary> Green and Blue (Cyan) channels (110). </summary>
        CYAN = 6,
        /// <summary> All color channels (111). </summary>
        WHITE = 7
    }
}
