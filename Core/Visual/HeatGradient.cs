using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SpaceVisual.Core.Visual
{
    /// <summary>
    /// Piecewise-linear gradient over color stops, plus utilities for
    /// min-max normalisation and dual-metric blending. Powers SV_Colorize.
    /// </summary>
    public sealed class HeatGradient
    {
        private readonly (double T, Color C)[] _stops;

        public HeatGradient(IEnumerable<(double t, Color c)> stops)
        {
            if (stops == null) throw new ArgumentNullException(nameof(stops));
            var arr = stops.OrderBy(s => s.t).Select(s => (T: s.t, C: s.c)).ToArray();
            if (arr.Length < 2) throw new ArgumentException("Gradient requires at least 2 stops.");
            _stops = arr;
        }

        /// <summary>Sample the gradient at t ∈ [0,1]. NaN inputs map to a neutral gray.</summary>
        public Color Sample(double t)
        {
            if (double.IsNaN(t)) return Color.FromArgb(255, 128, 128, 128);
            int last = _stops.Length - 1;
            if (t <= _stops[0].T) return _stops[0].C;
            if (t >= _stops[last].T) return _stops[last].C;
            for (int i = 1; i < _stops.Length; i++)
            {
                if (t <= _stops[i].T)
                {
                    var (t0, c0) = _stops[i - 1];
                    var (t1, c1) = _stops[i];
                    double k = (t - t0) / (t1 - t0);
                    return Lerp(c0, c1, k);
                }
            }
            return _stops[last].C;
        }

        /// <summary>
        /// Map a list of values to colors via min-max normalisation.
        /// Pass <paramref name="minOverride"/> / <paramref name="maxOverride"/>
        /// to lock the domain (e.g., for comparable cross-frame legends).
        /// </summary>
        public Color[] MapValues(
            IReadOnlyList<double> values,
            double? minOverride = null,
            double? maxOverride = null)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            int n = values.Count;
            var result = new Color[n];
            if (n == 0) return result;

            double min = minOverride ?? double.PositiveInfinity;
            double max = maxOverride ?? double.NegativeInfinity;
            if (minOverride == null || maxOverride == null)
            {
                for (int i = 0; i < n; i++)
                {
                    double v = values[i];
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                    if (minOverride == null && v < min) min = v;
                    if (maxOverride == null && v > max) max = v;
                }
            }

            double range = max - min;
            bool degenerate = !(range > 0);

            for (int i = 0; i < n; i++)
            {
                double v = values[i];
                if (double.IsNaN(v) || double.IsInfinity(v))
                {
                    result[i] = Color.FromArgb(255, 128, 128, 128);
                    continue;
                }
                double t = degenerate ? 0.5 : (v - min) / range;
                result[i] = Sample(t);
            }
            return result;
        }

        /// <summary>
        /// Blend two gradients applied to two value series of equal length.
        /// Each value series is independently normalised, then per-index colors
        /// are linearly mixed with <paramref name="weightA"/> (0..1, weight of A).
        /// </summary>
        public static Color[] MapDual(
            IReadOnlyList<double> valuesA, HeatGradient gradA,
            IReadOnlyList<double> valuesB, HeatGradient gradB,
            double weightA = 0.5)
        {
            if (valuesA == null) throw new ArgumentNullException(nameof(valuesA));
            if (valuesB == null) throw new ArgumentNullException(nameof(valuesB));
            if (valuesA.Count != valuesB.Count)
                throw new ArgumentException("valuesA and valuesB must have the same length.");

            if (weightA < 0) weightA = 0; else if (weightA > 1) weightA = 1;
            double weightB = 1.0 - weightA;

            var ca = gradA.MapValues(valuesA);
            var cb = gradB.MapValues(valuesB);
            var result = new Color[valuesA.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = Lerp(ca[i], cb[i], weightB);
            return result;
        }

        private static Color Lerp(Color a, Color b, double k)
        {
            if (k < 0) k = 0; else if (k > 1) k = 1;
            int r = (int)Math.Round(a.R + (b.R - a.R) * k);
            int g = (int)Math.Round(a.G + (b.G - a.G) * k);
            int bl = (int)Math.Round(a.B + (b.B - a.B) * k);
            int al = (int)Math.Round(a.A + (b.A - a.A) * k);
            return Color.FromArgb(al, r, g, bl);
        }

        // ------------------------------------------------------------ presets

        /// <summary>Perceptually-uniform purple→teal→yellow. Recommended default for analysis maps.</summary>
        public static HeatGradient Viridis { get; } = new(new (double, Color)[]
        {
            (0.00, Color.FromArgb(68, 1, 84)),
            (0.25, Color.FromArgb(59, 82, 139)),
            (0.50, Color.FromArgb(33, 145, 140)),
            (0.75, Color.FromArgb(94, 201, 98)),
            (1.00, Color.FromArgb(253, 231, 37)),
        });

        /// <summary>Black→red→yellow→white classic heat ramp.</summary>
        public static HeatGradient Heat { get; } = new(new (double, Color)[]
        {
            (0.00, Color.FromArgb(0, 0, 0)),
            (0.33, Color.FromArgb(178, 34, 34)),
            (0.66, Color.FromArgb(255, 215, 0)),
            (1.00, Color.FromArgb(255, 255, 255)),
        });

        /// <summary>Blue→white→red diverging ramp. Good for signed metrics.</summary>
        public static HeatGradient CoolWarm { get; } = new(new (double, Color)[]
        {
            (0.00, Color.FromArgb(59, 76, 192)),
            (0.50, Color.FromArgb(221, 221, 221)),
            (1.00, Color.FromArgb(180, 4, 38)),
        });

        /// <summary>Full spectrum blue→cyan→green→yellow→red.</summary>
        public static HeatGradient Spectrum { get; } = new(new (double, Color)[]
        {
            (0.00, Color.FromArgb(0, 0, 255)),
            (0.25, Color.FromArgb(0, 255, 255)),
            (0.50, Color.FromArgb(0, 255, 0)),
            (0.75, Color.FromArgb(255, 255, 0)),
            (1.00, Color.FromArgb(255, 0, 0)),
        });
    }
}
