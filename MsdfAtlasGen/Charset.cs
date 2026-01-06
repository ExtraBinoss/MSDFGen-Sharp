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

        /// <summary>
        /// Creates a charset containing all printable ASCII characters.
        /// </summary>
        private static Charset CreateAsciiCharset()
        {
            var ascii = new Charset();
            for (uint cp = 0x20; cp < 0x7f; ++cp)
                ascii.Add(cp);
            return ascii;
        }

        /// <summary>
        /// Adds a Unicode codepoint to the charset.
        /// </summary>
        public bool Add(uint cp)
        {
            return _codepoints.Add(cp);
        }

        /// <summary>
        /// Removes a Unicode codepoint from the charset.
        /// </summary>
        public void Remove(uint cp)
        {
            _codepoints.Remove(cp);
        }

        public int Count => _codepoints.Count;

        public bool Empty => _codepoints.Count == 0;

        /// <summary>
        /// Returns an enumerator that iterates through the codepoints in the charset.
        /// </summary>
        public IEnumerator<uint> GetEnumerator()
        {
            return _codepoints.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
