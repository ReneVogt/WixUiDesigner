/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using WixUiDesigner.Document;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Margin
{
    internal class WixUiDesignerBaseMarginFactory
    {
        protected IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, Dock position)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (WixUiDesignerPackage.Options?.DesignerPosition != position) return null;
            var document = WixUiDocument.Get(wpfTextViewHost.TextView);
            if (document is null) return null;
            Logger.Log(DebugContext.Margin, $"Creating {position} margin for {document.FileName}.");
            return new WixUiDesignerMargin(document);
        }
    }
}
