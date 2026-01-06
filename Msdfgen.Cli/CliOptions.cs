using System;
using Msdfgen;

namespace Msdfgen.Cli
{
    public class CliOptions
    {
        public enum MsdfMode
        {
            SDF,
            PSDF,
            MSDF,
            MTSDF
        }

        // Mode
        public MsdfMode Mode { get; set; } = MsdfMode.MSDF;

        // Input
        public string? FontFile { get; set; }
        public char CharCode { get; set; }
        public string? ShapeDescFile { get; set; }
        public string? ShapeDesc { get; set; }

        // Output
        public string OutputFile { get; set; } = "output.png";
        public int Width { get; set; } = 64;
        public int Height { get; set; } = 64;

        // Generator Config
        public double PxRange { get; set; } = 2.0;
        public Vector2 Scale { get; set; } = new Vector2(1.0, 1.0);
        public Vector2 Translate { get; set; } = new Vector2(0.0, 0.0);
        public double AngleThreshold { get; set; } = 3.0;

        // Flags
        public bool AutoFrame { get; set; } = false;
        public bool ScaleSpecified { get; set; } = false;
        public bool PrintMetrics { get; set; } = false;

        // Extras
        public string? ExportShapeFile { get; set; }
        public string? TestRenderFile { get; set; }
        public int TestRenderWidth { get; set; } = 512;
        public int TestRenderHeight { get; set; } = 512;
        public bool TestRenderSpecified { get; set; } = false;
        
        public bool IsHelp { get; set; } = false;
        public bool OutputFileSpecified { get; set; } = false;
    }
}
