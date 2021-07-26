/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Windows.Forms;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Editor
{
    public sealed class EditorControl : RichTextBox
    {
        readonly Logger logger;

        internal EditorControl(Logger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
