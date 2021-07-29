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
        public event EventHandler? Closed;

        bool disposed;
        
        public string FileName { get; }
        public IWpfTextView WpfTextView { get; }
        public XDocument Xml { get; private set; }

        WixUiDocument(string fileName, IWpfTextView wpfTextView, XDocument xml)
        {
            FileName = fileName;
            WpfTextView = wpfTextView;
            Xml = xml;

            WpfTextView.TextBuffer.Changed += OnTextChanged;
            WpfTextView.Caret.PositionChanged += OnCaretPositionChanged;
            WpfTextView.Closed += OnClosed;
        }
        public void Dispose()
        {
            if (disposed) return;
            Logger.Log(DebugContext.WiX, $"Closing document {FileName}.");
            disposed = true;
            Closed?.Invoke(this, EventArgs.Empty);
            WpfTextView.TextBuffer.Changed -= OnTextChanged;
            WpfTextView.Closed -= OnClosed;
            WpfTextView.Properties.RemoveProperty(typeof(WixUiDocument));
        }

        void OnTextChanged(object sender, EventArgs e)
        {
            Xml = XDocument.Parse(WpfTextView.TextBuffer.CurrentSnapshot.GetText());
            UpdateRequired?.Invoke(this, e);
        }
        void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e) => UpdateRequired?.Invoke(this, e);
        void OnClosed(object sender, EventArgs e) => Dispose();



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
