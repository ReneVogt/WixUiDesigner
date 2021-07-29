/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Document
{
    sealed class WixUiDocument
    {
        public static XmlNamespaceManager WixNamespaceManager { get; }

        public string FileName { get; }
        public IWpfTextView WpfTextView { get; }

        static WixUiDocument()
        {
            WixNamespaceManager = new (new NameTable());
            WixNamespaceManager.AddNamespace("wix", "http://schemas.microsoft.com/wix/2006/wi");
        }

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

            try
            {
                var text = wpfTextView.TextBuffer.CurrentSnapshot.GetText();
                var xml = XDocument.Parse(text);
                var numberOfDialogs = xml.XPathSelectElements("/wix:Wix/wix:Fragment/wix:UI/wix:Dialog", WixNamespaceManager).Count();
                if (numberOfDialogs != 1)
                {
                    Logger.Log(DebugContext.Document, $"Invalid number of dialogs in {document.FilePath}: {numberOfDialogs}.");
                    return null;
                }

                Logger.Log(DebugContext.Document, $"Creating document entry for {document.FilePath}.");
                return wpfTextView.Properties.GetOrCreateSingletonProperty(() => new WixUiDocument(document.FilePath, wpfTextView));
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.Document, $"Failed to parse document {document.FilePath}: {exception}");
                return null;
            }
        }
    }
}
