/*
 * (C) René Vogt
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

        internal const string EditorGuidString = "BCA9A5E7-A7EE-4EE2-8357-FF6F1BE0AA2B";

        internal const string LanguageServiceGuidString = "6FB0C119-289A-431C-8318-A6C451DFA056";
        internal static Guid LanguageServiceGuid { get; } = new Guid(LanguageServiceGuidString);

        internal const string ProductName = "WiX UI Designer";
        internal const string PackageName = "WixUiDesigner";
    }
}
