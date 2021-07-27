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
    [Name(nameof(WixUiDesignerBottomMarginFactory))]
    [Order(After = PredefinedMarginNames.HorizontalScrollBar)] 
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [ContentType("xml")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class WixUiDesignerBottomMarginFactory : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (WixUiDesignerPackage.Options?.DesignerPosition != Dock.Bottom) return null;

            Logger.Log(DebugContext.Margin, "Creating bottom margin.");
            return new WixUiDesignerMargin(wpfTextViewHost.TextView, true);
        }

    }
}
