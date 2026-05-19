using Grasshopper.Kernel;
using SpaceVisual.Core;

namespace SpaceVisual
{
    /// <summary>
    /// Runs at Grasshopper load time. Registers the tab-strip icon, short name,
    /// and symbol character for the "Space Visual" category. GH_AssemblyInfo.Icon
    /// alone does NOT control the tab logo — these calls on ComponentServer do.
    /// </summary>
    public class SpaceVisualPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            var icon = IconLoader.Load("Head icon");
            if (icon != null)
                Grasshopper.Instances.ComponentServer.AddCategoryIcon(Constants.Category, icon);

            Grasshopper.Instances.ComponentServer.AddCategorySymbolName(Constants.Category, 'S');
            Grasshopper.Instances.ComponentServer.AddCategoryShortName(Constants.Category, "SV");

            return GH_LoadingInstruction.Proceed;
        }
    }
}
