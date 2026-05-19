using System;
using System.Drawing;
using Grasshopper.Kernel;
using SpaceVisual.Core;

namespace SpaceVisual
{
    public class SpaceVisualInfo : GH_AssemblyInfo
    {
        public override string Name => "Space Visual";

        public override Bitmap? Icon => IconLoader.Load("Head icon");

        public override string Description =>
            "Visibility and sightline analysis for Grasshopper. " +
            "2D/3D Isovist with geometric metrics, VGA space-syntax metrics, " +
            "visibility paths, and inverse surface visibility.";

        public override Guid Id => new Guid("a3f5c2d1-7b48-4e91-9c3a-5d2e8f1a6b7c");

        public override string AuthorName => "POLY LAB";

        public override string AuthorContact => "";

        public override string Version => "0.1.0";

        public override string AssemblyVersion => "0.1.0.0";
    }
}
