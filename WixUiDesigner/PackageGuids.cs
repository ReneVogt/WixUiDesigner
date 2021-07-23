/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */
using System;
#nullable enable

namespace Com.Revo.WixUiDesigner
{
    sealed class PackageGuids
    {
        public const string PackageGuidString = "3EB42D7C-ADC9-40E7-A724-49101751F904";
        public const string WixUiEditorGuidString = "403D58CA-E91E-4ED6-9B6A-71BB29DB11C5";

        public static Guid PackageGuid { get; } = new Guid(PackageGuidString);
        public static Guid WixUiEditorGuid { get; } = new Guid(WixUiEditorGuidString);
    }
}
