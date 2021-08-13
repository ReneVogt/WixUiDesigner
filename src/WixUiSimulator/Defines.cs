/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;

#nullable enable

namespace WixUiSimulator
{
    static class Defines
    {
        internal const string PackageGuidString = "13c4f662-6ebc-4dbd-9e57-165c8f7dbcbf";
        internal static Guid PackageGuid { get; } = new Guid(PackageGuidString);

        internal const string EditorGuidString = "6BF6EFCD-CD85-4670-A07F-B91E1345B9E0";
        internal static Guid EditorGuid { get; } = new Guid(EditorGuidString);

        internal const string ProductName = "WiX UI Simulator";
        internal const string PackageName = "WixUiSimulator";

        internal const string WixProjectKindGuidString = "930c7802-8a8c-48f9-8165-68863bccd9dd";
        internal static Guid WixProjectKindGuid { get; } = new Guid(WixProjectKindGuidString);
    }
}
