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

        public ImmediateAtlasGenerator(GeneratorFunction<T> generatorFunction)
        {
            _generatorFunction = generatorFunction;
            _layout = new List<GlyphBox>();
            _storage = new BitmapAtlasStorage<T>(0, 0, 3); // Default?
        }

        public ImmediateAtlasGenerator(int width, int height, GeneratorFunction<T> generatorFunction, int channels = 3)
        {
            _generatorFunction = generatorFunction;
            _layout = new List<GlyphBox>();
            _storage = new BitmapAtlasStorage<T>(width, height, channels);
        }

        // Additional constructors for storage args... 
        // For simplicity, assuming BitmapAtlasStorage is the storage pattern.

        public void SetAttributes(GeneratorAttributes attributes)
        {
            _attributes = attributes;
        }

        public void SetThreadCount(int threadCount)
        {
            _threadCount = threadCount;
        }

        public BitmapAtlasStorage<T> AtlasStorage => _storage;
        public List<GlyphBox> GetLayout() => _layout;

        public void Generate(GlyphGeometry[] glyphs)
        {
            int count = glyphs.Length;
            int maxBoxArea = 0;
            _layout.Clear();
            _layout.Capacity = count;

            for (int i = 0; i < count; ++i)
            {
                var glyph = glyphs[i];
                var box = glyph.ToGlyphBox(); // Assuming helper I added
                _layout.Add(box);
                maxBoxArea = Math.Max(maxBoxArea, box.Rect.W * box.Rect.H);
            }
            
            // Buffer management
            // C# GC handles small allocations well, but we can reuse if we want.
            // For now, per-thread allocation is simple.
            // Or parallel loop with ThreadLocal buffer.
            
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _threadCount > 0 ? _threadCount : -1 };
            
            Parallel.For(0, count, parallelOptions, (i) =>
            {
                var glyph = glyphs[i];
                if (!glyph.IsWhitespace())
                {
                    glyph.GetBoxRect(out int l, out int b, out int w, out int h);
                    if (w > 0 && h > 0)
                    {
                        // Allocate bitmap for this glyph
                        // We need to know channels. _storage.Bitmap.Channels
                        var glyphBitmap = new Bitmap<T>(w, h, _storage.Bitmap.Channels);
                        _generatorFunction(glyphBitmap, glyph, _attributes);
                        
                        // Storage Put (blit) needs to be thread safe if writing to overlapping regions or if internal bitmap isn't thread safe.
                        // Bitmap<T>.Pixels is an array. Disjoint writes are safe.
                        // Guillotine packing ensures disjoint regions.
                        
                        _storage.Put(l, b, glyphBitmap);
                    }
                }
            });
        }
        
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
        
        public void Resize(int width, int height)
        {
             // Resize usually implies copying existing? Or clearing?
             // C++ Resize: AtlasStorage newStorage((AtlasStorage &&) storage, width, height);
             // BitmapAtlasStorage ctor copies/blits.
             
             // I'll assume we copy existing content (corner 0,0?)
             // Actually C++ generic AtlasStorage ctor(orig, w, h) implementation:
             // blit(bitmap, orig.bitmap, 0, 0, 0, 0, min(w, orig.w), min(h, orig.h));
             // Similar to 'resize' in image editors (crop/extend).
             
             // BitmapAtlasStorage<T> doesn't have that constructor in my C# interface yet.
             // I only implemented (width, height, channels) and (orig, remapping).
             // I need to add (orig, width, height) constructor to BitmapAtlasStorage.cs.
        }
    }
}
