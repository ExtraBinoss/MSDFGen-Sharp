using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using Msdfgen;
using Msdfgen.Extensions;

namespace MsdfAtlasGen
{
    // BMFont XML classes (serialization)
    [XmlRoot("font")]
    public class BmFont
    {
        [XmlElement("info")] public required BmFontInfo Info { get; set; }
        [XmlElement("common")] public required BmFontCommon Common { get; set; }
        [XmlElement("pages")] public required BmFontPages Pages { get; set; }
        [XmlElement("distanceField")] public required BmFontDistanceField DistanceField { get; set; }
        [XmlElement("chars")] public required BmFontChars Chars { get; set; }
        [XmlElement("kernings")] public required BmFontKernings Kernings { get; set; }
    }

    public class BmFontInfo
    {
        [XmlAttribute("face")] public required string Face { get; set; }
        [XmlAttribute("size")] public int Size { get; set; }
        [XmlAttribute("bold")] public int Bold { get; set; } = 0;
        [XmlAttribute("italic")] public int Italic { get; set; } = 0;
        [XmlAttribute("charset")] public string Charset { get; set; } = "";
        [XmlAttribute("unicode")] public int Unicode { get; set; } = 0;
        [XmlAttribute("stretchH")] public int StretchH { get; set; } = 100;
        [XmlAttribute("smooth")] public int Smooth { get; set; } = 1;
        [XmlAttribute("aa")] public int Aa { get; set; } = 1;
        [XmlAttribute("padding")] public string Padding { get; set; } = "0,0,0,0";
        [XmlAttribute("spacing")] public string Spacing { get; set; } = "1,1";
        [XmlAttribute("outline")] public int Outline { get; set; } = 0;
    }

    public class BmFontCommon
    {
        [XmlAttribute("lineHeight")] public int LineHeight { get; set; }
        [XmlAttribute("base")] public int Base { get; set; }
        [XmlAttribute("scaleW")] public int ScaleW { get; set; }
        [XmlAttribute("scaleH")] public int ScaleH { get; set; }
        [XmlAttribute("pages")] public int Pages { get; set; } = 1;
        [XmlAttribute("packed")] public int Packed { get; set; } = 0;
        [XmlAttribute("alphaChnl")] public int AlphaChnl { get; set; } = 0;
        [XmlAttribute("redChnl")] public int RedChnl { get; set; } = 0;
        [XmlAttribute("greenChnl")] public int GreenChnl { get; set; } = 0;
        [XmlAttribute("blueChnl")] public int BlueChnl { get; set; } = 0;
    }

    public class BmFontPages
    {
        [XmlElement("page")] public List<BmFontPage> PageList { get; set; } = [];
    }

    public class BmFontPage
    {
        [XmlAttribute("id")] public int Id { get; set; }
        [XmlAttribute("file")] public required string File { get; set; }
    }

    public class BmFontDistanceField
    {
        [XmlAttribute("fieldType")] public required string FieldType { get; set; }
        [XmlAttribute("distanceRange")] public int DistanceRange { get; set; }
    }

    public class BmFontChars
    {
        [XmlAttribute("count")] public int Count { get; set; }
        [XmlElement("char")] public List<BmFontChar> CharList { get; set; } = [];
    }

