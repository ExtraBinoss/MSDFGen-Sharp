using System;
using System.Runtime.InteropServices;
using Msdfgen;
using FreeTypeSharp;
using static FreeTypeSharp.FT;

namespace Msdfgen.Extensions
{
    /// <summary>
    /// Represents a FreeType library handle. Wrapper around FT_Library.
    /// </summary>
    public unsafe class FreetypeHandle : IDisposable
    {
        internal FT_LibraryRec_* Library { get; private set; }

        private FreetypeHandle(FT_LibraryRec_* library)
        {
            Library = library;
        }

        /// <summary>
        /// Initializes the FreeType library.
        /// </summary>
        public static FreetypeHandle? Initialize()
        {
            FT_LibraryRec_* library;
            var error = FT_Init_FreeType(&library);
            if (error != FreeTypeSharp.FT_Error.FT_Err_Ok)
                return null;
            return new FreetypeHandle(library);
        }

        public void Dispose()
        {
            if (Library != null)
            {
                FT_Done_FreeType(Library);
                Library = null;
            }
        }
    }

    /// <summary>
    /// Represents a font handle. Wrapper around FT_Face.
    /// </summary>
    public unsafe class FontHandle : IDisposable
    {
        internal FT_FaceRec_* Face { get; private set; }
        private bool _ownership;

        private FontHandle(FT_FaceRec_* face, bool ownership)
        {
            Face = face;
            _ownership = ownership;
        }

        /// <summary>
        /// Loads a font file and returns its handle.
        /// </summary>
        public static FontHandle? LoadFont(FreetypeHandle library, string filename)
        {
            if (library == null || library.Library == null)
                return null;

            FT_FaceRec_* face;
            var filenamePtr = Marshal.StringToHGlobalAnsi(filename);
            try
            {
                var error = FT_New_Face(library.Library, (byte*)filenamePtr, IntPtr.Zero, &face);
                if (error != FreeTypeSharp.FT_Error.FT_Err_Ok)
                    return null;

                return new FontHandle(face, true);
            }
            finally
            {
                Marshal.FreeHGlobal(filenamePtr);
            }
        }

        /// <summary>
        /// Loads a font from binary data and returns its handle.
        /// </summary>
        public static FontHandle? LoadFontData(FreetypeHandle library, byte[] data)
        {
            if (library == null || library.Library == null)
                return null;

            FT_FaceRec_* face;
            fixed (byte* dataPtr = data)
            {
                var error = FT_New_Memory_Face(library.Library, dataPtr, (IntPtr)data.Length, IntPtr.Zero, &face);
                if (error != FreeTypeSharp.FT_Error.FT_Err_Ok)
                    return null;

                return new FontHandle(face, true);
            }
        }

        /// <summary>
        /// Creates a FontHandle from FT_Face that was loaded by the user.
        /// Dispose must still be called but will not affect the FT_Face.
        /// </summary>
        public static FontHandle AdoptFreetypeFont(FT_FaceRec_* ftFace)
        {
            return new FontHandle(ftFace, false);
        }

        public void Dispose()
        {
            if (_ownership && Face != null)
            {
                FT_Done_Face(Face);
            }
            Face = null;
        }
    }

    /// <summary>
    /// Represents a glyph index within a font.
    /// </summary>
    public struct GlyphIndex
    {
        private uint _index;

        public GlyphIndex(uint index)
        {
            _index = index;
        }

        public uint Index => _index;

        public static implicit operator uint(GlyphIndex glyphIndex) => glyphIndex._index;
        public static implicit operator GlyphIndex(uint index) => new GlyphIndex(index);
    }

    /// <summary>
    /// Global metrics of a typeface (in font units).
    /// </summary>
    public struct FontMetrics
    {
        /// <summary>The size of one EM.</summary>
        public double EmSize;
        /// <summary>The vertical position of the ascender and descender relative to the baseline.</summary>
        public double AscenderY;
        public double DescenderY;
        /// <summary>The vertical difference between consecutive baselines.</summary>
        public double LineHeight;
        /// <summary>The vertical position and thickness of the underline.</summary>
        public double UnderlineY;
        public double UnderlineThickness;
    }

