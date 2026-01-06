using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Msdfgen;
using Msdfgen.Extensions;

namespace MsdfAtlasGen.Cli
{
    /// <summary>
    /// Core atlas generation logic.
    /// </summary>
    public class AtlasGeneratorRunner
    {
        private readonly CliConfig _config;
        
        // Output folder structure inside MsdfAtlasGen.Cli
        private static string BaseOutputFolder
        {
            get
            {
                // Get the directory where the CLI executable is located
                string? exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(exeDir))
                    exeDir = Directory.GetCurrentDirectory();
                
                // Go up to find MsdfAtlasGen.Cli source directory if running from bin
                if (exeDir.Contains("bin"))
                {
                    var current = new DirectoryInfo(exeDir);
                    while (current != null && current.Name != "MsdfAtlasGen.Cli")
                    {
                        current = current.Parent;
                    }
                    if (current != null)
                        return Path.Combine(current.FullName, "output");
                }
                
                // Fallback: use MsdfAtlasGen.Cli/output relative to current working directory
                return Path.Combine("MsdfAtlasGen.Cli", "output");
            }
        }

        // Subdirectories
        private static string JsonFolder => Path.Combine(BaseOutputFolder, "Json");
        private static string FntFolder => Path.Combine(BaseOutputFolder, "Fnt");
        private static string RenderFolder => Path.Combine(BaseOutputFolder, "Renders");
        private static string GlyphDumpFolder => Path.Combine(BaseOutputFolder, "GlyphDump");

        public AtlasGeneratorRunner(CliConfig config)
        {
            _config = config;
        }

