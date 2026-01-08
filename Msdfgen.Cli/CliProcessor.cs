using System;
using System.Collections.Generic;
using System.IO;
using Msdfgen;
using Msdfgen.Extensions;

namespace Msdfgen.Cli
{
    public class CliProcessor
    {
        public void Process(CliOptions options)
        {
            PrintConfiguration(options);

            // 1. Load Shape
            Console.WriteLine("[PHASE] Loading Glyph Geometry...");
            Shape? shape = LoadShape(options);
            if (shape == null) return;

            // 2. Pre-process
            if (!shape.Validate())
                 Console.WriteLine("Warning: Shape validation failed.");

            shape.OrientContours();
            shape.Normalize();

            // Debug info
            PrintShapeInfo(shape);

            // 3. Configuration (AutoFrame, Projection)
            ConfigureGeometry(shape, options);
            ConfigureOutputPaths(options);

            // 4. Color Edges (if needed)
            if (options.Mode == CliOptions.MsdfMode.MSDF || options.Mode == CliOptions.MsdfMode.MTSDF)
            {
                EdgeColoring.EdgeColoringSimple(shape, options.AngleThreshold);
                PrintEdgeColors(shape);
            }

            // 5. Print Metrics
            if (options.PrintMetrics)
                PrintMetrics(shape, options);

            // 6. Generate
            Console.WriteLine("[PHASE] Generating MSDF Pixels...");
            Bitmap<float> output = Generate(shape, options);

            // 7. Save
            Console.WriteLine("[PHASE] Saving Result...");
            Console.WriteLine($"  Saving to {options.OutputFile}...");
            ImageSaver.Save(output, options.OutputFile);

            // 8. Test Render
            if (options.TestRenderFile != null)
            {
                Console.WriteLine($"Rendering test image to {options.TestRenderFile}...");
                RenderTestImage(output, options);
            }
        }

        private Shape? LoadShape(CliOptions options)
        {
            if (string.IsNullOrEmpty(options.FontFile))
            {
                Console.Error.WriteLine("[Error] Missing font argument. Please specify a font file.\nUsage: msdf -font <file.ttf> '<character>'");
                return null;
            }

            using var font = FreetypeHandle.Initialize();
            if (font == null)
            {
                Console.Error.WriteLine("[Fatal Error] Could not initialize FreeType library.");
                return null;
            }

            using var fontHandle = FontHandle.LoadFont(font, options.FontFile);
            if (fontHandle == null)
            {
                Console.Error.WriteLine("[Error] Unable to load the font file. Check if the path exists and the file is valid.");
                return null;
            }

            var shape = new Shape();
            uint codepoint = options.CharCode;
            if (!FontLoader.LoadGlyph(shape, fontHandle, codepoint, FontCoordinateScaling.EmNormalized, out double advance))
            {
                Console.Error.WriteLine($"[Error] Could not load glyph data for character U+{codepoint:X4}. It may not exist in the font.");
                return null;
            }

            // Validate and normalize similar to atlas path
            if (!shape.Validate())
                Console.WriteLine("Warning: Shape validation failed.");
            shape.Normalize();

            // Fix winding if needed (outside distance positive)
            var bounds = shape.GetBounds();
            Vector2 outerPoint = new(
                bounds.L - (bounds.R - bounds.L) - 1,
                bounds.B - (bounds.T - bounds.B) - 1
            );
            var combiner = new SimpleContourCombiner<TrueDistanceSelector>(shape);
            var finder = new ShapeDistanceFinder<SimpleContourCombiner<TrueDistanceSelector>>(shape, combiner);
            double distance = finder.Distance(outerPoint);
            if (distance > 0)
            {
                foreach (var contour in shape.Contours)
                    contour.Reverse();
            }
            return shape;
        }

        private static void PrintShapeInfo(Shape shape)
        {
            Console.WriteLine($"Shape has {shape.Contours.Count} contour(s)");
            for (int c = 0; c < shape.Contours.Count; c++)
            {
                var contour = shape.Contours[c];
                Console.WriteLine($"  Contour {c}: {contour.Edges.Count} edges, winding={contour.Winding()}");
            }
        }

