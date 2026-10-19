using System;
#if SPACEVISUAL_TEST
using System.Collections.Generic;
#endif

namespace SpaceVisual.Core
{
    /// <summary>
    /// Stable, separate identities for local test documents. Production GUIDs
    /// pass through unchanged. Never change existing test GUIDs after saving GH files.
    /// Add an entry here when introducing a new component or custom parameter.
    /// </summary>
    internal static class PluginIdentity
    {
#if SPACEVISUAL_TEST
        private static readonly Dictionary<Guid, Guid> TestIds = new Dictionary<Guid, Guid>
        {
            { new Guid("d7e3a9b4-5f12-4c80-a6e9-2b8d4f7c1903"), new Guid("5af2754c-e2f1-5f62-b09b-b29a82e7e4a6") },
            { new Guid("a5d2c9f3-7e84-4b10-8f6c-2a4e9b1d5072"), new Guid("2de01402-9ab5-58de-84f1-5240ba113c51") },
            { new Guid("e8b4a2c9-1f53-4d76-9a02-3c5e7b8f1d62"), new Guid("02999f25-066e-55b0-b16e-b0806e2068dc") },
            { new Guid("f4a7b8e2-6c91-4d05-bcad-3e8a2f7d9106"), new Guid("12fb028c-dce7-5312-bd0f-204719433c3e") },
            { new Guid("e7c2b491-3f6d-4a85-9b07-1d4e8c3f5a92"), new Guid("52f3cfac-d1b0-5439-aedd-fcb2a9433027") },
            { new Guid("f9a4b7e3-6d12-4c08-bf5a-2e8c3f1d7a04"), new Guid("2a07077f-9a69-5d4e-aaff-076b41190454") },
            { new Guid("f3a8e5c2-7b41-4d09-bc52-1d6e8a3f4c70"), new Guid("05f21b60-01fd-513f-8302-3a81e94dff6c") },
            { new Guid("b1a8c3d5-2e74-4a09-b5c1-8f3a6d1e2c47"), new Guid("268e770f-4f4c-5558-9238-67ef6435d1ee") },
            { new Guid("a1d4f3c5-9b62-4e07-bd58-3a8c2f1d6e74"), new Guid("39d9e064-d3b2-5af4-8122-b6481eef9d78") },
            { new Guid("c2b6e8a1-3f47-4a09-bd05-2e8c1d7f5b30"), new Guid("eb295c9f-66d8-5087-a99d-9fcfca85f8c8") },
            { new Guid("c8e2f5a1-9b34-4d07-bc6a-5d2e8f1a7036"), new Guid("fd65fe27-d93c-5c3e-9744-498b15828a4c") },
            { new Guid("d5e9b3a2-7f48-4c10-bd62-3a8e1f7c0269"), new Guid("ced07cdf-1fe1-5f67-b736-e208d30b7fe4") },
            { new Guid("c2e6d7b1-9a4f-4e1b-bd35-7e0f9c2a8d51"), new Guid("ae1c52ae-de07-53ee-b6b3-1ad8e532ae2d") },
            { new Guid("a3f5c2d1-7b48-4e91-9c3a-5d2e8f1a6b7c"), new Guid("b5c4b3b4-e416-5e3e-88ef-fef1b114cd63") },
        };
#endif

        internal static Guid For(Guid productionId)
        {
#if SPACEVISUAL_TEST
            return TestIds[productionId];
#else
            return productionId;
#endif
        }
    }
}
