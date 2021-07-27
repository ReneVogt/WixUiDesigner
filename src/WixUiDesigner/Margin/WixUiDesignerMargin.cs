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
    internal class WixUiDesignerMargin : DockPanel, IWpfTextViewMargin
    {
        readonly Dock position;
        readonly Canvas canvas;
        readonly IWpfTextView textView;

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

        public WixUiDesignerMargin(IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
            position = WixUiDesignerPackage.Options?.DesignerPosition ?? Options.DefaultDesignerPosition;
            canvas = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true
            };
            canvas.Background = Brushes.Red;

            CreateControls();
        }
        public void Dispose()
        {
            if (isDisposed) return;
            GC.SuppressFinalize(this);
            isDisposed = true;
        }

        public ITextViewMargin? GetTextViewMargin(string marginName) => marginName switch
        {
            nameof(WixUiDesignerLeftMarginFactory) or nameof(WixUiDesignerRightMarginFactory) or nameof(WixUiDesignerTopMarginFactory) or nameof(WixUiDesignerBottomMarginFactory) => this,
            _ => null,
        };

        void CreateControls()
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

            grid.Children.Add(canvas);

            switch (position)
            {
                case Dock.Top:
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel), MinHeight = 150 });
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    Grid.SetColumn(canvas, 0);
                    Grid.SetRow(canvas, 0);
                    break;
                case Dock.Bottom:
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel), MinHeight = 150 });
                    Grid.SetColumn(canvas, 0);
                    Grid.SetRow(canvas, 2);
                    break;
                case Dock.Left:
                    grid.ColumnDefinitions.Add(new() { Width = new(size, GridUnitType.Pixel), MinWidth = 150 });
                    grid.ColumnDefinitions.Add(new() { Width = new(5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Star) });
                    Grid.SetRow(canvas, 0);
                    Grid.SetColumn(canvas, 0);
                    break;
                case Dock.Right:
                    grid.ColumnDefinitions.Add(new () { Width = new (0, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new () { Width = new (5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new () { Width = new (size, GridUnitType.Pixel), MinWidth = 150 });
                    Grid.SetRow(canvas, 0);
                    Grid.SetColumn(canvas, 2);
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

            double size = Horizontal ? canvas.ActualHeight : canvas.ActualWidth;
            if (double.IsNaN(size)) return;

            WixUiDesignerPackage.Options.DesignerSize = (int)size;
            WixUiDesignerPackage.Options.SaveSettingsToStorage();
        }

        void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(WixUiDesignerMargin));
        }
    }
}
