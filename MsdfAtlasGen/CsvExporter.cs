using System;
using System.IO;
using Msdfgen;

namespace MsdfAtlasGen
{
    public static class CsvExporter
    {
        public static void Export(FontGeometry[] fonts, int atlasWidth, int atlasHeight, YAxisOrientation yDirection, string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                for (int i = 0; i < fonts.Length; ++i)
                {
                    var font = fonts[i];
                    foreach (var glyph in font.GetGlyphs())
                    {
                        if (fonts.Length > 1)
                            writer.Write($"{i},");
                        
                        // Identifier
                         if (font.GetPreferredIdentifierType() == GlyphIdentifierType.GlyphIndex)
                            writer.Write($"{glyph.GetIndex()},");
                        else
                            writer.Write($"{glyph.GetCodepoint()},");
                        
                        writer.Write($"{glyph.GetAdvance():G17},");

                        glyph.GetQuadPlaneBounds(out double l, out double b, out double r, out double t);
                        if (yDirection == YAxisOrientation.Downward)
                            writer.Write($"{l:G17},{-t:G17},{r:G17},{-b:G17},");
                        else
                            writer.Write($"{l:G17},{b:G17},{r:G17},{t:G17},");

                         glyph.GetQuadAtlasBounds(out double al, out double ab, out double ar, out double at);
                        if (yDirection == YAxisOrientation.Downward)
                            writer.WriteLine($"{al:G17},{atlasHeight - at:G17},{ar:G17},{atlasHeight - ab:G17}");
                        else
                            writer.WriteLine($"{al:G17},{ab:G17},{ar:G17},{at:G17}");
                    }
                }
            }
        }
    }
}
