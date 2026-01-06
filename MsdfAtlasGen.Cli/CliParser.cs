using System;
using Msdfgen;

namespace MsdfAtlasGen.Cli
{
    /// <summary>
    /// Parses command-line arguments into a CliConfig object.
    /// </summary>
    public static class CliParser
    {
        public static CliConfig Parse(string[] args)
        {
            var config = new CliConfig();

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLower();
                switch (arg)
                {
                    // Input
                    case "-font":
                        config.FontPath = args[++i];
                        break;
                    case "-charset":
                        config.CharsetPath = args[++i];
                        break;
                    case "-glyphset":
                        config.GlyphsetPath = args[++i];
                        break;
                    case "-chars":
                    case "-glyphs":
                        config.InlineChars = args[++i];
                        break;
                    case "-allglyphs":
                        config.AllGlyphs = true;
                        break;
                    case "-fontscale":
                        config.FontScale = double.Parse(args[++i]);
                        break;
                    case "-fontname":
                        config.FontName = args[++i];
                        break;

                    // Atlas type
                    case "-type":
                        config.Type = ParseImageType(args[++i]);
                        break;

                    // Atlas format
                    case "-format":
                        config.Format = ParseImageFormat(args[++i]);
                        break;

                    // Atlas dimensions
                    case "-dimensions":
                        config.Width = int.Parse(args[++i]);
                        config.Height = int.Parse(args[++i]);
                        break;
                    case "-pots":
                        config.DimensionsConstraint = DimensionsConstraint.PowerOfTwoSquare;
                        break;
                    case "-potr":
                        config.DimensionsConstraint = DimensionsConstraint.PowerOfTwoRectangle;
                        break;
                    case "-square":
                        config.DimensionsConstraint = DimensionsConstraint.Square;
                        break;
                    case "-square2":
                        config.DimensionsConstraint = DimensionsConstraint.EvenSquare;
                        break;
                    case "-square4":
                        config.DimensionsConstraint = DimensionsConstraint.MultipleOfFourSquare;
                        break;

                    // Outputs
                    case "-imageout":
                        config.ImageOutRequested = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            config.ImageOut = args[++i];
                        }
                        break;
                    case "-json":
                        config.JsonOutRequested = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            config.JsonOut = args[++i];
                        }
                        break;
                    case "-csv":
                        config.CsvOutRequested = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            config.CsvOut = args[++i];
                        }
                        break;
                    case "-fnt":
                        config.GenerateFnt = true;
                        // Check if next arg is a path (not starting with -)
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            config.FntOut = args[++i];
                        }
                        break;

                    // Glyph configuration
                    case "-size":
                        config.Size = double.Parse(args[++i]);
                        break;
                    case "-minsize":
                        config.MinSize = double.Parse(args[++i]);
                        break;
                    case "-pxrange":
                        config.PxRange = new Msdfgen.Range(double.Parse(args[++i]));
                        break;
                    case "-emrange":
                        config.EmRange = double.Parse(args[++i]);
                        break;

                    // Distance field settings
                    case "-angle":
                        config.AngleThreshold = ParseAngle(args[++i]);
                        break;
                    case "-coloringstrategy":
                        config.ColoringStrategy = args[++i].ToLower();
                        break;
                    case "-miterlimit":
                        config.MiterLimit = double.Parse(args[++i]);
                        break;
                    case "-overlap":
                        config.Overlap = true;
                        break;
                    case "-nopreprocess":
                        config.NoPreprocess = true;
                        break;
                    case "-scanline":
                        config.Scanline = true;
                        break;
                    case "-seed":
                        config.Seed = ulong.Parse(args[++i]);
                        break;
                    case "-threads":
                        config.Threads = int.Parse(args[++i]);
                        break;
                    case "-yorigin":
                        config.YOrigin = args[++i].ToLower() == "top" ? YAxisOrientation.Downward : YAxisOrientation.Upward;
                        break;

                    // Packing
                    case "-spacing":
                        config.Spacing = int.Parse(args[++i]);
                        break;
                    case "-scale":
                        config.Scale = double.Parse(args[++i]);
                        break;

                    case "-help":
                    case "--help":
                    case "-h":
                        PrintHelp();
                        Environment.Exit(0);
                        break;

                    // Test Render
                    case "-testrender":
                        config.TestRender = true;
                        // Check if next arg is a path (not starting with -)
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            config.TestRenderFile = args[++i];
                        }
                        break;
                    case "-testrendersize":
                        config.TestRenderWidth = int.Parse(args[++i]);
                        config.TestRenderHeight = int.Parse(args[++i]);
                        break;
                }
            }

            return config;
        }

        private static ImageType ParseImageType(string s)
        {
            return s.ToLower() switch
            {
                "hardmask" => ImageType.HardMask,
                "softmask" => ImageType.SoftMask,
                "sdf" => ImageType.Sdf,
                "psdf" => ImageType.Psdf,
                "msdf" => ImageType.Msdf,
                "mtsdf" => ImageType.Mtsdf,
                _ => ImageType.Msdf
            };
        }

        private static ImageFormat ParseImageFormat(string s)
        {
            return s.ToLower() switch
            {
                "png" => ImageFormat.Png,
                "bmp" => ImageFormat.Bmp,
                "tiff" => ImageFormat.Tiff,
                "rgba" => ImageFormat.Rgba,
                "fl32" => ImageFormat.Fl32,
                "text" => ImageFormat.Text,
                "textfloat" => ImageFormat.TextFloat,
                "bin" => ImageFormat.Binary,
                "binfloat" => ImageFormat.BinaryFloat,
                "binfloatbe" => ImageFormat.BinaryFloatBe,
                _ => ImageFormat.Png
            };
        }

        private static double ParseAngle(string s)
        {
            if (s.EndsWith("D", StringComparison.OrdinalIgnoreCase) || s.EndsWith("d"))
            {
                // Degrees
                double deg = double.Parse(s.TrimEnd('D', 'd'));
                return deg * Math.PI / 180.0;
            }
            return double.Parse(s);
        }

        public static void PrintHelp()
        {
            Console.WriteLine(@"
MsdfAtlasGen.Cli - Generate MSDF font atlases

Usage: MsdfAtlasGen.Cli -font <fontfile.ttf> [options]

Input:
  -font <fontfile.ttf>          Sets the input font file (required)
  -charset <charset.txt>        Sets the character set file
  -chars <chars>                Sets characters inline
  -allglyphs                    Use all glyphs in font
  -fontscale <scale>            Applies scaling to glyphs
  -fontname <name>              Sets font name in metadata

Atlas Type:
  -type <type>                  hardmask|softmask|sdf|psdf|msdf|mtsdf (default: msdf)

Atlas Dimensions:
  -dimensions <width> <height>  Sets fixed atlas dimensions
  -pots                         Power-of-two square
  -potr                         Power-of-two rectangle
  -square                       Any square
  -square2                      Even square
  -square4                      Square divisible by 4 (default)

Outputs (saved to MsdfAtlasGen.Cli/output/):
  -imageout <filename>          Output atlas image -> Fnt/ folder
  -json <filename.json>         Output JSON metadata -> Json/ folder
  -csv <filename.csv>           Output CSV layout -> Json/ folder
  -fnt [filename.fnt]           Output BMFont FNT format -> Fnt/ folder (with PNG)

  If no output options are specified, defaults to <fontname>.json in Json/ folder.
  Using -fnt creates both .fnt and .png in the Fnt/ folder.

Test Render:
  -testrender [filename]        Render a test image -> Renders/ folder
  -testrendersize <w> <h>       Test render dimensions (default: 512x512)

Glyph Configuration:
  -size <em size>               Glyph size in pixels per em (default: 32)
  -pxrange <range>              Distance field range in pixels (default: 2)
  -emrange <range>              Distance field range in em

Generator Settings:
  -angle <angle>                Corner angle threshold (append D for degrees)
  -coloringstrategy <strategy>  simple|inktrap|distance (default: inktrap)
  -miterlimit <value>           Miter limit for bounding boxes
  -overlap                      Enable overlap support
  -scanline                     Enable scanline pass
  -threads <N>                  Thread count (0 = auto)
  -yorigin <bottom|top>         Y-axis direction (default: bottom)

-help                           Show this help
");
        }
    }
}
