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
    [Name(nameof(WixUiDesignerTopMarginFactory))]
    [Order(After = PredefinedMarginNames.Top)] 
    [MarginContainer(PredefinedMarginNames.Top)]
    [ContentType("xml")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class WixUiDesignerTopMarginFactory : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (WixUiDesignerPackage.Options?.DesignerPosition != Dock.Top) return null;

            Logger.Log(DebugContext.Margin, "Creating top margin.");
            return new WixUiDesignerMargin(wpfTextViewHost.TextView);
        }

    }
}
