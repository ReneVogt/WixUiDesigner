/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

#nullable enable

namespace WixUiDesigner.Margin
{
    /// <summary>
    /// Margin's canvas and visual definition including both size and content
    /// </summary>
    internal class WixUiDesignerMargin : Canvas, IWpfTextViewMargin
    {
        readonly bool horizontal;

        bool isDisposed;

        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }
        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return ActualHeight;
            }
        }
        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public WixUiDesignerMargin(IWpfTextView textView, bool horizontal)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.horizontal = horizontal;
            if (this.horizontal)
                Height = WixUiDesignerPackage.Options?.DesignerSize ?? Options.DefaultDesignerSize;
            else
                Width = WixUiDesignerPackage.Options?.DesignerSize ?? Options.DefaultDesignerSize;

            ClipToBounds = true;
            Background = new SolidColorBrush(Colors.LightGreen);

            var label = new Label
            {
                Background = new SolidColorBrush(Colors.LightGreen),
                Content = "Hello WixUiDesignerMargin",
            };

            Children.Add(label);
        }

        public ITextViewMargin? GetTextViewMargin(string marginName) => marginName switch
        {
            nameof(WixUiDesignerLeftMarginFactory) or nameof(WixUiDesignerRightMarginFactory) or nameof(WixUiDesignerTopMarginFactory) or nameof(WixUiDesignerBottomMarginFactory) => this,
            _ => null,
        };

        public void Dispose()
        {
            if (isDisposed) return;
            GC.SuppressFinalize(this);
            isDisposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(WixUiDesignerMargin));
        }
    }
}
