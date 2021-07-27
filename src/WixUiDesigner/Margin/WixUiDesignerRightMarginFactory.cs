/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.ComponentModel.Composition;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Margin
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(nameof(WixUiDesignerRightMarginFactory))]
    [Order(After = PredefinedMarginNames.VerticalScrollBar)] 
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType("xml")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class WixUiDesignerRightMarginFactory : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (WixUiDesignerPackage.Options?.DesignerPosition != Dock.Right) return null;

            Logger.Log(DebugContext.Margin, "Creating right margin.");
            return new WixUiDesignerMargin(wpfTextViewHost.TextView, false);
        }

    }
}
