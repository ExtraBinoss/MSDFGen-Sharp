using System;
using System.Collections.Generic;
using System.Collections;

namespace MsdfAtlasGen
{
    /// <summary>
    /// Represents a set of Unicode codepoints (characters)
    /// </summary>
    public class Charset : IEnumerable<uint>
    {
        private readonly SortedSet<uint> _codepoints = new SortedSet<uint>();

        public static readonly Charset ASCII = CreateAsciiCharset();

        private static Charset CreateAsciiCharset()
        {
            var ascii = new Charset();
            for (uint cp = 0x20; cp < 0x7f; ++cp)
                ascii.Add(cp);
            return ascii;
        }

        public void Add(uint cp)
        {
            _codepoints.Add(cp);
        }

        public void Remove(uint cp)
        {
            _codepoints.Remove(cp);
        }

        public int Count => _codepoints.Count;

        public bool Empty => _codepoints.Count == 0;

        public IEnumerator<uint> GetEnumerator()
        {
            return _codepoints.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
