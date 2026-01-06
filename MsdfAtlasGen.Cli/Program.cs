using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Msdfgen;
using Msdfgen.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MsdfAtlasGen.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: MsdfAtlasGen.Cli -font <filename.ttf> -type <msdf|sdf|psdf> [options]");
                 return;
            }

            // Configuration
            string fontPath = null;
            ImageType imageType = ImageType.MSDF;
            string imageFormat = "png";
            string outputJson = null;
            string outputCsv = null;
            string outputImage = "atlas.png";
            int width = -1;
            int height = -1;
            int size = 32;
            Msdfgen.Range pxRange = new Msdfgen.Range(2.0);
            double miterLimit = 1.0;
            double angleThreshold = 3.0; // Radians? C++ uses default 3.0
            bool kerning = true;
            string charsetPath = null;
            
            // Packer config
            int spacing = 0;
            double scale = -1;
            
            // Parsing
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "-font":
                        fontPath = args[++i];
                        break;
                    case "-type":
                        string typeStr = args[++i].ToLower();
                        if (typeStr == "msdf") imageType = ImageType.MSDF;
                        else if (typeStr == "sdf") imageType = ImageType.SDF;
                        else if (typeStr == "psdf") imageType = ImageType.PSDF;
                        else if (typeStr == "mtsdf") imageType = ImageType.MTSDF;
                        break;
                    case "-imageout":
                        outputImage = args[++i];
                        break;
                    case "-json":
                        outputJson = args[++i];
                        break;
                    case "-csv":
                        outputCsv = args[++i];
                        break;
                    case "-size":
                        size = int.Parse(args[++i]);
                        break;
                    case "-width":
                        width = int.Parse(args[++i]);
                        break;
                    case "-height":
                        height = int.Parse(args[++i]);
                        break;
                    case "-pxrange":
                        double r = double.Parse(args[++i]);
                        pxRange = new Msdfgen.Range(r);
                        break;
                    case "-range":
                         // range logic varies
                        break;
                    case "-scale":
                        scale = double.Parse(args[++i]);
                        break;
                    case "-spacing":
                         spacing = int.Parse(args[++i]);
                         break;
                    case "-charset":
                         charsetPath = args[++i];
                         break;
                }
            }
            
            if (string.IsNullOrEmpty(fontPath))
            {
                Console.Error.WriteLine("No font specified.");
                return;
            }

            // 1. Load Font
            var fonts = new List<FontGeometry>();
            var fontGeometry = new FontGeometry();
            
            // We need to load the actual Font object first
            Font font = null;
            try
            {
                font = FontLoader.Load(fontPath);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Failed to load font: {ex.Message}");
                return;
            }

            // 2. Load Charset
            Charset charset;
            if (string.IsNullOrEmpty(charsetPath))
                charset = Charset.ASCII;
            else
            {
                 // parsing charset
                 // simplistic for now (assuming file with chars)
                 // or ASCII
                 charset = Charset.ASCII; 
            }
            
            // 3. Load Glyphs
            // fontScale set to make emSize match something? 
            // C++ doesn't scale font at load time usually unless specific logic.
            // Using 1.0 default logic, font geometry scale will be computed based on Metrics.
            
            fontGeometry.LoadCharset(font, 1.0, charset);
            fontGeometry.SetName(Path.GetFileNameWithoutExtension(fontPath));
            
            // Edge Coloring
            foreach (var glyph in fontGeometry.GetGlyphs().Glyphs)
            {
                glyph.EdgeColoring(Msdfgen.Core.EdgeColoring.InkTrap, angleThreshold, 0);
            }
            
            fonts.Add(fontGeometry);

            // 4. Pack
            var glyphs = fontGeometry.GetGlyphs().Glyphs.ToArray(); // For single font, array is fine.
            
            var packer = new TightAtlasPacker();
            packer.SetDimensions(width, height);
            packer.SetScale(scale);
            packer.SetPixelRange(pxRange);
            packer.SetMiterLimit(miterLimit);
            packer.SetSpacing(spacing);
            // defaults
            
            // Packer sets the 'Box' in each glyph during packing
            int result = packer.Pack(glyphs);
            
            if (result != 0)
            {
                 Console.Error.WriteLine("Packing failed");
                 return;
            }
            
            packer.GetDimensions(out width, out height);
            
            Console.WriteLine($"Atlas Dimensions: {width}x{height}");
            
            // 5. Generate
            var generatorAttributes = new GeneratorAttributes();
            generatorAttributes.Config = new MSDFGeneratorConfig(true, ErrorCorrectionConfig.Default);
            generatorAttributes.ScanlinePass = true;

             // Choose generator based on type
             if (imageType == ImageType.MSDF)
             {
                 var generator = new ImmediateAtlasGenerator<float>(width, height, (bitmap, glyph, attrs) => 
                 {
                     var proj = glyph.GetBoxProjection();
                     var range = glyph.GetBoxRange();
                     MsdfGenerator.GenerateMSDF(bitmap, glyph.GetShape(), proj, range, attrs.Config);
                 }, 3);
                 
                 generator.SetAttributes(generatorAttributes);
                 generator.SetThreadCount(Environment.ProcessorCount);
                 generator.Generate(glyphs);
                 
                 // Save
                 // BitmapAtlasStorage<float> -> Image<Rgba32>
                 SaveAtlas(generator.AtlasStorage.Bitmap, outputImage);
                 
                 if (outputJson != null)
                 {
                     var metrics = new JsonAtlasMetrics 
                     { 
                         Width = width, Height = height, Size = size, // Size logic?
                         DistanceRange = pxRange, // Approx
                         YDirection = YAxisOrientation.Downward // MSDFGen default is bottom-up, but ImageSharp saves top-down. 
                         // We flip Y on saving.
                     };
                     JsonExporter.Export(fonts.ToArray(), imageType, metrics, outputJson, kerning);
                 }
                 
                 if (outputCsv != null)
                 {
                      CsvExporter.Export(fonts.ToArray(), width, height, YAxisOrientation.Downward, outputCsv);
                 }
             }
             else if (imageType == ImageType.SDF)
             {
                 // Similar for SDF with 1 channel
                 var generator = new ImmediateAtlasGenerator<float>(width, height, (bitmap, glyph, attrs) => 
                 {
                     var proj = glyph.GetBoxProjection();
                     var range = glyph.GetBoxRange();
                     // GeneratorConfig for SDF (not MSDFGeneratorConfig)
                     var sdfConfig = new GeneratorConfig(attrs.Config.OverlapSupport);
                     MsdfGenerator.GenerateSDF(bitmap, glyph.GetShape(), proj, range, sdfConfig);
                 }, 1);
                 
                  generator.SetAttributes(generatorAttributes);
                  generator.SetThreadCount(Environment.ProcessorCount);
                  generator.Generate(glyphs);
                  
                  SaveAtlas(generator.AtlasStorage.Bitmap, outputImage);
             }
             
             Console.WriteLine("Done.");
        }
        
        static void SaveAtlas(Bitmap<float> bitmap, string filename)
        {
            // Simple saver handling 1, 3, 4 channels
             using (var image = new Image<Rgba32>(bitmap.Width, bitmap.Height))
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        // Flip Y
                        int srcY = bitmap.Height - 1 - y; 
                        
                        // MsdfGen Bitmap is flat array
                        if (bitmap.Channels == 1)
                        {
                            float v = bitmap[x, srcY, 0];
                            byte b = Clamp(v * 255.0f);
                            image[x, y] = new Rgba32(b, b, b, 255);
                        }
                        else if (bitmap.Channels == 3)
                        {
                            byte r = Clamp(bitmap[x, srcY, 0] * 255.0f);
                            byte g = Clamp(bitmap[x, srcY, 1] * 255.0f);
                            byte b = Clamp(bitmap[x, srcY, 2] * 255.0f);
                            image[x, y] = new Rgba32(r, g, b, 255);
                         }
                        else if (bitmap.Channels == 4)
                        { // MTSDF
                            byte r = Clamp(bitmap[x, srcY, 0] * 255.0f);
                            byte g = Clamp(bitmap[x, srcY, 1] * 255.0f);
                            byte b = Clamp(bitmap[x, srcY, 2] * 255.0f);
                            byte a = Clamp(bitmap[x, srcY, 3] * 255.0f);
                            image[x, y] = new Rgba32(r, g, b, a);
                        }
                    }
                }
                image.Save(filename);
            }
        }
        
        static byte Clamp(float v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }
    }
}
