using System;
using Msdfgen;

namespace MsdfAtlasGen
{
    public interface IAtlasStorage
    {
        // Tagging interface or basic props if needed
    }

    public class BitmapAtlasStorage<T> : IAtlasStorage
    {
        public Bitmap<T> Bitmap { get; private set; }

        public BitmapAtlasStorage(int width, int height, int channels)
        {
             Bitmap = new Bitmap<T>(width, height, channels);
        }
        
        public BitmapAtlasStorage(Bitmap<T> bitmap)
        {
            Bitmap = bitmap;
        }
        
        // Constructor that rearranges (Remap)
        public BitmapAtlasStorage(BitmapAtlasStorage<T> orig, int width, int height, Remap[] remapping)
        {
            Bitmap = new Bitmap<T>(width, height, orig.Bitmap.Channels);
             
             foreach (var remap in remapping)
             {
                 BitmapBlit.Blit(Bitmap, orig.Bitmap, remap.Target.X, remap.Target.Y, remap.Source.X, remap.Source.Y, remap.Width, remap.Height);
             }
        }

        public BitmapAtlasStorage(BitmapAtlasStorage<T> orig, int width, int height)
        {
            Bitmap = new Bitmap<T>(width, height, orig.Bitmap.Channels);
            BitmapBlit.Blit(Bitmap, orig.Bitmap, 0, 0, 0, 0, Math.Min(width, orig.Bitmap.Width), Math.Min(height, orig.Bitmap.Height));
        }

        public void Put<S>(int x, int y, Bitmap<S> subBitmap)
        {
            if (subBitmap is Bitmap<T> src)
            {
                BitmapBlit.Blit(Bitmap, src, x, y, 0, 0, subBitmap.Width, subBitmap.Height);
            }
            else if (Bitmap is Bitmap<byte> dstByte && subBitmap is Bitmap<float> srcFloat)
            {
                BitmapBlit.Blit(dstByte, srcFloat, x, y, 0, 0, subBitmap.Width, subBitmap.Height);
            }
            else
            {
                throw new NotSupportedException($"Blit from {typeof(S)} to {typeof(T)} not supported.");
            }
        }
        
        public static implicit operator Bitmap<T>(BitmapAtlasStorage<T> storage) => storage.Bitmap;
    }
}
