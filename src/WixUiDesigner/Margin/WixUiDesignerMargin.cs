/*
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
using WixUiDesigner.Document;
using WixUiDesigner.Exceptions;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Margin
{
    internal class WixUiDesignerMargin : DockPanel, IWpfTextViewMargin
    {
        readonly Dock position;
        readonly ScrollViewer displayContainer;
        readonly WixUiDocument document;

        bool isDisposed;

        bool Horizontal => position == Dock.Top || position == Dock.Bottom;

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

        public WixUiDesignerMargin(WixUiDocument document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.document = document ?? throw new ArgumentNullException(nameof(document));
            position = WixUiDesignerPackage.Options?.DesignerPosition ?? Options.DefaultDesignerPosition;
            displayContainer = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            CreateFramework();
            UpdateControls();
            this.document.UpdateRequired += OnUpdateRequired;
            this.document.Closed += OnClosed;
        }
        public void Dispose()
        {
            if (isDisposed) return;
            Logger.Log(DebugContext.WiX, $"Closing margin for {document.FileName}.");
            document.UpdateRequired -= OnUpdateRequired;
            document.Closed -= OnClosed;
            document.Dispose();
            GC.SuppressFinalize(this);
            isDisposed = true;
        }

        public ITextViewMargin? GetTextViewMargin(string marginName) => marginName switch
        {
            nameof(WixUiDesignerLeftMarginFactory) or nameof(WixUiDesignerRightMarginFactory) or nameof(WixUiDesignerTopMarginFactory) or nameof(WixUiDesignerBottomMarginFactory) => this,
            _ => null,
        };

        void CreateFramework()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var size = WixUiDesignerPackage.Options?.DesignerSize ?? Options.DefaultDesignerSize;

            Grid grid = new();
            GridSplitter splitter = new ();
            if (Horizontal)
            {
                splitter.Height = 5;
                splitter.ResizeDirection = GridResizeDirection.Rows;
                grid.ColumnDefinitions.Add(new());
            }
            else
            {
                splitter.Width = 5;
                splitter.ResizeDirection = GridResizeDirection.Columns;
                grid.RowDefinitions.Add(new());
            }
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;

            grid.Children.Add(displayContainer);

            switch (position)
            {
                case Dock.Top:
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel), MinHeight = 150 });
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    Grid.SetColumn(displayContainer, 0);
                    Grid.SetRow(displayContainer, 0);
                    break;
                case Dock.Bottom:
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel), MinHeight = 150 });
                    Grid.SetColumn(displayContainer, 0);
                    Grid.SetRow(displayContainer, 2);
                    break;
                case Dock.Left:
                    grid.ColumnDefinitions.Add(new() { Width = new(size, GridUnitType.Pixel), MinWidth = 150 });
                    grid.ColumnDefinitions.Add(new() { Width = new(5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Star) });
                    Grid.SetRow(displayContainer, 0);
                    Grid.SetColumn(displayContainer, 0);
                    break;
                case Dock.Right:
                    grid.ColumnDefinitions.Add(new () { Width = new (0, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new () { Width = new (5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new () { Width = new (size, GridUnitType.Pixel), MinWidth = 150 });
                    Grid.SetRow(displayContainer, 0);
                    Grid.SetColumn(displayContainer, 2);
                    break;
            }

            grid.Children.Add(splitter);

            Grid.SetColumn(splitter, Horizontal ? 0 : 1);
            Grid.SetRow(splitter, Horizontal ? 1 : 0);

            Children.Add(grid);
            splitter.DragCompleted += SplitterDragCompleted;
        }
        void SplitterDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (WixUiDesignerPackage.Options is null) return;

            double size = Horizontal ? displayContainer.ActualHeight : displayContainer.ActualWidth;
            if (double.IsNaN(size)) return;

            WixUiDesignerPackage.Options.DesignerSize = (int)size;
            WixUiDesignerPackage.Options.SaveSettingsToStorage();
        }

        void OnUpdateRequired(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateControls();
        }
        void OnClosed(object sender, EventArgs e) => Dispose();
        
        void UpdateControls()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Logger.Log(DebugContext.WiX, $"Updating controls for {document.FileName}.");

            try
            {

                var xml = document.Xml;
                var dialogNode = xml.GetDialogNode();
                var dialog = displayContainer.Content as Grid;

                if (!(int.TryParse(dialogNode.Attribute("Width")?.Value ?? throw Errors.InvalidDialogSize(), out var width) &&
                      int.TryParse(dialogNode.Attribute("Height")?.Value ?? throw Errors.InvalidDialogSize(), out var height)))
                    throw Errors.InvalidDialogSize();

                var brush = new LinearGradientBrush(Colors.Green, Colors.Red, new (0, 0), new Point(1, 1));
                dialog ??= new (){Background = brush};
                dialog.Width = width;
                dialog.Height = height;
                displayContainer.Content = dialog;
            }
            catch (Exception exception)
            {
                Logger.LogError($"Failed to render WiX UI document: {exception}");
            }
        }

        void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(WixUiDesignerMargin));
        }
    }
}