    /// <summary>
    /// The scaling applied to font glyph coordinates when loading a glyph.
    /// </summary>
    public enum FontCoordinateScaling
    {
        /// <summary>The coordinates are kept as the integer values native to the font file.</summary>
        None,
        /// <summary>The coordinates will be normalized to the em size, i.e. 1 = 1 em.</summary>
        EmNormalized,
        /// <summary>The incorrect legacy version that was in effect before version 1.12, coordinate values are divided by 64 - DO NOT USE - for backwards compatibility only.</summary>
        Legacy
    }

    /// <summary>
    /// Font loading utilities using FreeType. This is a direct port of msdfgen's import-font functionality.
    /// </summary>
    public static unsafe class FontLoader
    {
        private const double LEGACY_FONT_COORDINATE_SCALE = 1.0 / 64.0;
        private const double DEFAULT_FONT_UNITS_PER_EM = 2048.0;

        private static double GetFontCoordinateScale(FT_FaceRec_* face, FontCoordinateScaling coordinateScaling)
        {
            switch (coordinateScaling)
            {
                case FontCoordinateScaling.None:
                    return 1.0;
                case FontCoordinateScaling.EmNormalized:
                    return 1.0 / (face->units_per_EM != 0 ? face->units_per_EM : 1);
                case FontCoordinateScaling.Legacy:
                    return LEGACY_FONT_COORDINATE_SCALE;
                default:
                    return 1.0;
            }
        }

        /// <summary>
        /// Outputs the metrics of a font.
        /// </summary>
        public static bool GetFontMetrics(out FontMetrics metrics, FontHandle font, FontCoordinateScaling coordinateScaling = FontCoordinateScaling.None)
        {
            metrics = new FontMetrics();
            if (font == null || font.Face == null)
                return false;

            FT_FaceRec_* face = font.Face;
            double scale = GetFontCoordinateScale(face, coordinateScaling);

            metrics.EmSize = scale * face->units_per_EM;
            metrics.AscenderY = scale * face->ascender;
            metrics.DescenderY = scale * face->descender;
            metrics.LineHeight = scale * face->height;
            metrics.UnderlineY = scale * face->underline_position;
            metrics.UnderlineThickness = scale * face->underline_thickness;

            return true;
        }

        /// <summary>
        /// Outputs the width of the space and tab characters.
        /// </summary>
        public static bool GetFontWhitespaceWidth(out double spaceAdvance, out double tabAdvance, FontHandle font, FontCoordinateScaling coordinateScaling = FontCoordinateScaling.None)
        {
            spaceAdvance = 0;
            tabAdvance = 0;

            if (font == null || font.Face == null)
                return false;

            FT_FaceRec_* face = font.Face;
            double scale = GetFontCoordinateScale(face, coordinateScaling);

            var error = FT_Load_Char(face, (UIntPtr)' ', FreeTypeSharp.FT_LOAD.FT_LOAD_NO_SCALE);
            if (error != FreeTypeSharp.FT_Error.FT_Err_Ok)
                return false;
            spaceAdvance = scale * (long)face->glyph->advance.x;

            error = FT_Load_Char(face, (UIntPtr)'\t', FreeTypeSharp.FT_LOAD.FT_LOAD_NO_SCALE);
            if (error != FreeTypeSharp.FT_Error.FT_Err_Ok)
                return false;
            tabAdvance = scale * (long)face->glyph->advance.x;

            return true;
        }

        public static bool GetGlyphCount(out uint count, FontHandle font)
        {
            count = 0;
            if (font == null || font.Face == null)
                return false;

            FT_FaceRec_* face = font.Face;
            count = (uint)(long)face->num_glyphs;
            return true;
        }

        /// <summary>
        /// Retrieves all Unicode codepoints supported by the font.
        /// </summary>
        public static bool GetAvailableCodepoints(out System.Collections.Generic.List<uint> codepoints, FontHandle font)
        {
            codepoints = [];
            if (font == null || font.Face == null)
                return false;

            FT_FaceRec_* face = font.Face;
            uint glyphIndex;
            ulong charCode = FT_Get_First_Char(face, &glyphIndex);

            while (glyphIndex != 0)
            {
                codepoints.Add((uint)charCode);
                charCode = FT_Get_Next_Char(face, checked((UIntPtr)charCode), &glyphIndex);
            }

            return true;
        }

