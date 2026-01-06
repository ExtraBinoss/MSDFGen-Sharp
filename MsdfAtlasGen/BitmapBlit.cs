using System;
using Msdfgen;

namespace MsdfAtlasGen
{
    public static class BitmapBlit
    {
        public static void Blit<T>(Bitmap<T> dst, Bitmap<T> src, int dx, int dy, int sx, int sy, int w, int h)
        {
            if (dst == null || src == null) return;
            
            // Clip logic
            if (dx < 0) { w += dx; sx -= dx; dx = 0; }
            if (dy < 0) { h += dy; sy -= dy; dy = 0; }
            if (sx < 0) { w += sx; dx -= sx; sx = 0; }
            if (sy < 0) { h += sy; dy -= sy; sy = 0; }
            
            if (w <= 0 || h <= 0) return;
            
            w = Math.Min(w, Math.Min(dst.Width - dx, src.Width - sx));
            h = Math.Min(h, Math.Min(dst.Height - dy, src.Height - sy));
            
            if (w <= 0 || h <= 0) return;

            int channels = dst.Channels; // Assumes src has same channels if T match
            if (src.Channels != channels) throw new ArgumentException("Channels mismatch");

            for (int y = 0; y < h; ++y)
            {
                int dstIndex = channels * ((dy + y) * dst.Width + dx);
                int srcIndex = channels * ((sy + y) * src.Width + sx);
                Array.Copy(src.Pixels, srcIndex, dst.Pixels, dstIndex, w * channels);
            }
        }

        public static void Blit(Bitmap<byte> dst, Bitmap<float> src, int dx, int dy, int sx, int sy, int w, int h)
        {
            if (dst == null || src == null) return;

             // Clip logic
            if (dx < 0) { w += dx; sx -= dx; dx = 0; }
            if (dy < 0) { h += dy; sy -= dy; dy = 0; }
            if (sx < 0) { w += sx; dx -= sx; sx = 0; }
            if (sy < 0) { h += sy; dy -= sy; sy = 0; }
            
            if (w <= 0 || h <= 0) return;
            
            w = Math.Min(w, Math.Min(dst.Width - dx, src.Width - sx));
            h = Math.Min(h, Math.Min(dst.Height - dy, src.Height - sy));

            if (w <= 0 || h <= 0) return;
            
            int channels = dst.Channels;
            if (src.Channels != channels) throw new ArgumentException("Channels mismatch");

            for (int y = 0; y < h; ++y)
            {
                int dstRowStart = channels * ((dy + y) * dst.Width + dx);
                int srcRowStart = channels * ((sy + y) * src.Width + sx);
                
                for (int i = 0; i < w * channels; ++i)
                {
                    dst.Pixels[dstRowStart + i] = PixelFloatToByte(src.Pixels[srcRowStart + i]);
                }
            }
        }

        private static byte PixelFloatToByte(float x)
        {
            return (byte)Math.Min(255, Math.Max(0, x * 255.0f + 0.5f));
        }
    }
}