        private static void PrintEdgeColors(Shape shape)
        {
             for (int c = 0; c < shape.Contours.Count; c++)
            {
                var contour = shape.Contours[c];
                var colorSet = new HashSet<EdgeColor>();
                foreach (var edge in contour.Edges)
                    colorSet.Add(edge.Color);
                Console.WriteLine($"  Contour {c} colors: {string.Join(", ", colorSet)}");
            }
        }

        private static void ConfigureGeometry(Shape shape, CliOptions options)
        {
            // Autoframe logic
            if (!options.ScaleSpecified && !options.AutoFrame)
                options.AutoFrame = true;

            if (options.AutoFrame)
            {
                double l = 1e240, b = 1e240, r = -1e240, t = -1e240;
                shape.Bound(ref l, ref b, ref r, ref t);
                
                if (l >= r || b >= t) { l = 0; b = 0; r = 1; t = 1; }

                Vector2 frame = new Vector2(options.Width, options.Height);
                frame = new Vector2(frame.X - options.PxRange, frame.Y - options.PxRange);

                if (frame.X <= 0 || frame.Y <= 0)
                {
                    Console.WriteLine("Cannot fit the specified pixel range.");
                    return; 
                }

                Vector2 dims = new Vector2(r - l, t - b);

                if (!options.ScaleSpecified)
                {
                    if (dims.X * frame.Y < dims.Y * frame.X)
                    {
                        double fitScale = frame.Y / dims.Y;
                        options.Translate = new Vector2(0.5 * (frame.X / frame.Y * dims.Y - dims.X) - l, -b);
                        options.Scale = new Vector2(fitScale, fitScale);
                    }
                    else
                    {
                        double fitScale = frame.X / dims.X;
                        options.Translate = new Vector2(-l, 0.5 * (frame.Y / frame.X * dims.X - dims.Y) - b);
                        options.Scale = new Vector2(fitScale, fitScale);
                    }
                    // Adjust for pxRange centering
                    options.Translate += new Vector2((options.PxRange/2)/options.Scale.X, (options.PxRange/2)/options.Scale.Y);
                }
                else
                {
                    options.Translate = new Vector2(0.5 * (frame.X / options.Scale.X - dims.X) - l, 0.5 * (frame.Y / options.Scale.Y - dims.Y) - b);
                }
            }
        }

