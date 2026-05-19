using System.Drawing;
using Rhino.Geometry;

namespace SpaceVisual.Core.Visual
{
    /// <summary>
    /// Bundle of colorization + legend layout parameters. SegHeight / SegWidth /
    /// TextHeight are scale multipliers relative to a base unit auto-computed from
    /// the analysis mesh's bounding box; defaults (1.0 / 0.3 / 1.0) produce a
    /// readable legend at any model scale.
    /// </summary>
    public sealed class SV_ColorParameter
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public Color[] Colors { get; set; } = DefaultColors;
        public bool Vertical { get; set; } = true;

        /// <summary>Multiplier on the auto base unit for each segment's length along the long axis.</summary>
        public double SegHeight { get; set; } = 1.0;
        /// <summary>Multiplier on the auto base unit for the legend strip thickness.</summary>
        public double SegWidth { get; set; } = 0.3;
        /// <summary>Multiplier on the auto base unit for label text height.</summary>
        public double TextHeight { get; set; } = 1.0;

        /// <summary>Lower-left anchor of the legend bar. Null = auto-place beside the analysis mesh.</summary>
        public Point3d? PlanePoint { get; set; }

        public static readonly Color[] DefaultColors =
        {
            Color.FromArgb(68, 1, 84),
            Color.FromArgb(59, 82, 139),
            Color.FromArgb(33, 145, 140),
            Color.FromArgb(94, 201, 98),
            Color.FromArgb(253, 231, 37),
        };

        public static SV_ColorParameter Default() => new();

        public HeatGradient ToGradient()
        {
            if (Colors == null || Colors.Length < 2) return HeatGradient.Viridis;
            int n = Colors.Length;
            var stops = new (double t, Color c)[n];
            for (int i = 0; i < n; i++)
                stops[i] = (n == 1 ? 0 : i / (double)(n - 1), Colors[i]);
            return new HeatGradient(stops);
        }
    }
}
