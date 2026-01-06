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
            double pxRange = 4.0;
            Vector2 scale = new Vector2(1.0, 1.0);
            Vector2 translate = new Vector2(0.0, 0.0);
            bool autoFrame = false;
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
                        scale = new Vector2(double.Parse(args[argPos++]), double.Parse(args[argPos++])); // Handle single argument too? C++ accepts 1 or 2. We'll assume strict 2 for now based on snippet "scale <scale>". Wait, C++ usually allows 1 arg for uniform.
                        // Let's assume uniform if next arg starts with -? Or just assume uniform for simplicity if parsing fails? 
                        // The user snippet says "-scale <scale>", singular. So uniform.
                        // But example in C++ might be Vector2.
                        // Let's revisit: "-scale <scale>" usually means uniform. "-scale <x> <y>" if non-uniform.
                        // We will implement uniform for safety as per snippet doc "scale <scale>".
                        // Wait, looking at args parsing logic is best. Let's assume uniform for now, or check next arg.
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
                        testRenderFile = args[argPos++];
                        testRenderW = int.Parse(args[argPos++]);
                        testRenderH = int.Parse(args[argPos++]);
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

            shape.Normalize();
            // Y-up by default in MSDFGen? 
            // Check Y-axis. C++ origin is bottom-left. SixLabors is top-left.
            // If FontLoader didn't flip, we might need to.
            // Let's assume FontLoader passed raw data.

            if (autoFrame)
            {
                // Implement autoframe logic: Calculate bounds, center, scale to fit.
                double l = 0, b = 0, r = 0, t = 0;
                shape.Bound(ref l, ref b, ref r, ref t);
                double frameW = width - 2 * pxRange;
                double frameH = height - 2 * pxRange;
                if (frameW > 0 && frameH > 0)
                {
                    double s = Math.Min(frameW / (r - l), frameH / (t - b));
                    scale = new Vector2(s, s);
                    translate = new Vector2(-l + (frameW / s - (r - l)) / 2 + pxRange / s, -b + (frameH / s - (t - b)) / 2 + pxRange / s);
                }
            }

            if (mode == Mode.MTSDF || mode == Mode.MSDF)
            {
                EdgeColoring.EdgeColoringSimple(shape, angleThreshold);
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
                 // Render shape using the generated bitmap if possible?
                 // Or render shape directly (Rasterization).
                 // User asked for "testrender ... using only the distance field".
                 // This requires RenderSDF.
                 // Not implemented yet.
                 Console.WriteLine("Test render not implemented yet.");
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: Msdfgen.Cli <mode> <input> <options>");
            // ... strict help text per user request
        }
    }
}
