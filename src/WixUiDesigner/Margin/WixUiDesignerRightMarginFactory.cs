/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.ComponentModel.Composition;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

#nullable enable

namespace WixUiDesigner.Margin
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(nameof(WixUiDesignerRightMarginFactory))]
    [Order(After = PredefinedMarginNames.VerticalScrollBar)] 
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType("xml")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class WixUiDesignerRightMarginFactory : WixUiDesignerBaseMarginFactory, IWpfTextViewMarginProvider
    {
#pragma warning disable VSTHRD010 // thread is synchronized in base method
        public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
            CreateMargin(wpfTextViewHost, Dock.Right);
    }
}
