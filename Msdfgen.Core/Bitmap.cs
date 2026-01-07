using System;

namespace Msdfgen
{
    /// <summary>
    /// Represents a generic bitmap storage with a specified number of channels.
    /// </summary>
    /// <typeparam name="T">The type of the pixel data (e.g., float, byte).</typeparam>
    public class Bitmap<T>
    {
        /// <summary> The raw pixel data array. </summary>
        public T[] Pixels { get; private set; }
        /// <summary> The width of the bitmap. </summary>
        public int Width { get; private set; }
        /// <summary> The height of the bitmap. </summary>
        public int Height { get; private set; }
        /// <summary> The number of channels per pixel. </summary>
        public int Channels { get; private set; }

        /// <summary>
        /// Initializes a new bitmap with specified dimensions and number of channels.
        /// </summary>
        public Bitmap(int width, int height, int channels = 1)
        {
            Width = width;
            Height = height;
            Channels = channels;
            Pixels = new T[Channels * Width * Height];
        }

        /// <summary>
        /// Gets or sets the pixel value at the specified coordinates and channel.
        /// </summary>
        public T this[int x, int y, int channel = 0]
        {
            get => Pixels[Channels * (Width * y + x) + channel];
            set => Pixels[Channels * (Width * y + x) + channel] = value;
        }
    }
}