        private void ConfigureOutputPaths(CliOptions options)
        {
            try 
            {
                // Determine base directory: favor "Msdfgen.Cli" if it exists (running from root), else use current directory.
                string baseDir = "Msdfgen.Cli";
                if (!Directory.Exists(baseDir))
                {
                    baseDir = ".";
                }

                string rawMsdfDir = Path.Combine(baseDir, "RawMsdf");
                string renderDir = Path.Combine(baseDir, "Render");
                
                if (!Directory.Exists(rawMsdfDir)) Directory.CreateDirectory(rawMsdfDir);
                if (!Directory.Exists(renderDir)) Directory.CreateDirectory(renderDir);
                
                // Auto-generate OutputFile if not specified
                if (!options.OutputFileSpecified)
                {
                    string charName = GetCharName(options.CharCode);
                    // If OutputFile was null/default, we set it here.
                    // Note: CliOptions defaults OutputFile to "output.png", so we overwrite it if !OutputFileSpecified
                    options.OutputFile = Path.Combine(rawMsdfDir, $"raw_msdf_{charName}.png");
                }
                else if (!string.IsNullOrEmpty(options.OutputFile))
                {
                    // User provided a filename/path
                    string? dir = Path.GetDirectoryName(options.OutputFile);
                    if (string.IsNullOrEmpty(dir))
                    {
                            // It's just a filename, put in RawMsdf
                            options.OutputFile = Path.Combine(rawMsdfDir, options.OutputFile);
                    }
                    // Else respect user path
                }
                
                // Auto-generate TestRenderFile if specified (flag true) but no file given
                if (options.TestRenderSpecified && string.IsNullOrEmpty(options.TestRenderFile))
                {
                    string charName = GetCharName(options.CharCode);
                    options.TestRenderFile = Path.Combine(renderDir, $"rendered_{charName}.png");
                }
                else if (!string.IsNullOrEmpty(options.TestRenderFile))
                {
                    // If just filename, put in RenderDir
                    string? dir = Path.GetDirectoryName(options.TestRenderFile);
                    if (string.IsNullOrEmpty(dir))
                    {
                            options.TestRenderFile = Path.Combine(renderDir, options.TestRenderFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure output directories: {ex.Message}");
            }
        }

        private string GetCharName(char c)
        {
            if (char.IsLetterOrDigit(c)) return c.ToString();
            return ((int)c).ToString("X4");
        }

        private void PrintMetrics(Shape shape, CliOptions options)
        {
            double l = 1e240, b = 1e240, r = -1e240, t = -1e240;
            shape.Bound(ref l, ref b, ref r, ref t);
            Console.WriteLine($"bounds = {l}, {b}, {r}, {t}");
            Console.WriteLine($"scale = {options.Scale.X}, {options.Scale.Y}");
            Console.WriteLine($"translate = {options.Translate.X}, {options.Translate.Y}");
            Console.WriteLine($"range {-options.PxRange/Math.Min(options.Scale.X, options.Scale.Y)} to {options.PxRange/Math.Min(options.Scale.X, options.Scale.Y)}");
        }

        private Bitmap<float> Generate(Shape shape, CliOptions options)
        {
            Console.WriteLine($"Generating {options.Mode}...");
            
            int channels = (options.Mode == CliOptions.MsdfMode.MSDF || options.Mode == CliOptions.MsdfMode.PSDF) ? 3 : (options.Mode == CliOptions.MsdfMode.MTSDF ? 4 : 1);
            if (options.Mode == CliOptions.MsdfMode.PSDF) channels = 1;

            Bitmap<float> output = new Bitmap<float>(options.Width, options.Height, channels);
            
            double rangeValue = options.PxRange / Math.Min(options.Scale.X, options.Scale.Y);
            Range range = new Range(rangeValue);
            Projection projection = new Projection(options.Scale, options.Translate);
            
            GeneratorConfig genConfig = new GeneratorConfig(true);
            MSDFGeneratorConfig msdfConfig = new MSDFGeneratorConfig(true, new ErrorCorrectionConfig(ErrorCorrectionConfig.DistanceErrorCorrectionMode.EDGE_ONLY));

            switch (options.Mode)
            {
                case CliOptions.MsdfMode.SDF:
                    MsdfGenerator.GenerateSDF(output, shape, projection, range, genConfig);
                    break;
                case CliOptions.MsdfMode.PSDF:
                    MsdfGenerator.GeneratePSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), genConfig);
                    break;
                case CliOptions.MsdfMode.MSDF:
                    MsdfGenerator.GenerateMSDF(output, shape, projection, range, msdfConfig);
                    break;
                case CliOptions.MsdfMode.MTSDF:
                    MsdfGenerator.GenerateMTSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), msdfConfig);
                    break;
            }
            return output;
        }

        private void RenderTestImage(Bitmap<float> msdf, CliOptions options)
        {
             Bitmap<float> renderOutput = new Bitmap<float>(options.TestRenderWidth, options.TestRenderHeight, 3);
             Range renderRange = new Range(options.PxRange);
             
             if (options.Mode == CliOptions.MsdfMode.MSDF || options.Mode == CliOptions.MsdfMode.MTSDF)
                 SdfRenderer.RenderMSDF(renderOutput, msdf, renderRange);
             else
                 SdfRenderer.RenderSDF(renderOutput, msdf, renderRange);
                 
             if (!string.IsNullOrEmpty(options.TestRenderFile))
                 ImageSaver.Save(renderOutput, options.TestRenderFile!);
        }

        private static void PrintConfiguration(CliOptions options)
        {
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine(" MSDF Generator Configuration (Single Glyph)");
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine($"  Font File:        {options.FontFile}");
            Console.WriteLine($"  Character:        U+{(int)options.CharCode:X4} ({options.CharCode})");
            Console.WriteLine($"  Mode:             {options.Mode}");
            Console.WriteLine($"  Width x Height:   {options.Width} x {options.Height}");
            Console.WriteLine($"  Pixel Range:      {options.PxRange}");
            Console.WriteLine($"  Angle Threshold:  {options.AngleThreshold}");
            Console.WriteLine($"  AutoFrame:        {options.AutoFrame}");
            Console.WriteLine($"  Output File:      {options.OutputFile}");
            Console.WriteLine($"  Test Render:      {(options.TestRenderFile ?? "Disabled")}");
            Console.WriteLine("-------------------------------------------------------------------------------");
        }
    }
}
