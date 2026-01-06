using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SixLabors.Fonts;
using Msdfgen;
using Msdfgen.Extensions;

namespace Msdfgen.Cli
{
using System;
using System.Globalization;

namespace Msdfgen.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; 

            var options = CliParser.Parse(args);
            
            if (options.IsHelp)
            {
                CliParser.PrintHelp();
                return;
            }

            var processor = new CliProcessor();
            processor.Process(options);
        }
    }
}
}
