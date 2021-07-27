/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;

#nullable enable

namespace WixUiDesigner.Logging
{
    [Flags]
    public enum DebugContext
    {
        None = 0,

        Package = 1 << 0,
        Margin = 1 << 1,
        WiX = 1 << 2,

        All = -1
    }
}
