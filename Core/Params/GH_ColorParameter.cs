using Grasshopper.Kernel.Types;
using SpaceVisual.Core.Visual;

namespace SpaceVisual.Core.Params
{
    public sealed class GH_ColorParameter : GH_Goo<SV_ColorParameter>
    {
        public GH_ColorParameter() { }
        public GH_ColorParameter(SV_ColorParameter value) : base(value) { }
        public GH_ColorParameter(GH_ColorParameter other) : base(other?.Value!) { }

        public override bool IsValid => Value != null;
        public override string TypeName => "Color Parameter";
        public override string TypeDescription =>
            "Bundle of colorization + legend layout parameters for SV_Colorize.";
        public override IGH_Goo Duplicate() => new GH_ColorParameter(this);

        public override string ToString()
        {
            if (Value == null) return "Null Parameter";
            string range = (Value.Min.HasValue || Value.Max.HasValue)
                ? $", domain=[{(Value.Min?.ToString("F2") ?? "auto")}, {(Value.Max?.ToString("F2") ?? "auto")}]"
                : "";
            string orient = Value.Vertical ? "V" : "H";
            return $"Color Parameter ({Value.Colors.Length} stops, {orient}{range})";
        }
    }
}
