/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using WixUiSimulator.Logging;

#nullable enable

namespace WixUiSimulator.Document
{
    sealed class WixUiDocument : IDisposable
    {
        public event EventHandler<UpdateRequiredEventArgs>? UpdateRequired;
        public event EventHandler? Closed;

        bool disposed;
        XDocument xml;
        bool xmlChanged;
        
        public string FileName { get; }
        public IWpfTextView WpfTextView { get; }
        public WixProject WixProject { get; }
        public XDocument Xml
        {
            get
            {
                if (!xmlChanged) return xml;
                xmlChanged = false;
                try
                {
                    xml = WixParser.Load(WpfTextView.TextBuffer.CurrentSnapshot.GetText());
                }
                catch (Exception exception)
                {
                    Logger.Log(DebugContext.Document | DebugContext.WiX | DebugContext.Exceptions, $"Failed to parse {FileName}: {exception}");
                }

                return xml;
            }
        }

        WixUiDocument(string fileName, IWpfTextView wpfTextView, XDocument xml, WixProject wixProject)
        {
            FileName = fileName;
            WpfTextView = wpfTextView;
            this.xml = xml;
            WixProject = wixProject;

            WpfTextView.TextBuffer.Changed += OnTextChanged;
            WpfTextView.Caret.PositionChanged += OnCaretPositionChanged;
            WpfTextView.Closed += OnClosed;
        }
        public void Dispose()
        {
            if (disposed) return;
            Logger.Log(DebugContext.Document, $"Closing document {FileName}.");
            disposed = true;
            Closed?.Invoke(this, EventArgs.Empty);
            WpfTextView.TextBuffer.Changed -= OnTextChanged;
            WpfTextView.Closed -= OnClosed;
            WpfTextView.Properties.RemoveProperty(typeof(WixUiDocument));
        }

        void OnTextChanged(object sender, EventArgs e)
        {
            xmlChanged = true;
            UpdateRequired?.Invoke(this, UpdateRequiredEventArgs.DocumentChangedArgs(e));
        }
        void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e) => UpdateRequired?.Invoke(this, UpdateRequiredEventArgs.SelectionChangedArgs(e));
        void OnClosed(object sender, EventArgs e) => Dispose();

        public static WixUiDocument? Get(IWpfTextView wpfTextView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!wpfTextView.TextBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var document))
            {
                Logger.Log(DebugContext.Document, "Could not determine document from WpfTextView!");
                return null;
            }

            try
            {
                var xml = WixParser.Load(wpfTextView.TextBuffer.CurrentSnapshot.GetText());
                if (!xml.IsWixUiDocument())
                {
                    Logger.Log(DebugContext.Document | DebugContext.WiX, $"{document.FilePath} is not a valid WiX UI document.");
                    return null;
                }

                var projectItem = SolutionParser.GetProjectItem(document.FilePath);
                if (projectItem is null)
                {
                    Logger.Log(DebugContext.Document | DebugContext.WiX, $"{document.FilePath} is not a project item.");
                    return null;
                }

                var wixProject = WixProject.Get(projectItem);
                if (wixProject is null)
                {
                    Logger.Log(DebugContext.Document | DebugContext.WiX, $"{document.FilePath} is not part of a WiX project.");
                    return null;
                }

                Logger.Log(DebugContext.Document, $"Creating document entry for {document.FilePath}, part of project {wixProject.Project.Name}.");
                return wpfTextView.Properties.GetOrCreateSingletonProperty(() => new WixUiDocument(document.FilePath, wpfTextView, xml, wixProject));
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.Document | DebugContext.WiX | DebugContext.Exceptions, $"Failed to parse document {document.FilePath}: {exception}");
                return null;
            }
        }
    }
}
