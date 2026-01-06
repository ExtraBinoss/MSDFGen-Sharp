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
            // 1. Load Shape
            Shape shape = LoadShape(options);
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
            Bitmap<float> output = Generate(shape, options);

            // 7. Save
            Console.WriteLine($"Saving to {options.OutputFile}...");
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
            Console.WriteLine("Font loading via SixLabors has been removed. Font loading is now handled by Typography.OpenFont in MsdfAtlasGen.Cli.");
            Console.WriteLine("Please use MsdfAtlasGen.Cli for font-based atlas generation.");
            return null;
        }

        private void PrintShapeInfo(Shape shape)
        {
            Console.WriteLine($"Shape has {shape.Contours.Count} contour(s)");
            for (int c = 0; c < shape.Contours.Count; c++)
            {
                var contour = shape.Contours[c];
                Console.WriteLine($"  Contour {c}: {contour.Edges.Count} edges, winding={contour.Winding()}");
            }
        }

        private void PrintEdgeColors(Shape shape)
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

        private void ConfigureGeometry(Shape shape, CliOptions options)
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
                string baseDir = "Msdfgen.Cli"; 
                if (Directory.Exists(baseDir))
                {
                    string rawMsdfDir = Path.Combine(baseDir, "RawMsdf");
                    string renderDir = Path.Combine(baseDir, "Render");
                    
                    if (!Directory.Exists(rawMsdfDir)) Directory.CreateDirectory(rawMsdfDir);
                    if (!Directory.Exists(renderDir)) Directory.CreateDirectory(renderDir);
                    
                    // Auto-generate OutputFile if not specified
                    if (!options.OutputFileSpecified)
                    {
                        string charName = GetCharName(options.CharCode);
                        options.OutputFile = Path.Combine(rawMsdfDir, $"raw_msdf_{charName}.png");
                    }
                    else if (!string.IsNullOrEmpty(options.OutputFile))
                    {
                        // Existing logic for specified file, just ensuring directory correctness relative to rawMsdfDir if needed?
                        // User might have provided full path or relative. Use as is if it looks like a path?
                        // Previous logic forced it into RawMsdfDir. Let's keep that behavior if it's just a filename.
                        string fName = Path.GetFileName(options.OutputFile);
                        string dir = Path.GetDirectoryName(options.OutputFile);
                        if (string.IsNullOrEmpty(dir))
                        {
                             // It's just a filename, put in RawMsdf
                             options.OutputFile = Path.Combine(rawMsdfDir, fName);
                        }
                        // Else user provided a path, respect it (or relative to cwd)
                    }
                    
                    // Auto-generate TestRenderFile if specified (flag true) but no file given, OR if not specified but user wants auto behavior?
                    // User asked: "if I omit the -o and testrender 'path'..." implies he might invoke -testrender w/o path.
                    if (options.TestRenderSpecified && string.IsNullOrEmpty(options.TestRenderFile))
                    {
                        string charName = GetCharName(options.CharCode);
                        options.TestRenderFile = Path.Combine(renderDir, $"rendered_{charName}.png");
                    }
                    else if (!string.IsNullOrEmpty(options.TestRenderFile))
                    {
                         // Similar logic: if just filename, put in RenderDir
                        string fName = Path.GetFileName(options.TestRenderFile);
                        string dir = Path.GetDirectoryName(options.TestRenderFile);
                        if (string.IsNullOrEmpty(dir))
                        {
                             options.TestRenderFile = Path.Combine(renderDir, fName);
                        }
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
    }
}
