using System;
using System.Collections.Generic;
using System.IO;
using Msdfgen;
using Newtonsoft.Json;

namespace MsdfAtlasGen
{
    public struct JsonAtlasMetrics
    {
        public struct GridMetrics
        {
            public int CellWidth;
            public int CellHeight;
            public int Columns;
            public int Rows;
            public double? OriginX;
            public double? OriginY;
            public int Spacing;
        }

        public Msdfgen.Range DistanceRange;
        public double Size;
        public int Width;
        public int Height;
        public YAxisOrientation YDirection;
        public GridMetrics? Grid;
    }

    public static class JsonExporter
    {
        /// <summary>
        /// Converts the image type enum to its string representation used in the JSON.
        /// </summary>
        private static string ImageTypeString(ImageType type)
        {
            switch (type)
            {
                case ImageType.HardMask: return "hardmask";
                case ImageType.SoftMask: return "softmask";
                case ImageType.Sdf: return "sdf";
                case ImageType.Psdf: return "psdf";
                case ImageType.Msdf: return "msdf";
                case ImageType.Mtsdf: return "mtsdf";
                default: return "unknown";
            }
        }

        /// <summary>
        /// Exports the font atlas metadata and glyph metrics to a JSON file.
        /// </summary>
        public static void Export(FontGeometry[] fonts, ImageType imageType, JsonAtlasMetrics metrics, string filename, bool kerning)
        {
            var root = new Dictionary<string, object>();

            var atlas = new Dictionary<string, object>
            {
                ["type"] = ImageTypeString(imageType)
            };
            if (imageType == MsdfAtlasGen.ImageType.Sdf || imageType == MsdfAtlasGen.ImageType.Psdf || imageType == MsdfAtlasGen.ImageType.Msdf || imageType == MsdfAtlasGen.ImageType.Mtsdf)
            {
                atlas["distanceRange"] = metrics.DistanceRange.Upper - metrics.DistanceRange.Lower;
                atlas["distanceRangeMiddle"] = 0.5 * (metrics.DistanceRange.Lower + metrics.DistanceRange.Upper);
            }
            atlas["size"] = metrics.Size;
            atlas["width"] = metrics.Width;
            atlas["height"] = metrics.Height;
            atlas["yOrigin"] = metrics.YDirection == YAxisOrientation.Downward ? "top" : "bottom";
            
            if (metrics.Grid.HasValue)
            {
                var grid = new Dictionary<string, object>
                {
                    ["cellWidth"] = metrics.Grid.Value.CellWidth,
                    ["cellHeight"] = metrics.Grid.Value.CellHeight,
                    ["columns"] = metrics.Grid.Value.Columns,
                    ["rows"] = metrics.Grid.Value.Rows
                };
                if (metrics.Grid.Value.OriginX.HasValue)
                    grid["originX"] = metrics.Grid.Value.OriginX.Value;
                if (metrics.Grid.Value.OriginY.HasValue)
                {
                    if (metrics.YDirection == YAxisOrientation.Downward)
                         grid["originY"] = (metrics.Grid.Value.CellHeight - metrics.Grid.Value.Spacing - 1) / metrics.Size - metrics.Grid.Value.OriginY.Value;
                    else
                         grid["originY"] = metrics.Grid.Value.OriginY.Value;
                }
                atlas["grid"] = grid;
            }
            root["atlas"] = atlas;

            if (fonts.Length > 1)
                root["variants"] = new List<object>();

            // If fonts.Length == 1, we merge properties into root or handle differently?
            // C++: if (fontCount > 1) fputs("\"variants\":[", f);
            // It seems it creates an array of font objects.
            // But if fontCount == 1, it dumps fields directly?
            // C++: for loop. if fontCount > 1, emit json object separator.
            // If fontCount == 1, it just dumps fields. Wait.
            // "atlas": { ... }, properties for font 0.
            // If fontCount > 1: "atlas": { ... }, "variants": [ { font 0 }, { font 1 } ]
            
            // Replicating C++ structure exactly:
            if (fonts.Length > 1)
            {
                 var variants = new List<object>();
                 foreach(var font in fonts)
                 {
                     variants.Add(CreateFontObject(font, metrics, kerning));
                 }
                 root["variants"] = variants;
            }
            else if (fonts.Length == 1)
            {
                // Merge font object keys into root
                var fontObj = CreateFontObject(fonts[0], metrics, kerning);
                foreach(var kvp in fontObj)
                    root[kvp.Key] = kvp.Value;
            }
            
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
            string json = JsonConvert.SerializeObject(root, settings);
            File.WriteAllText(filename, json);
        }

        /// <summary>
        /// Internal helper to create a JSON-compatible object representing a single font.
        /// </summary>
        private static Dictionary<string, object> CreateFontObject(FontGeometry font, JsonAtlasMetrics metrics, bool kerning)
        {
            var obj = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(font.GetName()))
                obj["name"] = font.GetName();

            double yFactor = metrics.YDirection == YAxisOrientation.Downward ? -1 : 1;
            var fontMetrics = font.GetMetrics();
            obj["metrics"] = new
            {
                emSize = fontMetrics.EmSize,
                lineHeight = fontMetrics.LineHeight,
                ascender = yFactor * fontMetrics.AscenderY,
                descender = yFactor * fontMetrics.DescenderY,
                underlineY = yFactor * fontMetrics.UnderlineY,
                underlineThickness = fontMetrics.UnderlineThickness
            };

            var glyphsList = new List<object>();
            foreach (var glyph in font.GetGlyphs().Glyphs)
            {
                var gObj = new Dictionary<string, object>();
                if (font.GetPreferredIdentifierType() == GlyphIdentifierType.GlyphIndex)
                    gObj["index"] = glyph.GetIndex();
                else
                    gObj["unicode"] = glyph.GetCodepoint();
                
                gObj["advance"] = glyph.GetAdvance();
                
                glyph.GetQuadPlaneBounds(out double l, out double b, out double r, out double t);
                if (l != 0 || b != 0 || r != 0 || t != 0)
                {
                    if (metrics.YDirection == YAxisOrientation.Downward)
                        gObj["planeBounds"] = new { left = l, top = -t, right = r, bottom = -b };
                    else
                         gObj["planeBounds"] = new { left = l, bottom = b, right = r, top = t };
                }

                glyph.GetQuadAtlasBounds(out double al, out double ab, out double ar, out double at);
                if (al != 0 || ab != 0 || ar != 0 || at != 0)
                {
                     if (metrics.YDirection == YAxisOrientation.Downward)
                        gObj["atlasBounds"] = new { left = al, top = metrics.Height - at, right = ar, bottom = metrics.Height - ab };
                    else
                         gObj["atlasBounds"] = new { left = al, bottom = ab, right = ar, top = at };
                }
                glyphsList.Add(gObj);
            }
            obj["glyphs"] = glyphsList;

            if (kerning)
            {
                // Kerning not implemented yet in FontGeometry, so empty list/skip
                // C++: if (kerning) ...
                // If I implement GetKerning later, populate this.
                // Assuming empty for now.
            }

            return obj;
        }
    }
}
