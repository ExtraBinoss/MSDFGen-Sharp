using System;
using System.Globalization;
using Msdfgen;

namespace Msdfgen.Cli
{
    public static class CliParser
    {
        public static CliOptions Parse(string[] args)
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
                        if (argPos + 3 <= args.Length)
                        {
                            options.TestRenderFile = args[argPos++];
                            options.TestRenderWidth = int.Parse(args[argPos++]);
                            options.TestRenderHeight = int.Parse(args[argPos++]);
                        }
                        else
                        {
                            Console.WriteLine("Error: -testrender requires <filename> <width> <height>");
                            return null;
                        }
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