        public static bool HasKerningInfo(FontHandle font)
        {
            if (font == null || font.Face == null)
                return false;
            
            // FT_FACE_FLAG_KERNING = 64 (1 << 6)
            return ((long)font.Face->face_flags & 64) != 0;
        }

        /// <summary>
        /// Outputs the glyph index corresponding to the specified Unicode character.
        /// </summary>
        public static bool GetGlyphIndex(out GlyphIndex glyphIndex, FontHandle font, uint unicode)
        {
            glyphIndex = new GlyphIndex(0);
            if (font == null || font.Face == null)
                return false;

            uint index = FT_Get_Char_Index(font.Face, (UIntPtr)unicode);
            glyphIndex = new GlyphIndex(index);
            return index != 0;
        }

        /// <summary>
        /// Loads the geometry of a glyph from a font by glyph index.
        /// </summary>
        public static bool LoadGlyph(Shape output, FontHandle font, GlyphIndex glyphIndex, FontCoordinateScaling coordinateScaling, out double advance)
        {
            advance = 0;
            if (font == null || font.Face == null)
                return false;

            var error = FT_Load_Glyph(font.Face, glyphIndex.Index, FreeTypeSharp.FT_LOAD.FT_LOAD_NO_SCALE);
            if (error != FreeTypeSharp.FT_Error.FT_Err_Ok)
                return false;

            FT_FaceRec_* face = font.Face;
            double scale = GetFontCoordinateScale(face, coordinateScaling);
            advance = scale * (long)face->glyph->advance.x;

            return ReadFreetypeOutline(output, &face->glyph->outline, scale);
        }

        /// <summary>
        /// Loads the geometry of a glyph from a font by Unicode codepoint.
        /// </summary>
        public static bool LoadGlyph(Shape output, FontHandle font, uint unicode, FontCoordinateScaling coordinateScaling, out double advance)
        {
            advance = 0;
            if (font == null || font.Face == null)
                return false;

            uint glyphIndex = FT_Get_Char_Index(font.Face, (UIntPtr)unicode);
            return LoadGlyph(output, font, new GlyphIndex(glyphIndex), coordinateScaling, out advance);
        }

        /// <summary>
        /// Outputs the kerning distance adjustment between two specific glyphs.
        /// </summary>
        public static bool GetKerning(out double kerning, FontHandle font, GlyphIndex glyphIndex0, GlyphIndex glyphIndex1, FontCoordinateScaling coordinateScaling = FontCoordinateScaling.None)
        {
            kerning = 0;
            if (font == null || font.Face == null)
                return false;

            FT_Vector_ kernVector;
            var error = FT_Get_Kerning(font.Face, glyphIndex0.Index, glyphIndex1.Index, FreeTypeSharp.FT_Kerning_Mode_.FT_KERNING_UNSCALED, &kernVector);
            if (error != FreeTypeSharp.FT_Error.FT_Err_Ok)
                return false;

            double scale = GetFontCoordinateScale(font.Face, coordinateScaling);
            kerning = scale * (long)kernVector.x;
            return true;
        }

        /// <summary>
        /// Outputs the kerning distance adjustment between two specific Unicode codepoints.
        /// </summary>
        public static bool GetKerning(out double kerning, FontHandle font, uint unicode0, uint unicode1, FontCoordinateScaling coordinateScaling = FontCoordinateScaling.None)
        {
            kerning = 0;
            if (font == null || font.Face == null)
                return false;

            uint glyphIndex0 = FT_Get_Char_Index(font.Face, (UIntPtr)unicode0);
            uint glyphIndex1 = FT_Get_Char_Index(font.Face, (UIntPtr)unicode1);
            return GetKerning(out kerning, font, new GlyphIndex(glyphIndex0), new GlyphIndex(glyphIndex1), coordinateScaling);
        }

