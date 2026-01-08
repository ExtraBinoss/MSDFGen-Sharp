using System;

namespace MsdfAtlasGen.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                CliParser.PrintHelp();
                return 1;
            }

            var config = CliParser.Parse(args);

            if (!config.IsValid)
            {
                Console.Error.WriteLine("[Error] No font file specified. You must provide a font using the '-font' argument.\nUsage Example: MsdfAtlasGen -font MyFont.ttf");
                return 1;
            }

            var runner = new AtlasGeneratorRunner(config);
            return runner.Run();
        }
    }
}
