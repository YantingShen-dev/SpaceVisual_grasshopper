namespace SpaceVisual.Core
{
    /// <summary>
    /// Plugin-wide constants. Three subcategories — the only intra-tab separator
    /// the user wants is the 2D ↔ 3D boundary.
    /// </summary>
    internal static class Constants
    {
        public const string Category = "Space Visual";

        // Number prefix forces GH to render subcategories in this order.
        public static class SubCategory
        {
            public const string Build      = "1 Build";
            public const string Analyze2D  = "2 Analyze 2D";
            public const string Analyze3D  = "3 Analyze 3D";
            public const string Visualize  = "4 Visualize";
        }
    }
}
