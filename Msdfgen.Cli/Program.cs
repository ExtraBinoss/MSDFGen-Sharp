using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SixLabors.Fonts;
using Msdfgen;
using Msdfgen.Extensions;

namespace Msdfgen.Cli
{
    class Program
    {
        private enum Mode
        {
            SDF,
            PSDF,
            MSDF,
            MTSDF
        }
        
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; // Ensure consistent parsing

            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            // Parse Mode
            Mode mode = Mode.MSDF;
            int argPos = 0;
            string modeStr = args[0].ToLower();
            if (modeStr == "sdf") { mode = Mode.SDF; argPos++; }
            else if (modeStr == "psdf") { mode = Mode.PSDF; argPos++; }
            else if (modeStr == "msdf") { mode = Mode.MSDF; argPos++; }
            else if (modeStr == "mtsdf") { mode = Mode.MTSDF; argPos++; }
            else if (modeStr == "metrics") { /* mode = Mode.METRICS; */ argPos++; } // Not Implemented yet

            if (argPos >= args.Length)
            {
                Console.WriteLine("No input specified.");
                return;
            }

            // Inputs
            string fontFile = null;
            char charCode = '\0';
            string shapeDescFile = null;
            string shapeDesc = null;
            
            // Options
            string outputFile = "output.png";
            int width = 64, height = 64;
            double pxRange = 2.0;  // C++ default is 2
            Vector2 scale = new Vector2(1.0, 1.0);
            Vector2 translate = new Vector2(0.0, 0.0);
            bool autoFrame = false;
            bool scaleSpecified = false;
            double angleThreshold = 3.0;
            string exportShapeFile = null;
            string testRenderFile = null;
            int testRenderW = 0, testRenderH = 0;
            bool printMetrics = false;
            
            // Parse arguments
            while (argPos < args.Length)
            {
                string arg = args[argPos++];
                switch (arg.ToLower())
                {
                    case "-font":
                        fontFile = args[argPos++];
                        string charArg = args[argPos++];
                        if (charArg.Length == 3 && charArg.StartsWith("'") && charArg.EndsWith("'"))
                            charCode = charArg[1];
                        else if (charArg.StartsWith("0x"))
                            charCode = (char)int.Parse(charArg.Substring(2), NumberStyles.HexNumber);
                        else if (int.TryParse(charArg, out int code))
                            charCode = (char)code;
                        else
                            charCode = charArg[0];
                        break;
                    case "-shapedesc":
                        shapeDescFile = args[argPos++];
                        break;
                     case "-defineshape":
                        shapeDesc = args[argPos++];
                        break;
                    case "-o":
                        outputFile = args[argPos++];
                        break;
                    case "-dimensions":
                        width = int.Parse(args[argPos++]);
                        height = int.Parse(args[argPos++]);
                        break;
                    case "-range":
                    case "-pxrange":
                         pxRange = double.Parse(args[argPos++]);
                        break;
                    case "-scale":
                        double s = double.Parse(args[argPos++]);
                        scale = new Vector2(s, s); // C++ uses single value for uniform scale
                        scaleSpecified = true;
                        break;
                    case "-translate":
                        translate = new Vector2(double.Parse(args[argPos++]), double.Parse(args[argPos++]));
                        break;
                    case "-autoframe":
                        autoFrame = true;
                        break;
                    case "-angle":
                        string angleArg = args[argPos++];
                        if (angleArg.EndsWith("D", StringComparison.OrdinalIgnoreCase))
                            angleThreshold = double.Parse(angleArg.Substring(0, angleArg.Length - 1)) * Math.PI / 180.0;
                        else
                            angleThreshold = double.Parse(angleArg);
                        break;
                    case "-exportshape":
                        exportShapeFile = args[argPos++];
                        break;
                    case "-testrender":
                        if (argPos + 3 <= args.Length)
                        {
                            testRenderFile = args[argPos++];
                            testRenderW = int.Parse(args[argPos++]);
                            testRenderH = int.Parse(args[argPos++]);
                        }
                        else
                        {
                            Console.WriteLine("Error: -testrender requires <filename> <width> <height>");
                            return;
                        }
                        break;
                     case "-printmetrics":
                        printMetrics = true;
                        break;
                    case "-help":
                         PrintHelp();
                         return;
                }
            }

            // Logic
            Shape shape = null;
            if (fontFile != null)
            {
                var collection = new FontCollection();
                var family = collection.Add(fontFile);
                var font = family.CreateFont(12, FontStyle.Regular); // Size doesn't matter for vector extraction
                shape = FontLoader.LoadShape(font, charCode);
            }
            // else if shapeDesc...

            if (shape == null)
            {
                Console.WriteLine("No valid shape loaded.");
                return;
            }
            
            if (!shape.Validate())
                 Console.WriteLine("Shape validation failed.");

            // Orient contours to fix winding order (important for correct SDF signs)
            shape.OrientContours();
            shape.Normalize();

            // Debug: Print shape info
            Console.WriteLine($"Shape has {shape.Contours.Count} contour(s)");
            for (int c = 0; c < shape.Contours.Count; c++)
            {
                var contour = shape.Contours[c];
                Console.WriteLine($"  Contour {c}: {contour.Edges.Count} edges, winding={contour.Winding()}");
            }

            // C++: If no scale specified and mode is not metrics, autoframe is commonly expected for usable output
            // Enable autoframe if no scale specified explicitly
            if (!scaleSpecified && !autoFrame)
            {
                autoFrame = true; // Auto-enable for sensible defaults
            }

