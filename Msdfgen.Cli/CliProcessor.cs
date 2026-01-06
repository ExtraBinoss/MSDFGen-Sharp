using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.Fonts;
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

        private Shape LoadShape(CliOptions options)
        {
            Shape shape = null;
            if (options.FontFile != null)
            {
                try 
                {
                    var collection = new FontCollection();
                    var family = collection.Add(options.FontFile);
                    var font = family.CreateFont(12, FontStyle.Regular);
                    shape = FontLoader.LoadShape(font, options.CharCode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading font: {ex.Message}");
                }
            }
            if (shape == null) Console.WriteLine("No valid shape loaded.");
            return shape;
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
                    
                    if (!string.IsNullOrEmpty(options.OutputFile))
                    {
                        string fName = Path.GetFileName(options.OutputFile);
                        if (fName.StartsWith("output_", StringComparison.OrdinalIgnoreCase))
                            fName = fName.Substring(7);
                        if (!fName.StartsWith("raw_msdf_", StringComparison.OrdinalIgnoreCase))
                            fName = "raw_msdf_" + fName;
                        options.OutputFile = Path.Combine(rawMsdfDir, fName);
                    }
                    
                    if (!string.IsNullOrEmpty(options.TestRenderFile))
                    {
                        string fName = Path.GetFileName(options.TestRenderFile);
                        if (fName.StartsWith("output_render_", StringComparison.OrdinalIgnoreCase))
                            fName = fName.Substring(14);
                        else if (fName.StartsWith("output_", StringComparison.OrdinalIgnoreCase))
                            fName = fName.Substring(7);
                        if (!fName.StartsWith("rendered_", StringComparison.OrdinalIgnoreCase))
                            fName = "rendered_" + fName;
                        options.TestRenderFile = Path.Combine(renderDir, fName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure output directories: {ex.Message}");
            }
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
                 
             ImageSaver.Save(renderOutput, options.TestRenderFile);
        }
    }
}
