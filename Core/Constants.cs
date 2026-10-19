namespace SpaceVisual.Core
{
    /// <summary>
    /// Plugin-wide category and subcategory names.
    /// </summary>
    internal static class Constants
    {
#if SPACEVISUAL_TEST
        public const string Category = "Space Visual test";
        public const string ShortName = "SV test";
#else
        public const string Category = "Space Visual";
        public const string ShortName = "SV";
#endif

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
