using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Msdfgen;

using SixLabors.ImageSharp.Formats.Png;
using System.Threading.Tasks;

namespace MsdfAtlasGen.Cli
{
    /// <summary>
    /// Saves atlas bitmaps to image files.
    /// </summary>
    public static class AtlasSaver
    {
        public static void SaveAtlas(Bitmap<float> bitmap, string filename)
        {
            // Ensure output directory exists
            string? dir = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            int width = bitmap.Width;
            int height = bitmap.Height;
            int channels = bitmap.Channels;
            var msdfPixels = bitmap.Pixels;
            
            // Create a buffer for Rgba32 pixels
            Rgba32[] pixelBuffer = new Rgba32[width * height];

            // Parallelize buffer filling
            Parallel.For(0, height, y =>
            {
                // ImageSharp is top-down, MSDFGen bitmap is bottom-up usually.
                int srcY = height - 1 - y;
                int srcRowOffset = channels * (width * srcY);
                int dstRowOffset = width * y;

                if (channels == 1)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float v = msdfPixels[srcRowOffset + x];
                        byte b = Clamp(v * 255.0f);
                        pixelBuffer[dstRowOffset + x] = new Rgba32(b, b, b, 255);
                    }
                }
                else if (channels == 3)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcOffset = srcRowOffset + (x * 3);
                        byte r = Clamp(msdfPixels[srcOffset] * 255.0f);
                        byte g = Clamp(msdfPixels[srcOffset + 1] * 255.0f);
                        byte b = Clamp(msdfPixels[srcOffset + 2] * 255.0f);
                        pixelBuffer[dstRowOffset + x] = new Rgba32(r, g, b, 255);
                    }
                }
                else if (channels == 4)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcOffset = srcRowOffset + (x * 4);
                        byte r = Clamp(msdfPixels[srcOffset] * 255.0f);
                        byte g = Clamp(msdfPixels[srcOffset + 1] * 255.0f);
                        byte b = Clamp(msdfPixels[srcOffset + 2] * 255.0f);
                        byte a = Clamp(msdfPixels[srcOffset + 3] * 255.0f);
                        pixelBuffer[dstRowOffset + x] = new Rgba32(r, g, b, a);
                    }
                }
            });

            // Load from buffer and save
            using var image = Image.LoadPixelData<Rgba32>(pixelBuffer, width, height);
            
            // Fast PNG encoding
            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestSpeed,
                FilterMethod = PngFilterMethod.None
            };

            image.SaveAsPng(filename, encoder);
        }

        private static byte Clamp(float v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }
    }
}
