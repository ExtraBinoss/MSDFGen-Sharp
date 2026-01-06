using System;
using Msdfgen;

namespace MsdfAtlasGen.Cli
{
    /// <summary>
    /// Configuration parsed from command-line arguments.
    /// Mirrors the msdf-atlas-gen CLI options.
    /// </summary>
    public class CliConfig
    {
        // Input
        public string FontPath { get; set; } = string.Empty;
        public string CharsetPath { get; set; } = string.Empty;
        public string GlyphsetPath { get; set; } = string.Empty;
        public string InlineChars { get; set; } = string.Empty;
        public bool AllGlyphs { get; set; } = false;
        public double FontScale { get; set; } = 1.0;
        public string FontName { get; set; } = string.Empty;

        // Atlas type
        public ImageType Type { get; set; } = ImageType.Msdf;

        // Atlas format
        public ImageFormat Format { get; set; } = ImageFormat.Png;

        // Atlas dimensions
        public int Width { get; set; } = -1;
        public int Height { get; set; } = -1;
        public DimensionsConstraint DimensionsConstraint { get; set; } = DimensionsConstraint.MultipleOfFourSquare;

        // Outputs
        public string ImageOut { get; set; } = string.Empty;
        public bool ImageOutRequested { get; set; } = false;
        public string JsonOut { get; set; } = string.Empty;
        public bool JsonOutRequested { get; set; } = false;
        public string CsvOut { get; set; } = string.Empty;
        public bool CsvOutRequested { get; set; } = false;
        public string FntOut { get; set; } = string.Empty;
        public bool GenerateFnt { get; set; } = false;

        // Glyph configuration
        public double Size { get; set; } = 32.0;
        public double MinSize { get; set; } = -1;
        public Msdfgen.Range PxRange { get; set; } = new Msdfgen.Range(2.0);
        public double EmRange { get; set; } = -1;

        // Distance field settings
        public double AngleThreshold { get; set; } = 3.0;
        public string ColoringStrategy { get; set; } = "inktrap";
        public double MiterLimit { get; set; } = 1.0;
        public bool Overlap { get; set; } = false;
        public bool NoPreprocess { get; set; } = false;
        public bool Scanline { get; set; } = true;
        public ulong Seed { get; set; } = 0;
        public int Threads { get; set; } = 0; // 0 = auto
        public YAxisOrientation YOrigin { get; set; } = YAxisOrientation.Upward;

        // Packing
        public int Spacing { get; set; } = 0;
        public double Scale { get; set; } = -1;

        // Kerning
        public bool Kerning { get; set; } = true;

        // Test Render
        public bool TestRender { get; set; } = false;
        public string TestRenderFile { get; set; } = string.Empty;
        public int TestRenderWidth { get; set; } = 512;
        public int TestRenderHeight { get; set; } = 512;

        public bool IsValid => !string.IsNullOrEmpty(FontPath);
    }
}
