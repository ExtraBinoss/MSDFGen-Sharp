using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Msdfgen;

namespace Msdfgen.Extensions
{
    public static class ImageSaver
    {
        public static void Save(Bitmap<float> bitmap, string filename)
        {
            // Determine format from filename or assume PNG? User said deduce from extension.
            // ImageSharp handles deduction.
            
            using (var image = new Image<Rgba32>(bitmap.Width, bitmap.Height))
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        // Bitmap<float> is linear float [0..1] (usually).
                        // Need to clamp and convert to byte.
                        // Assuming 3 channels for MSDF, 1 for SDF.
                        
                        float r, g, b, a;
                        
                        // We need to flip Y for image output? 
                        // MSDFGen standard: Origin bottom-left? 
                        // ImageSharp: Origin top-left.
                        // So usually we write buffer[x, height-1-y] to image[x, y] to correct orientation?
                        // Let's assume standard behavior:
                        
                        int srcY = bitmap.Height - 1 - y; 
                        
                        if (bitmap.Channels == 1)
                        {
                            float val = bitmap[x, srcY, 0];
                            byte v = ClampPositive(val * 255.0f);
                            image[x, y] = new Rgba32(v, v, v, 255);
                        }
                        else if (bitmap.Channels == 3)
                        {
                            byte rv = ClampPositive(bitmap[x, srcY, 0] * 255.0f);
                            byte gv = ClampPositive(bitmap[x, srcY, 1] * 255.0f);
                            byte bv = ClampPositive(bitmap[x, srcY, 2] * 255.0f);
                            image[x, y] = new Rgba32(rv, gv, bv, 255);
                        }
                         else if (bitmap.Channels == 4)
                        {
                            byte rv = ClampPositive(bitmap[x, srcY, 0] * 255.0f);
                            byte gv = ClampPositive(bitmap[x, srcY, 1] * 255.0f);
                            byte bv = ClampPositive(bitmap[x, srcY, 2] * 255.0f);
                            byte av = ClampPositive(bitmap[x, srcY, 3] * 255.0f);
                            image[x, y] = new Rgba32(rv, gv, bv, av);
                        }
                    }
                }
                
                image.Save(filename);
            }
        }
        
        private static byte ClampPositive(float v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }
    }
}
