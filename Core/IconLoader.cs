using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SpaceVisual.Core
{
    /// <summary>
    /// Loads PNG icons embedded via the csproj &lt;EmbeddedResource&gt; glob.
    /// Each resource is keyed by its base filename ("Build Graph 2D", "Head icon", …).
    /// Bitmaps are resized to <see cref="DefaultSize"/> (24×24, GH's expected size)
    /// with high-quality bicubic interpolation, and cached so repeated lookups are free.
    /// </summary>
    internal static class IconLoader
    {
        public const int DefaultSize = 24;

        private static readonly Dictionary<(string name, int size), Bitmap?> _cache = new();
        private static readonly object _lock = new();

        public static Bitmap? Load(string filenameWithoutExtension, int size = DefaultSize)
        {
            var key = (filenameWithoutExtension, size);
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var cached)) return cached;

                Bitmap? bmp = null;
                try
                {
                    var asm = typeof(IconLoader).Assembly;
                    var names = asm.GetManifestResourceNames();
                    string target = $"{filenameWithoutExtension}.png";

                    string? match = null;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (names[i].EndsWith(target, StringComparison.OrdinalIgnoreCase))
                        {
                            match = names[i];
                            break;
                        }
                    }

                    if (match != null)
                    {
                        using var stream = asm.GetManifestResourceStream(match);
                        if (stream != null)
                        {
                            using var raw = new Bitmap(stream);
                            bmp = (raw.Width == size && raw.Height == size)
                                ? new Bitmap(raw)        // copy at native size
                                : Resize(raw, size);     // downscale (typical)
                        }
                    }
                }
                catch
                {
                    bmp = null;
                }

                _cache[key] = bmp;
                return bmp;
            }
        }

        private static Bitmap Resize(Bitmap source, int size)
        {
            var dest = new Bitmap(size, size, source.PixelFormat == System.Drawing.Imaging.PixelFormat.Indexed
                ? System.Drawing.Imaging.PixelFormat.Format32bppArgb
                : source.PixelFormat);

            using var g = Graphics.FromImage(dest);
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode      = SmoothingMode.HighQuality;
            g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.CompositingMode    = CompositingMode.SourceOver;
            g.Clear(Color.Transparent);
            g.DrawImage(source, new Rectangle(0, 0, size, size));
            return dest;
        }
    }
}