            if (autoFrame)
            {
                // Implement autoframe logic: Calculate bounds, center, scale to fit.
                double l = 1e240, b = 1e240, r = -1e240, t = -1e240;
                shape.Bound(ref l, ref b, ref r, ref t);
                
                if (l >= r || b >= t)
                {
                    // Empty or invalid bounds, use fallback
                    l = 0; b = 0; r = 1; t = 1;
                }
                
                // C++ logic for autoframe with pxRange
                Vector2 frame = new Vector2(width, height);
                frame = new Vector2(frame.X + 2 * (-pxRange / 2), frame.Y + 2 * (-pxRange / 2)); // frame += 2*pxRange.lower (lower = -pxRange/2)
                
                if (frame.X <= 0 || frame.Y <= 0)
                {
                    Console.WriteLine("Cannot fit the specified pixel range.");
                    return;
                }
                
                Vector2 dims = new Vector2(r - l, t - b);
                
                if (!scaleSpecified)
                {
                    if (dims.X * frame.Y < dims.Y * frame.X)
                    {
                        // Height-constrained
                        double fitScale = frame.Y / dims.Y;
                        translate = new Vector2(0.5 * (frame.X / frame.Y * dims.Y - dims.X) - l, -b);
                        scale = new Vector2(fitScale, fitScale);
                    }
                    else
                    {
                        // Width-constrained
                        double fitScale = frame.X / dims.X;
                        translate = new Vector2(-l, 0.5 * (frame.Y / frame.X * dims.X - dims.Y) - b);
                        scale = new Vector2(fitScale, fitScale);
                    }
                    // Offset for pxRange (pxRange.lower = -pxRange/2)
                    translate = new Vector2(translate.X - (-pxRange / 2) / scale.X, translate.Y - (-pxRange / 2) / scale.Y);
                }
                else
                {
                    translate = new Vector2(0.5 * (frame.X / scale.X - dims.X) - l, 0.5 * (frame.Y / scale.Y - dims.Y) - b);
                }
            }

            if (mode == Mode.MTSDF || mode == Mode.MSDF)
            {
                EdgeColoring.EdgeColoringSimple(shape, angleThreshold);
                
                // Debug: Print edge colors
                for (int c = 0; c < shape.Contours.Count; c++)
                {
                    var contour = shape.Contours[c];
                    var colorSet = new HashSet<EdgeColor>();
                    foreach (var edge in contour.Edges)
                        colorSet.Add(edge.Color);
                    Console.WriteLine($"  Contour {c} colors: {string.Join(", ", colorSet)}");
                }
            }

            // Print metrics if requested
            if (printMetrics)
            {
                double l = 1e240, b = 1e240, r = -1e240, t = -1e240;
                shape.Bound(ref l, ref b, ref r, ref t);
                Console.WriteLine($"bounds = {l}, {b}, {r}, {t}");
                Console.WriteLine($"scale = {scale.X}, {scale.Y}");
                Console.WriteLine($"translate = {translate.X}, {translate.Y}");
                Console.WriteLine($"range {-pxRange/Math.Min(scale.X, scale.Y)} to {pxRange/Math.Min(scale.X, scale.Y)}");
            }

            int channels = (mode == Mode.MSDF || mode == Mode.PSDF) ? 3 : (mode == Mode.MTSDF ? 4 : 1); 
            // Wait, PSDF is monochrome (1). MSDF is 3. MTSDF is 4. SDF is 1.
            // Correct per doc: "psdf – generates a monochrome signed perpendicular distance field."
            if (mode == Mode.PSDF) channels = 1;
            
            Bitmap<float> output = new Bitmap<float>(width, height, channels);
            
            // Configs
            // Range in MSDFGen C++ is usually in "Shape Units" (em).
            // -pxrange specifies range in pixels.
            // Range = pxRange / scale.
            // MSDFGen C++ CLI prioritizes -range if both? No, -pxrange is handy.
            // We have pxRange from arg. range = pxRange/min(scale).
            
            double rangeValue = pxRange / Math.Min(scale.X, scale.Y);
            Range range = new Range(rangeValue);
            Projection projection = new Projection(scale, translate);
            
            GeneratorConfig genConfig = new GeneratorConfig(true);
            MSDFGeneratorConfig msdfConfig = new MSDFGeneratorConfig(true, new ErrorCorrectionConfig(ErrorCorrectionConfig.DistanceErrorCorrectionMode.EDGE_ONLY));

            Console.WriteLine($"Generating {mode}...");
            
            switch (mode)
            {
                case Mode.SDF:
                    MsdfGenerator.GenerateSDF(output, shape, projection, range, genConfig);
                    break;
                case Mode.PSDF:
                    MsdfGenerator.GeneratePSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), genConfig);
                    break;
                case Mode.MSDF:
                    MsdfGenerator.GenerateMSDF(output, shape, projection, range, msdfConfig);
                    break;
                case Mode.MTSDF:
                    MsdfGenerator.GenerateMTSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), msdfConfig);
                    break;
            }
            
            Console.WriteLine($"Saving to {outputFile}...");
            ImageSaver.Save(output, outputFile);
            
            if (testRenderFile != null)
            {
                Console.WriteLine($"Rendering test image to {testRenderFile}...");
                Bitmap<float> renderOutput = new Bitmap<float>(testRenderW, testRenderH, 3);
                
                // Use pxRange for rendering
                Range renderRange = new Range(pxRange);
                
                if (mode == Mode.MSDF || mode == Mode.MTSDF)
                {
                    SdfRenderer.RenderMSDF(renderOutput, output, renderRange);
                }
                else
                {
                    SdfRenderer.RenderSDF(renderOutput, output, renderRange);
                }
                
                ImageSaver.Save(renderOutput, testRenderFile);
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: Msdfgen.Cli <mode> <input> <options>");
            // ... strict help text per user request
        }
    }
}
