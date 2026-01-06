using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Msdfgen;
using SixLabors.Fonts;

namespace MsdfAtlasGen.Cli
{
    /// <summary>
    /// Core atlas generation logic.
    /// </summary>
    public class AtlasGeneratorRunner
    {
        private readonly CliConfig _config;
        private const string OutputFolder = "output";

        public AtlasGeneratorRunner(CliConfig config)
        {
            _config = config;
        }

        public int Run()
        {
            // 1. Load Font
            Font font;
            try
            {
                var fontCollection = new FontCollection();
                var fontFamily = fontCollection.Add(_config.FontPath);
                font = fontFamily.CreateFont((float)_config.Size);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load font: {ex.Message}");
                return 1;
            }

            // 2. Prepare output paths
            string imageOut = ResolveOutputPath(_config.ImageOut, "atlas.png");
            string? jsonOut = string.IsNullOrEmpty(_config.JsonOut) ? null : ResolveOutputPath(_config.JsonOut, null);
            string? csvOut = string.IsNullOrEmpty(_config.CsvOut) ? null : ResolveOutputPath(_config.CsvOut, null);

            // 3. Load Charset
            Charset charset = LoadCharset();

            // 4. Build FontGeometry
            var fonts = new List<FontGeometry>();
            var fontGeometry = new FontGeometry();

            string fontName = !string.IsNullOrEmpty(_config.FontName)
                ? _config.FontName
                : Path.GetFileNameWithoutExtension(_config.FontPath);

            fontGeometry.LoadCharset(font, _config.FontScale, charset);
            fontGeometry.SetName(fontName);

            // 5. Edge Coloring
            foreach (var glyph in fontGeometry.GetGlyphs().Glyphs)
            {
                glyph.EdgeColoring(GetColoringFunction(), _config.AngleThreshold, _config.Seed);
            }

            fonts.Add(fontGeometry);

            // 6. Pack
            var glyphs = fontGeometry.GetGlyphs().Glyphs.ToArray();

            var packer = new TightAtlasPacker();
            packer.SetDimensionsConstraint(_config.DimensionsConstraint);
            
            if (_config.Width > 0 && _config.Height > 0)
            {
                packer.SetDimensions(_config.Width, _config.Height);
            }
            
            if (_config.Scale > 0)
            {
                packer.SetScale(_config.Scale);
            }
            
            packer.SetPixelRange(_config.PxRange);
            packer.SetMiterLimit(_config.MiterLimit);
            packer.SetSpacing(_config.Spacing);
            
            if (_config.MinSize > 0)
            {
                packer.SetMinimumScale(_config.MinSize / _config.Size);
            }

            int result = packer.Pack(glyphs);
            if (result != 0)
            {
                Console.Error.WriteLine("Packing failed");
                return 1;
            }

            packer.GetDimensions(out int width, out int height);
            Console.WriteLine($"Atlas Dimensions: {width}x{height}");

            // 7. Generate
            var generatorAttributes = new GeneratorAttributes
            {
                Config = new MSDFGeneratorConfig(true, ErrorCorrectionConfig.Default),
                ScanlinePass = _config.Scanline
            };

            int threadCount = _config.Threads > 0 ? _config.Threads : Environment.ProcessorCount;
            int channels = GetChannelCount(_config.Type);

            var generator = new ImmediateAtlasGenerator<float>(width, height, (bitmap, glyph, attrs) =>
            {
                var proj = glyph.GetBoxProjection();
                var range = glyph.GetBoxRange();

                switch (_config.Type)
                {
                    case ImageType.Sdf:
                        var sdfConfig = new GeneratorConfig(attrs.Config.OverlapSupport);
                        MsdfGenerator.GenerateSDF(bitmap, glyph.GetShape(), proj, range, sdfConfig);
                        break;
                    case ImageType.Psdf:
                        var psdfTransform = new SDFTransformation(proj, new DistanceMapping(range));
                        var psdfConfig = new GeneratorConfig(attrs.Config.OverlapSupport);
                        MsdfGenerator.GeneratePSDF(bitmap, glyph.GetShape(), psdfTransform, psdfConfig);
                        break;
                    case ImageType.Msdf:
                        MsdfGenerator.GenerateMSDF(bitmap, glyph.GetShape(), proj, range, attrs.Config);
                        break;
                    case ImageType.Mtsdf:
                        var mtsdfTransform = new SDFTransformation(proj, new DistanceMapping(range));
                        MsdfGenerator.GenerateMTSDF(bitmap, glyph.GetShape(), mtsdfTransform, attrs.Config);
                        break;
                }
            }, channels);

            generator.SetAttributes(generatorAttributes);
            generator.SetThreadCount(threadCount);
            generator.Generate(glyphs);

            // 8. Save outputs
            Console.WriteLine($"Saving image to: {imageOut}");
            AtlasSaver.SaveAtlas(generator.AtlasStorage.Bitmap, imageOut);

            if (jsonOut != null)
            {
                Console.WriteLine($"Saving JSON to: {jsonOut}");
                var metrics = new JsonAtlasMetrics
                {
                    Width = width,
                    Height = height,
                    Size = _config.Size,
                    DistanceRange = _config.PxRange,
                    YDirection = _config.YOrigin
                };
                JsonExporter.Export(fonts.ToArray(), _config.Type, metrics, jsonOut, _config.Kerning);
            }

            if (csvOut != null)
            {
                Console.WriteLine($"Saving CSV to: {csvOut}");
                CsvExporter.Export(fonts.ToArray(), width, height, _config.YOrigin, csvOut);
            }

            Console.WriteLine("Done.");
            return 0;
        }

        private string ResolveOutputPath(string path, string? defaultName)
        {
            if (string.IsNullOrEmpty(path) && defaultName != null)
            {
                path = defaultName;
            }

            // If path is just a filename (no directory), put it in output folder
            if (!Path.IsPathRooted(path) && !path.Contains(Path.DirectorySeparatorChar) && !path.Contains('/'))
            {
                path = Path.Combine(OutputFolder, path);
            }

            // Ensure the directory exists
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return path;
        }

        private Charset LoadCharset()
        {
            if (!string.IsNullOrEmpty(_config.InlineChars))
            {
                var charset = new Charset();
                foreach (char c in _config.InlineChars)
                {
                    charset.Add(c);
                }
                return charset;
            }

            if (!string.IsNullOrEmpty(_config.CharsetPath) && File.Exists(_config.CharsetPath))
            {
                // Simple charset parser - just read characters from file
                var charset = new Charset();
                string content = File.ReadAllText(_config.CharsetPath);
                foreach (char c in content)
                {
                    if (!char.IsControl(c))
                        charset.Add(c);
                }
                return charset;
            }

            return Charset.ASCII;
        }

        private EdgeColoringDelegate GetColoringFunction()
        {
            return _config.ColoringStrategy.ToLower() switch
            {
                "simple" => EdgeColoring.EdgeColoringSimple,
                "inktrap" => EdgeColoring.EdgeColoringInkTrap,
                _ => EdgeColoring.EdgeColoringInkTrap
            };
        }

        private static int GetChannelCount(ImageType type)
        {
            return type switch
            {
                ImageType.HardMask => 1,
                ImageType.SoftMask => 1,
                ImageType.Sdf => 1,
                ImageType.Psdf => 1,
                ImageType.Msdf => 3,
                ImageType.Mtsdf => 4,
                _ => 3
            };
        }
    }
}
