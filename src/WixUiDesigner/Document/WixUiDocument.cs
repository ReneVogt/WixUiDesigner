/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Document
{
    sealed class WixUiDocument
    {
        public string FileName { get; }
        public IWpfTextView WpfTextView { get; }
        

        WixUiDocument(string fileName, IWpfTextView wpfTextView)
        {
            FileName = fileName;
            WpfTextView = wpfTextView;
        }

        public static WixUiDocument? Get(IWpfTextView wpfTextView)
        {
            if (!wpfTextView.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var document))
            {
                Logger.Log(DebugContext.Document, "Could not determine document from WpfTextView!");
                return null;
            }

            Logger.Log(DebugContext.Document, $"Creating document entry for {document.FilePath}.");
            return new (document.FilePath, wpfTextView);
        }
    }
}
