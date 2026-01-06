using System;
using System.Globalization;
using Msdfgen;

namespace Msdfgen.Cli
{
    public static class CliParser
    {
        public static CliOptions? Parse(string[] args)
        {
            var options = new CliOptions();

            if (args.Length == 0)
            {
                options.IsHelp = true;
                return options;
            }

            int argPos = 0;
            
            // Parse Mode
            string modeStr = args[0].ToLower();
            if (modeStr == "sdf") { options.Mode = CliOptions.MsdfMode.SDF; argPos++; }
            else if (modeStr == "psdf") { options.Mode = CliOptions.MsdfMode.PSDF; argPos++; }
            else if (modeStr == "msdf") { options.Mode = CliOptions.MsdfMode.MSDF; argPos++; }
            else if (modeStr == "mtsdf") { options.Mode = CliOptions.MsdfMode.MTSDF; argPos++; }
            else if (modeStr == "metrics") { /* mode = Mode.METRICS; */ argPos++; } // Not Implemented yet

            if (argPos >= args.Length && !options.IsHelp)
            {
               // Default behavior if only mode provided? Or error?
               // Program.cs said "No input specified." if argPos >= args.Length
               // We'll let the validator handle missing inputs, but here we just return what we have.
            }

            // Parse Arguments
            while (argPos < args.Length)
            {
                string arg = args[argPos++];
                switch (arg.ToLower())
                {
                    case "-font":
                        options.FontFile = args[argPos++];
                        string charArg = args[argPos++];
                        if (charArg.Length == 3 && charArg.StartsWith("'") && charArg.EndsWith("'"))
                            options.CharCode = charArg[1];
                        else if (charArg.StartsWith("0x"))
                            options.CharCode = (char)int.Parse(charArg.Substring(2), NumberStyles.HexNumber);
                        else if (int.TryParse(charArg, out int code))
                            options.CharCode = (char)code;
                        else
                            options.CharCode = charArg[0];
                        break;
                    case "-shapedesc":
                        options.ShapeDescFile = args[argPos++];
                        break;
                     case "-defineshape":
                        options.ShapeDesc = args[argPos++];
                        break;
                    case "-o":
                        options.OutputFile = args[argPos++];
                        options.OutputFileSpecified = true;
                        break;
                    case "-dimensions":
                        options.Width = int.Parse(args[argPos++]);
                        options.Height = int.Parse(args[argPos++]);
                        break;
                    case "-range":
                    case "-pxrange":
                         options.PxRange = double.Parse(args[argPos++]);
                        break;
                    case "-scale":
                        double s = double.Parse(args[argPos++]);
                        options.Scale = new Vector2(s, s);
                        options.ScaleSpecified = true;
                        break;
                    case "-translate":
                        options.Translate = new Vector2(double.Parse(args[argPos++]), double.Parse(args[argPos++]));
                        break;
                    case "-autoframe":
                        options.AutoFrame = true;
                        break;
                    case "-angle":
                        string angleArg = args[argPos++];
                        if (angleArg.EndsWith("D", StringComparison.OrdinalIgnoreCase))
                            options.AngleThreshold = double.Parse(angleArg.Substring(0, angleArg.Length - 1)) * Math.PI / 180.0;
                        else
                            options.AngleThreshold = double.Parse(angleArg);
                        break;
                    case "-exportshape":
                        options.ExportShapeFile = args[argPos++];
                        break;
                    case "-testrender":
                        options.TestRenderSpecified = true; // User explicitly requested test render
                        // Check for optional arguments
                        // Potential conventions:
                        // -testrender (no args) -> default name, default size
                        // -testrender output.png -> explicit name, default size
                        // -testrender output.png W H -> explicit name, explicit size
                        
                        // We check next arg. If it starts with '-', it's a new flag, so no args provided.
                        if (argPos < args.Length && !args[argPos].StartsWith("-"))
                        {
                             string nextArg = args[argPos];
                             // Check if it's a number (width) - unlikely as first arg for testrender usually filename
                             if (int.TryParse(nextArg, out int _))
                             {
                                 // Odd usage: -testrender 512 512? Assume first is width if both are numbers?
                                 // Let's stick to consistent: Filename first.
                                 // If it's a number, it's ambiguous if we allow skipping filename. 
                                 // Let's assumes if args exist, first is filename.
                                 options.TestRenderFile = args[argPos++];
                             }
                             else
                             {
                                 options.TestRenderFile = args[argPos++];
                             }

                             // Now check for dimensions
                             if (argPos + 1 < args.Length && int.TryParse(args[argPos], out int w) && int.TryParse(args[argPos+1], out int h))
                             {
                                 options.TestRenderWidth = w;
                                 options.TestRenderHeight = h;
                                 argPos += 2;
                             }
                        }
                        // Default filename will be handled in Processor if TestRenderFile is null
                        break;
                     case "-printmetrics":
                        options.PrintMetrics = true;
                        break;
                    case "-help":
                         options.IsHelp = true;
                         return options;
                }
            }

            return options;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Usage: Msdfgen.Cli <mode> <input> <options>");
            Console.WriteLine("Modes: sdf, psdf, msdf, mtsdf");
            Console.WriteLine("Input:");
            Console.WriteLine("  -font <filename.ttf> <char>  Load character from font");
            Console.WriteLine("Options:");
            Console.WriteLine("  -o <filename.png>            Output filename");
            Console.WriteLine("  -dimensions <w> <h>          Output dimensions");
            Console.WriteLine("  -pxrange <n>                 Pixel range (default 2)");
            Console.WriteLine("  -scale <s|x y>               Scale (manual)");
            Console.WriteLine("  -translate <x> <y>           Translate (manual)");
            Console.WriteLine("  -autoframe                   Automatically frame the glyph to fit");
            Console.WriteLine("  -angle <n>                   Angle threshold for corner detection");
            Console.WriteLine("  -testrender <out.png> <w> <h> Render a test image from the generated DF");
            Console.WriteLine("  -printmetrics                Print glyph metrics");
        }
    }
}
