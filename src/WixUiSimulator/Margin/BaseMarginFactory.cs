/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using WixUiSimulator.Document;
using WixUiSimulator.Logging;

#nullable enable

namespace WixUiSimulator.Margin
{
    internal class BaseMarginFactory
    {
        protected IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, Dock position)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (WixUiSimulatorPackage.Options?.SimulatorPosition != position) return null;
            var document = WixUiDocument.Get(wpfTextViewHost.TextView);
            if (document is null) return null;
            Logger.Log(DebugContext.Margin, $"Creating {position} margin for {document.FileName}.");
            return new SimulatorMargin(document);
        }
    }
}