    public class BmFontChar
    {
        [XmlAttribute("id")] public int Id { get; set; }
        [XmlAttribute("index")] public int Index { get; set; }
        [XmlAttribute("char")] public required string Char { get; set; }
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }
        [XmlAttribute("xoffset")] public int XOffset { get; set; }
        [XmlAttribute("yoffset")] public int YOffset { get; set; }
        [XmlAttribute("xadvance")] public int XAdvance { get; set; }
        [XmlAttribute("chnl")] public int Chnl { get; set; } = 15;
        [XmlAttribute("x")] public int X { get; set; }
        [XmlAttribute("y")] public int Y { get; set; }
        [XmlAttribute("page")] public int Page { get; set; } = 0;
    }

    public class BmFontKernings
    {
        [XmlAttribute("count")] public int Count { get; set; } = 0;
        [XmlElement("kerning")] public List<BmFontKerning> KerningList { get; set; } = [];
    }

    public class BmFontKerning
    {
        [XmlAttribute("first")] public int First { get; set; }
        [XmlAttribute("second")] public int Second { get; set; }
        [XmlAttribute("amount")] public int Amount { get; set; }
    }

    /// <summary>
    /// Exports font atlas data to BMFont XML format (.fnt)
    /// Uses FreeType-backed FontGeometry data for glyph metrics and positions
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
            FontMetrics metrics,
            YAxisOrientation yDirection = YAxisOrientation.Downward,
            Padding? outerPixelPadding = null,
            int spacing = 0)
        {
            if (fonts.Length == 0) return;

            var font = fonts[0]; // Use first font
            var fontName = font.GetName() ?? "Unknown";

            // Use actual outer pixel padding from packer config
            Padding actualPadding = outerPixelPadding ?? new Padding(0);
            int paddingLeft = (int)actualPadding.L;
            int paddingRight = (int)actualPadding.R;
            int paddingTop = (int)actualPadding.T;
            int paddingBottom = (int)actualPadding.B;

            var bmFont = new BmFont
            {
                Info = new BmFontInfo
                {
                    Face = fontName,
                    Size = (int)fontSize,
                    Unicode = 1,
                    Padding = $"{paddingTop},{paddingRight},{paddingBottom},{paddingLeft}",
                    Spacing = $"{spacing},{spacing}"
                },
                Common = new BmFontCommon
                {
                    LineHeight = (int)Math.Ceiling(metrics.LineHeight),
                    Base = (int)Math.Ceiling(metrics.AscenderY),
                    ScaleW = atlasWidth,
                    ScaleH = atlasHeight
                },
                Pages = new BmFontPages
                {
                    PageList = [new BmFontPage { Id = 0, File = Path.GetFileName(pngFilename) }]
                },
                DistanceField = new BmFontDistanceField
                {
                    FieldType = imageType switch
                    {
                        ImageType.Sdf => "sdf",
                        ImageType.Psdf => "psdf",
                        ImageType.Msdf => "msdf",
                        ImageType.Mtsdf => "mtsdf",
                        _ => "sdf"
                    },
                    DistanceRange = (int)distanceRange
                },
                Chars = new BmFontChars { Count = 0 },
                Kernings = new BmFontKernings()
            };

            // Export each glyph using stored geometry/metrics from FontGeometry
            foreach (var glyph in font.GetGlyphs().Glyphs)
            {
                glyph.GetBoxRect(out int x, out int y, out int w, out int h);

                int glyphIndex = glyph.GetIndex();
                float xadvance = (float)glyph.GetAdvanceUnscaled();
                
                // Use box bounds for positioning (includes padding, consistent with the image size)
                glyph.GetBoxPlaneBounds(out double pl, out double pb, out double pr, out double pt);
                
                // BMFont format (uses top-left origin):
                // - xoffset: horizontal offset from cursor position to left edge of glyph quad
                // - yoffset: vertical offset from baseline to TOP edge of glyph quad (negative = above baseline)
                // The plane bounds returned by GetBoxPlaneBounds are relative to the glyph origin (0,0)
                float xoffset = (float)pl;
                float yoffset = -(float)pt;

                int codepoint = (int)glyph.GetCodepoint();
                string charString = codepoint != 0 ? char.ConvertFromUtf32(codepoint) : "";

                bmFont.Chars.CharList.Add(new BmFontChar
                {
                    Id = codepoint,
                    Index = glyphIndex,
                    Char = charString,
                    Width = w,
                    Height = h,
                    XOffset = (int)Math.Round(xoffset),
                    YOffset = (int)Math.Round(yoffset),
                    XAdvance = (int)Math.Round(xadvance),
                    Chnl = 15,
                    X = x,
                    // Flip Y coordinate: BMFont uses Top-Left origin
                    Y = atlasHeight - y - h,
                    Page = 0
                });
            }

            bmFont.Chars.Count = bmFont.Chars.CharList.Count;

            // Serialize to XML
            var serializer = new XmlSerializer(typeof(BmFont));
            using var stream = new StreamWriter(outputFilename);
            serializer.Serialize(stream, bmFont);
        }
    }
}
