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
        
        public BitmapAtlasStorage(Bitmap<T> bitmap) // Move or copy?
        {
            Bitmap = bitmap;
        }
        
        // Constructor that rearranges (Remap)
        public BitmapAtlasStorage(BitmapAtlasStorage<T> orig, int width, int height, Remap[] remapping)
        {
            Bitmap = new Bitmap<T>(width, height, orig.Bitmap.Channels);
             // Clear bitmap? Array is 0 initialized.
             
             foreach (var remap in remapping)
             {
                 BitmapBlit.Blit(Bitmap, orig.Bitmap, remap.Target.X, remap.Target.Y, remap.Source.X, remap.Source.Y, remap.Width, remap.Height);
             }
        }

        public BitmapAtlasStorage(BitmapAtlasStorage<T> orig, int width, int height)
        {
            Bitmap = new Bitmap<T>(width, height, orig.Bitmap.Channels);
            // Copy (Resize/Crop)
            BitmapBlit.Blit(Bitmap, orig.Bitmap, 0, 0, 0, 0, Math.Min(width, orig.Bitmap.Width), Math.Min(height, orig.Bitmap.Height));
        }

        public void Put<S>(int x, int y, Bitmap<S> subBitmap)
        {
            // Dynamic dispatch or check types?
            if (typeof(T) == typeof(S))
            {
                BitmapBlit.Blit(Bitmap, subBitmap as Bitmap<T>, x, y, 0, 0, subBitmap.Width, subBitmap.Height);
            }
            else if (typeof(T) == typeof(byte) && typeof(S) == typeof(float))
            {
                BitmapBlit.Blit(Bitmap as Bitmap<byte>, subBitmap as Bitmap<float>, x, y, 0, 0, subBitmap.Width, subBitmap.Height);
            }
            else
            {
                throw new NotSupportedException($"Blit from {typeof(S)} to {typeof(T)} not supported.");
            }
        }
        
        public static implicit operator Bitmap<T>(BitmapAtlasStorage<T> storage) => storage.Bitmap;
    }
}
