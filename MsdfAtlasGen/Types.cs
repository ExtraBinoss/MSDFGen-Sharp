using System;

namespace MsdfAtlasGen
{
    public delegate void EdgeColoringDelegate(Msdfgen.Shape shape, double angleThreshold, ulong seed);

    public class Types
    {
        // Aliasing types if needed, but C# has its own conventions.
    }

    /// <summary>
    /// Type of atlas image contents
    /// </summary>
    public enum ImageType
    {
        /// <summary>
        /// Rendered glyphs without anti-aliasing (two colors only)
        /// </summary>
        HardMask,
        /// <summary>
        /// Rendered glyphs with anti-aliasing
        /// </summary>
        SoftMask,
        /// <summary>
        /// Signed (true) distance field
        /// </summary>
        Sdf,
        /// <summary>
        /// Signed perpendicular distance field
        /// </summary>
        Psdf,
        /// <summary>
        /// Multi-channel signed distance field
        /// </summary>
        Msdf,
        /// <summary>
        /// Multi-channel & true signed distance field
        /// </summary>
        Mtsdf
    }

    /// <summary>
    /// Atlas image encoding
    /// </summary>
    public enum ImageFormat
    {
        Unspecified,
        Png,
        Bmp,
        Tiff,
        Rgba,
        Fl32,
        Text,
        TextFloat,
        Binary,
        BinaryFloat,
        BinaryFloatBe
    }

    /// <summary>
    /// Glyph identification
    /// </summary>
    public enum GlyphIdentifierType
    {
        GlyphIndex,
        UnicodeCodepoint
    }

    /// <summary>
    /// The method of computing the layout of the atlas
    /// </summary>
    public enum PackingStyle
    {
        Tight,
        Grid
    }

    /// <summary>
    /// Constraints for the atlas's dimensions - see size selectors for more info
    /// </summary>
    public enum DimensionsConstraint
    {
        None,
        Square,
        EvenSquare,
        MultipleOfFourSquare,
        PowerOfTwoRectangle,
        PowerOfTwoSquare
    }

    /// <summary>
    /// Carries progress information for atlas generation.
    /// </summary>
    public struct GeneratorProgress(double proportion, string glyphName, int current, int total)
    {
        public double Proportion = proportion;
        public string GlyphName = glyphName;
        public int Current = current;
        public int Total = total;
    }
}
