using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Msdfgen;

namespace MsdfAtlasGen
{
    public class ImmediateAtlasGenerator<T>
    {
        private BitmapAtlasStorage<T> _storage;
        private List<GlyphBox> _layout;
        private GeneratorAttributes _attributes;
        private int _threadCount = 1;
        private readonly GeneratorFunction<T> _generatorFunction;

        /// <summary>
        /// Initializes a new immediate atlas generator with a generator function.
        /// </summary>
        public ImmediateAtlasGenerator(GeneratorFunction<T> generatorFunction)
        {
            _generatorFunction = generatorFunction;
            _layout = new List<GlyphBox>();
            _storage = new BitmapAtlasStorage<T>(0, 0, 3); // Default?
        }

        /// <summary>
        /// Initializes a new immediate atlas generator with specified dimensions and generator function.
        /// </summary>
        public ImmediateAtlasGenerator(int width, int height, GeneratorFunction<T> generatorFunction, int channels = 3)
        {
            _generatorFunction = generatorFunction;
            _layout = new List<GlyphBox>();
            _storage = new BitmapAtlasStorage<T>(width, height, channels);
        }

        /// <summary>
        /// Sets the generator attributes (e.g., range, miter limit).
        /// </summary>
        public void SetAttributes(GeneratorAttributes attributes)
        {
            _attributes = attributes;
        }

        /// <summary>
        /// Sets the number of threads to use for parallel generation.
        /// </summary>
        public void SetThreadCount(int threadCount)
        {
            _threadCount = threadCount;
        }

        /// <summary>
        /// Returns the underlying atlas storage.
        /// </summary>
        public BitmapAtlasStorage<T> AtlasStorage => _storage;

        /// <summary>
        /// Returns the current layout of glyphs in the atlas.
        /// </summary>
        public List<GlyphBox> GetLayout() => _layout;

        /// <summary>
        /// Generates the distance field data for the provided glyphs and stores them in the atlas.
        /// </summary>
        public void Generate(GlyphGeometry[] glyphs, IProgress<double>? progress = null)
        {
            int count = glyphs.Length;
            int maxBoxArea = 0;
            _layout.Clear();
            _layout.Capacity = count;

            for (int i = 0; i < count; ++i)
            {
                var glyph = glyphs[i];
                var box = glyph.ToGlyphBox();
                _layout.Add(box);
                maxBoxArea = Math.Max(maxBoxArea, box.Rect.W * box.Rect.H);
            }
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _threadCount > 0 ? _threadCount : -1 };
            
            int completed = 0;

            Parallel.For(0, count, parallelOptions, (i) =>
            {
                var glyph = glyphs[i];
                if (!glyph.IsWhitespace())
                {
                    glyph.GetBoxRect(out int l, out int b, out int w, out int h);
                    if (w > 0 && h > 0)
                    {
                        var glyphBitmap = new Bitmap<T>(w, h, _storage.Bitmap.Channels);
                        _generatorFunction(glyphBitmap, glyph, _attributes);
                        
                        // Storage Put (blit) needs to be thread safe if writing to overlapping regions or if internal bitmap isn't thread safe.
                        // Bitmap<T>.Pixels is an array. Disjoint writes are safe.
                        // Guillotine packing ensures disjoint regions.
                        
                        _storage.Put(l, b, glyphBitmap);
                    }
                }
                
                int current = System.Threading.Interlocked.Increment(ref completed);
                progress?.Report((double)current / count);
            });
        }
        
        /// <summary>
        /// Rearranges the glyphs in the atlas based on a remapping.
        /// </summary>
        public void Rearrange(int width, int height, Remap[] remapping, int count)
        {
             for (int i = 0; i < count; ++i)
             {
                 var box = _layout[remapping[i].Index];
                 box.Rect.X = remapping[i].Target.X;
                 box.Rect.Y = remapping[i].Target.Y;
                 _layout[remapping[i].Index] = box; // Struct copy update
             }
             
             var newStorage = new BitmapAtlasStorage<T>(_storage, width, height, remapping);
             _storage = newStorage;
        }
        
        /// <summary>
        /// Resizes the atlas storage to the specified dimensions.
        /// </summary>
        public void Resize(int width, int height)
        {
             _storage = new BitmapAtlasStorage<T>(_storage, width, height);
        }
    }
}