        public int Run()
        {
            PrintConfiguration();

            // 1. Load font via FreeType
            Console.WriteLine("[PHASE] Loading FreeType and Font...");
            using var ft = FreetypeHandle.Initialize();
            if (ft == null)
            {
                Console.Error.WriteLine("Failed to initialize FreeType library.");
                return 1;
            }

            using var fontHandle = FontHandle.LoadFont(ft, _config.FontPath);
            if (fontHandle == null)
            {
                Console.Error.WriteLine("Failed to load font via FreeType.");
                return 1;
            }

            // Get font name for default filenames
            string fontName = !string.IsNullOrEmpty(_config.FontName)
                ? _config.FontName
                : Path.GetFileNameWithoutExtension(_config.FontPath);

            // 2. Prepare output paths
            // FNT folder contains both .fnt and .png (they go together)
            // If ImageOut is not explicitly requested, but FNT is, we just use the name from FNT? 
            // Actually, if ImageOut is NOT empty, use it. If it is empty:
            // - If ImageOutRequested=true, default name.
            // - If FntRequested, default name in FNT folder?
            // - If AutoGenerate, default name.
            
            bool autoGenerateOutputs = !_config.ImageOutRequested && 
                                       !_config.JsonOutRequested && 
                                       !_config.CsvOutRequested &&
                                       !_config.GenerateFnt;

            string? imageOut = null;
            if (_config.ImageOutRequested)
            {
                imageOut = ResolveOutputPath(_config.ImageOut, $"{fontName}.png", _config.GenerateFnt ? FntFolder : FntFolder);
            }
            else if (_config.GenerateFnt)
            {
                 // FNT requires an image. If not explicitly requested, generate in FNT folder with default name.
                 imageOut = ResolveOutputPath("", $"{fontName}.png", FntFolder);
            }
            else if (autoGenerateOutputs)
            {
                 // Default behavior (as per requirement: "if I omit ... it just creates those")
                 // The requirement specifically said "if I omit -imageout and -json".
                 // So we treat auto-generate as generating image + json.
                 imageOut = ResolveOutputPath("", $"{fontName}.png", FntFolder); // Or JsonFolder? Default usually PNG somewhere. Let's stick to FntFolder or root?
                 // The previous implementation used FntFolder for PNG.
            }
            // If still null (e.g. only -json requested), we assume we DO need an image for atlas generation?
            // Actually, MSDF gen ALWAYS creates an image. We probably should save it.
            // But if user ONLY requested JSON? Maybe they want the metrics only? 
            // The msdf-atlas-gen typically generates image.
            // Let's ensure imageOut is set if we are running generator.
            if (imageOut == null) 
            {
                 imageOut = ResolveOutputPath("", $"{fontName}.png", FntFolder);
            }


            string? jsonOut = null;
            if (_config.JsonOutRequested)
            {
                jsonOut = ResolveOutputPath(_config.JsonOut, $"{fontName}.json", JsonFolder);
            }
            else if (autoGenerateOutputs)
            {
                jsonOut = ResolveOutputPath("", $"{fontName}.json", JsonFolder);
            }
            
            string? csvOut = null;
            if (_config.CsvOutRequested)
            {
                csvOut = ResolveOutputPath(_config.CsvOut, $"{fontName}.csv", JsonFolder);
            }

            string? fntOut = null;
            if (_config.GenerateFnt)
            {
                fntOut = string.IsNullOrEmpty(_config.FntOut)
                    ? ResolveOutputPath("", $"{fontName}.fnt", FntFolder)
                    : ResolveOutputPath(_config.FntOut, null, FntFolder);
            }

            // 3. Load Charset
            Console.WriteLine("[PHASE] Loading Charset and Metrics...");
            Charset charset = LoadCharset();

            // 4. Build FontGeometry
            var fonts = new List<FontGeometry>();
            var fontGeometry = new FontGeometry();

            // FontScale is an additional scale multiplier; Size is the primary font size
            // Pass Size * FontScale as the geometry scale
            fontGeometry.LoadCharset(fontHandle, _config.Size * _config.FontScale, charset);
            fontGeometry.SetName(fontName);

            // 5. Edge Coloring
            foreach (var glyph in fontGeometry.GetGlyphs().Glyphs)
            {
                glyph.EdgeColoring(GetColoringFunction(), _config.AngleThreshold, _config.Seed);
            }

            fonts.Add(fontGeometry);

            // 6. Pack
            Console.WriteLine("[PHASE] Packing Glyphs...");
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
            
            // Configure padding
            packer.SetInnerPixelPadding(_config.InnerPxPadding);
            packer.SetOuterPixelPadding(_config.OuterPxPadding);
            packer.SetInnerUnitPadding(_config.InnerEmPadding);
            packer.SetOuterUnitPadding(_config.OuterEmPadding);
            
            if (_config.MinSize > 0)
            {
                packer.SetMinimumScale(_config.MinSize / _config.Size);
            }

            int result = packer.Pack(glyphs);
            if (result < 0)
            {
                Console.Error.WriteLine("Packing failed");
                return 1;
            }

            // 7. Generate MSDF atlas
            int width, height;
            packer.GetDimensions(out width, out height);
            Console.WriteLine($"  Atlas dimensions: {width} x {height}");

            // 7. Generate MSDF atlas
            Console.WriteLine("[PHASE] Generating Atlas Pixels...");
            var generatorAttributes = new GeneratorAttributes
            {
                Config = new MSDFGeneratorConfig(_config.Overlap, GetErrorCorrectionConfig()),
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
                        MsdfGenerator.GenerateSDF(bitmap, glyph.GetShape()!, proj, range, sdfConfig);
                        break;
                    case ImageType.Psdf:
                        var psdfTransform = new SDFTransformation(proj, new DistanceMapping(range));
                        var psdfConfig = new GeneratorConfig(attrs.Config.OverlapSupport);
                        MsdfGenerator.GeneratePSDF(bitmap, glyph.GetShape()!, psdfTransform, psdfConfig);
                        break;
                    case ImageType.Msdf:
                        MsdfGenerator.GenerateMSDF(bitmap, glyph.GetShape()!, proj, range, attrs.Config);
                        break;
                    case ImageType.Mtsdf:
                        var mtsdfTransform = new SDFTransformation(proj, new DistanceMapping(range));
                        MsdfGenerator.GenerateMTSDF(bitmap, glyph.GetShape()!, mtsdfTransform, attrs.Config);
                        break;
                }
            }, channels);

            generator.SetAttributes(generatorAttributes);
            generator.SetThreadCount(threadCount);

            Console.WriteLine($"Generation: {threadCount} threads used");
            var progress = new Progress<GeneratorProgress>(p =>
            {
                string progressText = $"\rGenerating Atlas: {p.Proportion * 100:F0}% ({p.Current}/{p.Total}) [Glyph: {p.GlyphName}]";
                Console.Write(progressText.PadRight(Console.WindowWidth - 1));
            });

            generator.Generate(glyphs, progress);
            Console.WriteLine(); // Newline after completion

            // 8. Save outputs
            Console.WriteLine("[PHASE] Saving Outputs...");
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

            // Debug: dump a single glyph if requested
            if (!string.IsNullOrEmpty(_config.DebugGlyph))
            {
                Directory.CreateDirectory(GlyphDumpFolder);
                DumpRequestedGlyph(fontGeometry, _config.DebugGlyph);
            }

            if (csvOut != null)
            {
                Console.WriteLine($"Saving CSV to: {csvOut}");
                CsvExporter.Export(fonts.ToArray(), width, height, _config.YOrigin, csvOut);
            }

            if (fntOut != null)
            {
                Console.WriteLine($"Saving FNT to: {fntOut}");
                double distanceRange = _config.PxRange.Upper - _config.PxRange.Lower;

                var metrics = fontGeometry.GetMetrics();

                FntExporter.Export(
                    fonts.ToArray(),
                    _config.Type,
                    width,
                    height,
                    _config.Size,
                    distanceRange,
                    imageOut,
                    fntOut,
                    metrics,
                    _config.YOrigin,
                    _config.OuterPxPadding,
                    _config.Spacing
                );
            }

            // 9. Test Render (optional)
            if (_config.TestRender)
            {
                string testRenderOut = string.IsNullOrEmpty(_config.TestRenderFile)
                    ? ResolveOutputPath("", $"{fontName}_render.png", RenderFolder)
                    : ResolveOutputPath(_config.TestRenderFile, null, RenderFolder);
                
                Console.WriteLine($"Rendering test image to: {testRenderOut}");
                RenderTestImage(generator.AtlasStorage.Bitmap, testRenderOut);
            }

            Console.WriteLine("Done.");
            return 0;
        }

        private void DumpRequestedGlyph(FontGeometry font, string glyphSpec)
        {
            // Resolve codepoint
            uint codepoint = 0;
            if (glyphSpec.Length == 1)
                codepoint = glyphSpec[0];
            else if (glyphSpec.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                codepoint = Convert.ToUInt32(glyphSpec.Substring(2), 16);
            else if (uint.TryParse(glyphSpec, out uint cpInt))
                codepoint = cpInt;

            var glyph = font.GetGlyph(codepoint);
            if (glyph == null)
            {
                Console.WriteLine($"DebugGlyph: codepoint {codepoint} not found.");
                return;
            }

            // Prepare filenames
            string name = codepoint != 0 ? char.ConvertFromUtf32((int)codepoint) : glyph.GetIndex().ToString();
            string rawOut = Path.Combine(GlyphDumpFolder, $"raw_msdf_{name}.png");
            string renderOut = Path.Combine(GlyphDumpFolder, $"render_{name}.png");

            // Use glyph's projection and range to generate per-glyph MSDF
            var proj = glyph.GetBoxProjection();
            var range = glyph.GetBoxRange();
            glyph.GetBoxRect(out int gx, out int gy, out int gw, out int gh);

            int channels = GetChannelCount(_config.Type);
            var msdf = new Bitmap<float>(Math.Max(gw, 1), Math.Max(gh, 1), channels);

            switch (_config.Type)
            {
                case ImageType.Sdf:
                    MsdfGenerator.GenerateSDF(msdf, glyph.GetShape()!, proj, range, new GeneratorConfig(true));
                    break;
                case ImageType.Psdf:
                    MsdfGenerator.GeneratePSDF(msdf, glyph.GetShape()!, new SDFTransformation(proj, new DistanceMapping(range)), new GeneratorConfig(true));
                    break;
                case ImageType.Msdf:
                    MsdfGenerator.GenerateMSDF(msdf, glyph.GetShape()!, proj, range, new MSDFGeneratorConfig(true, GetErrorCorrectionConfig()));
                    break;
                case ImageType.Mtsdf:
                    MsdfGenerator.GenerateMTSDF(msdf, glyph.GetShape()!, new SDFTransformation(proj, new DistanceMapping(range)), new MSDFGeneratorConfig(true, GetErrorCorrectionConfig()));
                    break;
            }

            AtlasSaver.SaveAtlas(msdf, rawOut);

            // Render view
            var render = new Bitmap<float>(_config.TestRenderWidth, _config.TestRenderHeight, 3);
            var renderRange = new Msdfgen.Range(_config.PxRange.Upper - _config.PxRange.Lower);
            if (_config.Type == ImageType.Msdf || _config.Type == ImageType.Mtsdf)
                SdfRenderer.RenderMSDF(render, msdf, renderRange);
            else
                SdfRenderer.RenderSDF(render, msdf, renderRange);

            AtlasSaver.SaveAtlas(render, renderOut);
            Console.WriteLine($"Dumped glyph {name} -> {rawOut}, {renderOut}");
        }

        private void RenderTestImage(Bitmap<float> msdfAtlas, string outputPath)
        {
            int width = msdfAtlas.Width;
            int height = msdfAtlas.Height;
            
            var renderOutput = new Bitmap<float>(width, height, 3);
            var renderRange = new Msdfgen.Range(_config.PxRange.Upper - _config.PxRange.Lower);

            if (_config.Type == ImageType.Msdf || _config.Type == ImageType.Mtsdf)
                SdfRenderer.RenderMSDF(renderOutput, msdfAtlas, renderRange);
            else
                SdfRenderer.RenderSDF(renderOutput, msdfAtlas, renderRange);

            AtlasSaver.SaveAtlas(renderOutput, outputPath);
        }

        private string ResolveOutputPath(string path, string? defaultName, string defaultFolder)
        {
            if (string.IsNullOrEmpty(path) && defaultName != null)
            {
                path = defaultName;
            }

            // If path is just a filename (no directory), put it in the specified folder
            if (!string.IsNullOrEmpty(path) && !Path.IsPathRooted(path) && !path.Contains(Path.DirectorySeparatorChar) && !path.Contains('/'))
            {
                path = Path.Combine(defaultFolder, path);
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
                int duplicates = 0;
                foreach (char c in _config.InlineChars)
                {
                    if (!charset.Add(c))
                        duplicates++;
                }
                if (duplicates > 0)
                    Console.WriteLine($"Ignored {duplicates} duplicate characters in inline charset.");
                return charset;
            }

            if (!string.IsNullOrEmpty(_config.CharsetPath) && File.Exists(_config.CharsetPath))
            {
                // Simple charset parser - just read characters from file
                var charset = new Charset();
                string content = File.ReadAllText(_config.CharsetPath);
                int duplicates = 0;
                foreach (char c in content)
                {
                    if (!char.IsControl(c))
                    {
                        if (!charset.Add(c))
                            duplicates++;
                    }
                }
                if (duplicates > 0)
                    Console.WriteLine($"Ignored {duplicates} duplicate characters in charset file.");
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

        private ErrorCorrectionConfig GetErrorCorrectionConfig()
        {
            var mode = ErrorCorrectionConfig.DistanceErrorCorrectionMode.EDGE_ONLY;
            var distanceCheck = ErrorCorrectionConfig.DistanceCheckMode.CHECK_DISTANCE_AT_EDGE;

            switch (_config.ErrorCorrection.ToLower())
            {
                case "disabled": 
                    mode = ErrorCorrectionConfig.DistanceErrorCorrectionMode.DISABLED; 
                    break;
                case "indiscriminate": 
                    mode = ErrorCorrectionConfig.DistanceErrorCorrectionMode.INDISCRIMINATE;
                    distanceCheck = ErrorCorrectionConfig.DistanceCheckMode.CHECK_DISTANCE_ALWAYS; // Usually desired for thorough fix
                    break;
                case "edgeonly": 
                    mode = ErrorCorrectionConfig.DistanceErrorCorrectionMode.EDGE_ONLY; 
                    break;
                case "auto": 
                    mode = ErrorCorrectionConfig.DistanceErrorCorrectionMode.AUTO; 
                    break;
            }
            return new ErrorCorrectionConfig(mode, distanceCheck);
        }

        private void PrintConfiguration()
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(" MSDF Atlas Gen Configuration");
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine($"  Font Path:        {_config.FontPath}");
            Console.WriteLine($"  Font Name:        {_config.FontName ?? "Auto"}");
            Console.WriteLine($"  Charset:          {(!string.IsNullOrEmpty(_config.InlineChars) ? "Inline Content" : (string.IsNullOrEmpty(_config.CharsetPath) ? "ASCII (Default)" : _config.CharsetPath))}");
            Console.WriteLine($"  Atlas Type:       {_config.Type}");
            Console.WriteLine($"  Output Format:    {_config.Format}");
            Console.WriteLine($"  Glyph Size:       {_config.Size} pt");
            Console.WriteLine($"  Pixel Range:      {_config.PxRange.Lower} to {_config.PxRange.Upper}");
            Console.WriteLine($"  Miter Limit:      {_config.MiterLimit}");
            Console.WriteLine($"  Overlap Support:  {_config.Overlap}");
            Console.WriteLine($"  Error Correction: {_config.ErrorCorrection}");
            Console.WriteLine($"  Color Strategy:   {_config.ColoringStrategy}");
            Console.WriteLine($"  Angle Threshold:  {_config.AngleThreshold}");
            Console.WriteLine($"  Dimensions:       {(_config.Width > 0 ? _config.Width.ToString() : "Auto")} x {(_config.Height > 0 ? _config.Height.ToString() : "Auto")}");
            Console.WriteLine($"  Packing Spacing:  {_config.Spacing} px");
            Console.WriteLine($"  Padding (Outer):  L:{_config.OuterPxPadding.L} B:{_config.OuterPxPadding.B} R:{_config.OuterPxPadding.R} T:{_config.OuterPxPadding.T}");
            Console.WriteLine($"  Y-Origin:         {_config.YOrigin}");
            Console.WriteLine($"  Thread Count:     {(_config.Threads == 0 ? $"Auto ({Environment.ProcessorCount})" : _config.Threads)}");
            Console.WriteLine($"  Scanline Pass:    {_config.Scanline}");
            Console.WriteLine($"  Test Render:      {_config.TestRender}");
            Console.WriteLine("-------------------------------------------------------------------------------");
        }
    }
}
