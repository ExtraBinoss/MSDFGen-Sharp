using System;
using System.IO;
using System.Text;
using System.Xml;
using Msdfgen;

namespace MsdfAtlasGen
{
    /// <summary>
    /// Exports font atlas data to BMFont XML format (.fnt)
    /// </summary>
    public static class FntExporter
    {
        public static void Export(
            FontGeometry[] fonts,
            ImageType imageType,
            int atlasWidth,
            int atlasHeight,
            double fontSize,
            double distanceRange,
            string pngFilename,
            string outputFilename,
            YAxisOrientation yDirection = YAxisOrientation.Downward,
            Padding? outerPixelPadding = null)
        {
            if (fonts.Length == 0) return;

            var font = fonts[0]; // For now, just use first font
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\r\n",
                Encoding = Encoding.UTF8
            };

            using var stream = new FileStream(outputFilename, FileMode.Create);
            using var writer = XmlWriter.Create(stream, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("font");

            // <info> element
            var metrics = font.GetMetrics();
            writer.WriteStartElement("info");
            writer.WriteAttributeString("face", font.GetName() ?? "Unknown");
            writer.WriteAttributeString("size", ((int)fontSize).ToString());
            writer.WriteAttributeString("bold", "0");
            writer.WriteAttributeString("italic", "0");
            writer.WriteAttributeString("charset", "");
            writer.WriteAttributeString("unicode", "1");
            writer.WriteAttributeString("stretchH", "100");
            writer.WriteAttributeString("smooth", "1");
            writer.WriteAttributeString("aa", "1");
            // Padding in FNT is the atlas padding used during generation
            int infoPadding;
            if (outerPixelPadding.HasValue)
            {
                // Use the outer pixel padding that was applied
                Padding pad = outerPixelPadding.Value;
                int padL = (int)pad.L;
                int padR = (int)pad.R;
                int padT = (int)pad.T;
                int padB = (int)pad.B;
                infoPadding = (padL + padR + padT + padB) / 4; // Average of all sides
            }
            else
            {
                infoPadding = (int)(distanceRange / 2.0) + 2; // Fallback: Range/2 + outer padding
            }
            writer.WriteAttributeString("padding", $"{infoPadding},{infoPadding},{infoPadding},{infoPadding}");
            writer.WriteAttributeString("spacing", "0,0");
            writer.WriteAttributeString("outline", "0");
            writer.WriteEndElement();

            // <common> element
            writer.WriteStartElement("common");
            writer.WriteAttributeString("lineHeight", ((int)(metrics.LineHeight * fontSize / metrics.EmSize)).ToString());
            writer.WriteAttributeString("base", ((int)(metrics.AscenderY * fontSize / metrics.EmSize)).ToString());
            writer.WriteAttributeString("scaleW", atlasWidth.ToString());
            writer.WriteAttributeString("scaleH", atlasHeight.ToString());
            writer.WriteAttributeString("pages", "1");
            writer.WriteAttributeString("packed", "0");
            writer.WriteAttributeString("alphaChnl", "0");
            writer.WriteAttributeString("redChnl", "0");
            writer.WriteAttributeString("greenChnl", "0");
            writer.WriteAttributeString("blueChnl", "0");
            writer.WriteEndElement();

            // <pages> element
            writer.WriteStartElement("pages");
            writer.WriteStartElement("page");
            writer.WriteAttributeString("id", "0");
            writer.WriteAttributeString("file", Path.GetFileName(pngFilename));
            writer.WriteEndElement();
            writer.WriteEndElement();

            // <distanceField> element (MSDF specific)
            if (imageType == ImageType.Sdf || imageType == ImageType.Psdf || 
                imageType == ImageType.Msdf || imageType == ImageType.Mtsdf)
            {
                writer.WriteStartElement("distanceField");
                string fieldType = imageType switch
                {
                    ImageType.Sdf => "sdf",
                    ImageType.Psdf => "psdf",
                    ImageType.Msdf => "msdf",
                    ImageType.Mtsdf => "mtsdf",
                    _ => "sdf"
                };
                writer.WriteAttributeString("fieldType", fieldType);
                writer.WriteAttributeString("distanceRange", ((int)distanceRange).ToString());
                writer.WriteEndElement();
            }

            // <chars> element
            var glyphs = font.GetGlyphs().Glyphs;
            int count = 0;
            foreach (var _ in glyphs) count++;

            writer.WriteStartElement("chars");
            writer.WriteAttributeString("count", count.ToString());

            foreach (var glyph in font.GetGlyphs().Glyphs)
            {
                glyph.GetQuadAtlasBounds(out double al, out double ab, out double ar, out double at);
                glyph.GetQuadPlaneBounds(out double pl, out double pb, out double pr, out double pt);
                
                int x = (int)Math.Round(al);
                int y, height;
                
                if (yDirection == YAxisOrientation.Downward)
                {
                    y = (int)Math.Round(atlasHeight - at);
                    height = (int)Math.Round(at - ab);
                }
                else
                {
                    y = (int)Math.Round(ab);
                    height = (int)Math.Round(at - ab);
                }
                
                int width = (int)Math.Round(ar - al);
                
                // BMFont format:
                // xoffset: offset from current cursor position to left edge of glyph (in pixels)
                // yoffset: offset from baseline to top of glyph
                // xadvance: horizontal advance (distance to move cursor after rendering)
                
                // pl, pb, pr, pt are in scaled EM units from GetQuadPlaneBounds
                // metrics values are also scaled from FontGeometry.LoadMetrics
                // After scaling, EmSize becomes 1.0
                
                // xoffset and yoffset are already in the right scale
                int xoffset = (int)Math.Round(pl);
                int yoffset = (int)Math.Round(metrics.AscenderY - pt);
                
                // xadvance: GetAdvance() returns scaled EM units (1 unit = 1 pixel at fontSize)
                // Since metrics.EmSize is 1.0 after scaling, we just round it
                int xadvance = (int)Math.Round(glyph.GetAdvance());

                writer.WriteStartElement("char");
                writer.WriteAttributeString("id", glyph.GetCodepoint().ToString());
                writer.WriteAttributeString("index", glyph.GetIndex().ToString());
                
                // Write char attribute with XML escaping for special chars
                char c = (char)glyph.GetCodepoint();
                if (c == '"') 
                    writer.WriteAttributeString("char", "&quot;");
                else if (c == '<')
                    writer.WriteAttributeString("char", "&lt;");
                else if (c == '>')
                    writer.WriteAttributeString("char", "&gt;");
                else if (c == '&')
                    writer.WriteAttributeString("char", "&amp;");
                else if (char.IsControl(c) || c == ' ')
                    writer.WriteAttributeString("char", " ");
                else
                    writer.WriteAttributeString("char", c.ToString());

                writer.WriteAttributeString("width", width.ToString());
                writer.WriteAttributeString("height", height.ToString());
                writer.WriteAttributeString("xoffset", xoffset.ToString());
                writer.WriteAttributeString("yoffset", yoffset.ToString());
                writer.WriteAttributeString("xadvance", xadvance.ToString());
                writer.WriteAttributeString("chnl", "15");
                writer.WriteAttributeString("x", x.ToString());
                writer.WriteAttributeString("y", y.ToString());
                writer.WriteAttributeString("page", "0");
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // chars

            // <kernings> element (empty for now)
            writer.WriteStartElement("kernings");
            writer.WriteAttributeString("count", "0");
            writer.WriteEndElement();

            writer.WriteEndElement(); // font
            writer.WriteEndDocument();
        }
    }
}
