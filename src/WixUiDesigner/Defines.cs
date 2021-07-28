/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;

#nullable enable

namespace WixUiDesigner
{
    static class Defines
    {
        internal const string PackageGuidString = "13c4f662-6ebc-4dbd-9e57-165c8f7dbcbf";
        internal static Guid PackageGuid { get; } = new Guid(PackageGuidString);

        internal const string EditorGuidString = "6BF6EFCD-CD85-4670-A07F-B91E1345B9E0";
        internal static Guid EditorGuid { get; } = new Guid(EditorGuidString);

        internal const string ProductName = "WiX UI Designer";
        internal const string PackageName = "WixUiDesigner";
    }
}
