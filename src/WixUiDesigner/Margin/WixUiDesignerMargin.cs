/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using WixUiDesigner.Document;
using WixUiDesigner.Exceptions;
using WixUiDesigner.Logging;

#nullable enable

namespace WixUiDesigner.Margin
{
    sealed class WixUiDesignerMargin : DockPanel, IWpfTextViewMargin
    {
        readonly Dock position;
        readonly ScrollViewer displayContainer;
        readonly Grid dialog;
        readonly WixUiDocument document;
        readonly DispatcherTimer updateTimer;

        bool isDisposed;

        bool Horizontal => position == Dock.Top || position == Dock.Bottom;

        public FrameworkElement VisualElement => this;
        public double MarginSize => ActualHeight;
        public bool Enabled => !isDisposed;

        public WixUiDesignerMargin(WixUiDocument document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.document = document ?? throw new ArgumentNullException(nameof(document));
            position = WixUiDesignerPackage.Options?.DesignerPosition ?? Options.DefaultDesignerPosition;

            updateTimer = new (){Interval = TimeSpan.FromSeconds(WixUiDesignerPackage.Options?.UpdateInterval ?? Options.DefaultUpdateInterval)};
            updateTimer.Tick += OnUpdateTimerTicked;

            dialog = new() {Background = SystemColors.ControlBrush, Margin = new(20)};
            displayContainer = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = dialog
            };

            CreateFramework();
            UpdateControls();
            this.document.UpdateRequired += OnUpdateRequired;
            this.document.Closed += OnClosed;
        }
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            Logger.Log(DebugContext.WiX, $"Closing margin for {document.FileName}.");
            updateTimer.Tick -= OnUpdateTimerTicked;
            updateTimer.Stop();
            document.UpdateRequired -= OnUpdateRequired;
            document.Closed -= OnClosed;
            document.Dispose();
        }

        public ITextViewMargin? GetTextViewMargin(string marginName) => marginName switch
        {
            nameof(WixUiDesignerLeftMarginFactory) or nameof(WixUiDesignerRightMarginFactory) or nameof(WixUiDesignerTopMarginFactory) or nameof(WixUiDesignerBottomMarginFactory) => this,
            _ => null,
        };

        void OnUpdateTimerTicked(object sender, EventArgs e)
        {
            updateTimer.Stop();
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateControls();
        }
        void OnUpdateRequired(object sender, EventArgs e)
        {
            if (isDisposed) return;
            ThreadHelper.ThrowIfNotOnUIThread();
            updateTimer.Stop();
            updateTimer.Start();
        }
        void OnSplitterDragged(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (isDisposed) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            if (WixUiDesignerPackage.Options is null) return;

            double size = Horizontal ? displayContainer.ActualHeight : displayContainer.ActualWidth;
            if (double.IsNaN(size)) return;

            WixUiDesignerPackage.Options.DesignerSize = (int)size;
            WixUiDesignerPackage.Options.SaveSettingsToStorage();
        }
        void OnClosed(object sender, EventArgs e) => Dispose();

        void CreateFramework()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var size = WixUiDesignerPackage.Options?.DesignerSize ?? Options.DefaultDesignerSize;

            Grid grid = new();
            GridSplitter splitter = new();
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
                    grid.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new() { Width = new(5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new() { Width = new(size, GridUnitType.Pixel), MinWidth = 150 });
                    Grid.SetRow(displayContainer, 0);
                    Grid.SetColumn(displayContainer, 2);
                    break;
            }

            grid.Children.Add(splitter);

            Grid.SetColumn(splitter, Horizontal ? 0 : 1);
            Grid.SetRow(splitter, Horizontal ? 1 : 0);

            Children.Add(grid);
            splitter.DragCompleted += OnSplitterDragged;
        }
        void UpdateControls()
        {
            if (isDisposed) return;
            ThreadHelper.ThrowIfNotOnUIThread();

            Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Updating controls for {document.FileName}.");

            try
            {
                var xml = document.Xml;
                var dialogNode = xml.GetDialogNode();

                if (!(int.TryParse(dialogNode.Attribute("Width")?.Value ?? throw Errors.InvalidDialogSize(), out var width) &&
                      int.TryParse(dialogNode.Attribute("Height")?.Value ?? throw Errors.InvalidDialogSize(), out var height)))
                    throw Errors.InvalidDialogSize();

                dialog.Width = width;
                dialog.Height = height;

                var bufferPosition = document.WpfTextView.Caret.Position.BufferPosition;
                var containingLine = bufferPosition.GetContainingLine();
                int column = containingLine.Start.Difference(bufferPosition) + 1;
                int line = containingLine.LineNumber + 1;
                var selectedElement = xml.GetControlAt(line, column);
                Logger.Log(DebugContext.Margin, $"Selected control: {selectedElement?.Attribute("Id")?.Value ?? "<null>"}");

                Control? selectedControl = null;

                List<Control> controls = new();

                foreach (var controlNode in dialogNode.GetControlNodes())
                {
                    switch (controlNode.Attribute("Type")?.Value)
                    {
                        case "Edit": break;
                        case "Text":
                            Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Adding label {controlNode.Attribute("Id")?.Value ?? "<nulL>"}.");
                            var label = new Label
                            {
                                Content = controlNode.Attribute("Text")?.Value,
                            };
                            controls.Add(label);
                            LayoutControl(label, controlNode);
                            if (controlNode == selectedElement)
                                selectedControl = label;
                            break;
                        case "Line": break;
                        case "PushButton": break;
                        case "CheckBox": break;
                    }
                }

                dialog.Children.Clear();
                controls.ForEach(c => dialog.Children.Add(c));

                if (selectedControl is null) return;
                var adornerLayer = AdornerLayer.GetAdornerLayer(selectedControl);
                if (adornerLayer is null) return;
                adornerLayer.Add(new SelectedElementAdorner(selectedControl));
            }
            catch (Exception exception)
            {
                Logger.LogError($"Failed to render WiX UI document: {exception}");
            }
        }
        static void LayoutControl(Control control, XElement node)
        {
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment = VerticalAlignment.Top;

            var margin = control.Margin;
            
            if (double.TryParse(node.Attribute("X")?.Value ?? string.Empty, out var x))
                margin.Left = x;
            if (double.TryParse(node.Attribute("Y")?.Value ?? string.Empty, out var y))
                margin.Top = y;
            if (double.TryParse(node.Attribute("Width")?.Value ?? string.Empty, out var w))
                control.Width = w;
            if (double.TryParse(node.Attribute("Height")?.Value ?? string.Empty, out var h))
                control.Height = h;

            control.Margin = margin;
        }
    }
}
