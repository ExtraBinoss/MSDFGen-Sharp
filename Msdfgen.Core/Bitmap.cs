using System;

namespace Msdfgen
{
    public class Bitmap<T>
    {
        public T[] Pixels { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Channels { get; private set; }

        public Bitmap(int width, int height, int channels = 1)
        {
            Width = width;
            Height = height;
            Channels = channels;
            Pixels = new T[Channels * Width * Height];
        }

        public T this[int x, int y, int channel = 0]
        {
            get => Pixels[Channels * (Width * y + x) + channel];
            set => Pixels[Channels * (Width * y + x) + channel] = value;
        }

        // Helper to get pixel as span or array segment if needed, but for now direct access
    }
}
