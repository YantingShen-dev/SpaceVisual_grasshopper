using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SpaceVisual.Core.Base
{
    /// <summary>
    /// Common base class for all Space Visual components.
    /// Centralises Category, exposure, and error-message helpers so
    /// individual components only declare their SubCategory + IO.
    /// </summary>
    public abstract class SVComponent : GH_Component
    {
        protected SVComponent(string name, string nickname, string description, string subCategory)
            : base(name, nickname, description, Constants.Category, subCategory)
        {
        }

        /// <summary>Override to attach a custom icon. Default = null (GH draws letters).</summary>
        protected override Bitmap? Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>Convenience: stop solving with an error message. Returns false so callers can `if (Fail(...)) return;`.</summary>
        protected bool Fail(string message)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
            return true;
        }

        /// <summary>Convenience: emit a warning without stopping.</summary>
        protected void Warn(string message)
            => AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

        /// <summary>Convenience: emit a remark/info message.</summary>
        protected void Remark(string message)
            => AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, message);
    }
}
