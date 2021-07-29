/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Document
{
    sealed class WixUiDocument : IDisposable
    {
        public event EventHandler? UpdateRequired;

        public string FileName { get; }
        public IWpfTextView WpfTextView { get; }
        public XDocument Xml { get; }

        WixUiDocument(string fileName, IWpfTextView wpfTextView, XDocument xml)
        {
            FileName = fileName;
            WpfTextView = wpfTextView;
            Xml = xml;
        }
        public void Dispose()
        {
            WpfTextView.Properties.RemoveProperty(typeof(WixUiDocument));
        }

        public static WixUiDocument? Get(IWpfTextView wpfTextView)
        {
            if (!wpfTextView.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var document))
            {
                Logger.Log(DebugContext.Document, "Could not determine document from WpfTextView!");
                return null;
            }

            try
            {
                var text = wpfTextView.TextBuffer.CurrentSnapshot.GetText();
                var xml = XDocument.Parse(text);
                if (!xml.IsWixUiDocument())
                {
                    Logger.Log(DebugContext.Document, $"{document.FilePath} is not a valid WiX UI document.");
                    return null;
                }

                Logger.Log(DebugContext.Document, $"Creating document entry for {document.FilePath}.");
                return wpfTextView.Properties.GetOrCreateSingletonProperty(() => new WixUiDocument(document.FilePath, wpfTextView, xml));
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.Document, $"Failed to parse document {document.FilePath}: {exception}");
                return null;
            }
        }
    }
}