        /// <summary>
        /// Converts the geometry of FreeType's FT_Outline to a Shape object.
        /// This is a direct port of the C++ implementation.
        /// </summary>
        private static bool ReadFreetypeOutline(Shape output, FT_Outline_* outline, double scale)
        {
            output.Contours.Clear();
            output.SetYAxisOrientation(YAxisOrientation.Upward);

            var context = new FtContext
            {
                Scale = scale,
                Shape = output,
                Position = new Vector2(0, 0)
            };

            // FreeType outline decomposition
            FT_Outline_Funcs_ funcs = new FT_Outline_Funcs_
            {
                move_to = (void*)Marshal.GetFunctionPointerForDelegate<FT_Outline_MoveToFunc>(FtMoveTo),
                line_to = (void*)Marshal.GetFunctionPointerForDelegate<FT_Outline_LineToFunc>(FtLineTo),
                conic_to = (void*)Marshal.GetFunctionPointerForDelegate<FT_Outline_ConicToFunc>(FtConicTo),
                cubic_to = (void*)Marshal.GetFunctionPointerForDelegate<FT_Outline_CubicToFunc>(FtCubicTo),
                shift = 0,
                delta = IntPtr.Zero
            };

            // Pin the context for the callback
            GCHandle contextHandle = GCHandle.Alloc(context);
            try
            {
                var contextPtr = GCHandle.ToIntPtr(contextHandle);
                var error = FT_Outline_Decompose(outline, &funcs, (void*)contextPtr);
                
                // Remove empty last contour if present
                if (output.Contours.Count > 0 && output.Contours[output.Contours.Count - 1].Edges.Count == 0)
                {
                    output.Contours.RemoveAt(output.Contours.Count - 1);
                }

                return error == FreeTypeSharp.FT_Error.FT_Err_Ok;
            }
            finally
            {
                contextHandle.Free();
            }
        }

        private class FtContext
        {
            public double Scale;
            public Vector2 Position;
            public Shape? Shape;
            public Contour? Contour;
        }

        private static Vector2 FtPoint2(FT_Vector_ vector, double scale)
        {
            return new Vector2(scale * (long)vector.x, scale * (long)vector.y);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int FT_Outline_MoveToFunc(FT_Vector_* to, void* user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int FT_Outline_LineToFunc(FT_Vector_* to, void* user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int FT_Outline_ConicToFunc(FT_Vector_* control, FT_Vector_* to, void* user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int FT_Outline_CubicToFunc(FT_Vector_* control1, FT_Vector_* control2, FT_Vector_* to, void* user);

        private static int FtMoveTo(FT_Vector_* to, void* user)
        {
            var contextPtr = new IntPtr(user);
            var context = (FtContext)GCHandle.FromIntPtr(contextPtr).Target!;
            if (!(context.Contour != null && context.Contour.Edges.Count == 0))
            {
                context.Contour = new Contour();
                context.Shape!.AddContour(context.Contour);
            }
            context.Position = FtPoint2(*to, context.Scale);
            return 0;
        }

        private static int FtLineTo(FT_Vector_* to, void* user)
        {
            var contextPtr = new IntPtr(user);
            var context = (FtContext)GCHandle.FromIntPtr(contextPtr).Target!;
            Vector2 endpoint = FtPoint2(*to, context.Scale);
            if (endpoint != context.Position)
            {
                context.Contour!.AddEdge(new LinearSegment(context.Position, endpoint));
                context.Position = endpoint;
            }
            return 0;
        }

        private static int FtConicTo(FT_Vector_* control, FT_Vector_* to, void* user)
        {
            var contextPtr = new IntPtr(user);
            var context = (FtContext)GCHandle.FromIntPtr(contextPtr).Target!;
            Vector2 endpoint = FtPoint2(*to, context.Scale);
            if (endpoint != context.Position)
            {
                context.Contour!.AddEdge(new QuadraticSegment(context.Position, FtPoint2(*control, context.Scale), endpoint));
                context.Position = endpoint;
            }
            return 0;
        }

        private static int FtCubicTo(FT_Vector_* control1, FT_Vector_* control2, FT_Vector_* to, void* user)
        {
            var contextPtr = new IntPtr(user);
            var context = (FtContext)GCHandle.FromIntPtr(contextPtr).Target!;
            Vector2 endpoint = FtPoint2(*to, context.Scale);
            Vector2 c1 = FtPoint2(*control1, context.Scale);
            Vector2 c2 = FtPoint2(*control2, context.Scale);
            
            // Check if endpoint is different or if control points define a valid curve
            if (endpoint != context.Position || Vector2.CrossProduct(c1 - endpoint, c2 - endpoint) != 0)
            {
                context.Contour!.AddEdge(new CubicSegment(context.Position, c1, c2, endpoint));
                context.Position = endpoint;
            }
            return 0;
        }
    }
}
