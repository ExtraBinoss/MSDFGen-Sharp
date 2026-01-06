using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Msdfgen;

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

            using var image = new Image<Rgba32>(bitmap.Width, bitmap.Height);
            
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    // Flip Y for image formats (top-down vs bottom-up)
                    int srcY = bitmap.Height - 1 - y;

                    if (bitmap.Channels == 1)
                    {
                        float v = bitmap[x, srcY, 0];
                        byte b = Clamp(v * 255.0f);
                        image[x, y] = new Rgba32(b, b, b, 255);
                    }
                    else if (bitmap.Channels == 3)
                    {
                        byte r = Clamp(bitmap[x, srcY, 0] * 255.0f);
                        byte g = Clamp(bitmap[x, srcY, 1] * 255.0f);
                        byte b = Clamp(bitmap[x, srcY, 2] * 255.0f);
                        image[x, y] = new Rgba32(r, g, b, 255);
                    }
                    else if (bitmap.Channels == 4)
                    {
                        byte r = Clamp(bitmap[x, srcY, 0] * 255.0f);
                        byte g = Clamp(bitmap[x, srcY, 1] * 255.0f);
                        byte b = Clamp(bitmap[x, srcY, 2] * 255.0f);
                        byte a = Clamp(bitmap[x, srcY, 3] * 255.0f);
                        image[x, y] = new Rgba32(r, g, b, a);
                    }
                }
            }

            image.Save(filename);
        }

        private static byte Clamp(float v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }
    }
}
