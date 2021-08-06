/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Linq;
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
        const double MinimumSize = 150;

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

            this.document.UpdateRequired += OnUpdateRequired;
            this.document.Closed += OnClosed;
            if (Horizontal)
                this.document.WpfTextView.ViewportHeightChanged += OnViewPortChanged;
            else
                this.document.WpfTextView.ViewportWidthChanged += OnViewPortChanged;
        }
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            Logger.Log(DebugContext.WiX, $"Closing margin for {document.FileName}.");
            updateTimer.Tick -= OnUpdateTimerTicked;
            updateTimer.Stop();
            document.WpfTextView.ViewportHeightChanged -= OnViewPortChanged;
            document.WpfTextView.ViewportWidthChanged -= OnViewPortChanged;
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
        void OnClosed(object sender, EventArgs e) => Dispose();

        void CreateFramework()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var size = Math.Max(MinimumSize, 0.4 * (Horizontal ? document.WpfTextView.ViewportHeight : document.WpfTextView.ViewportWidth));

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
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel), MinHeight = MinimumSize });
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    Grid.SetColumn(displayContainer, 0);
                    Grid.SetRow(displayContainer, 0);
                    break;
                case Dock.Bottom:
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel), MinHeight = MinimumSize });
                    Grid.SetColumn(displayContainer, 0);
                    Grid.SetRow(displayContainer, 2);
                    break;
                case Dock.Left:
                    grid.ColumnDefinitions.Add(new() { Width = new(size, GridUnitType.Pixel), MinWidth = MinimumSize });
                    grid.ColumnDefinitions.Add(new() { Width = new(5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Star) });
                    Grid.SetRow(displayContainer, 0);
                    Grid.SetColumn(displayContainer, 0);
                    break;
                case Dock.Right:
                    grid.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new() { Width = new(5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new() { Width = new(size, GridUnitType.Pixel), MinWidth = MinimumSize });
                    Grid.SetRow(displayContainer, 0);
                    Grid.SetColumn(displayContainer, 2);
                    break;
            }

            grid.Children.Add(splitter);

            Grid.SetColumn(splitter, Horizontal ? 0 : 1);
            Grid.SetRow(splitter, Horizontal ? 1 : 0);

            Children.Add(grid);
        }
        void UpdateControls()
        {
            if (isDisposed) return;
            ThreadHelper.ThrowIfNotOnUIThread();

            Logger.Log(DebugContext.Margin, $"Updating controls for {document.FileName}.");

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

                UpdateControls(dialog, dialogNode, selectedElement);
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.Margin|DebugContext.WiX|DebugContext.Exceptions,$"Failed to render WiX UI document: {exception}");
            }
        }
        static void UpdateControls(Grid parentControl, XElement parentNode, XElement? selectedElement)
        {
            try
            {
                var usedControls = parentNode.GetControlNodes()
                                             .Select(node => UpdateControl(parentControl, node, selectedElement))
                                             .Where(control => control is not null)
                                             .ToList();
                var controlsToRemove = parentControl.Children.Cast<Control>()
                                                    .Where(control => !usedControls.Contains(control))
                                                    .ToList();
                controlsToRemove.ForEach(control => parentControl.Children.Remove(control));

                var controlsToAdd = usedControls.Where(control => !parentControl.Children.Contains(control)).ToList();
                controlsToAdd.ForEach(control => parentControl.Children.Add(control!));
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.WiX|DebugContext.Margin|DebugContext.Exceptions, $"UpdateControls failed: {exception}");
            }
        }

        #region Control renderer
        static Control? UpdateControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            var id = node.Attribute("Id")?.Value!;
            if (string.IsNullOrWhiteSpace(id))
            {
                var (line, column) = node.GetPosition();
                Logger.Log(DebugContext.Margin | DebugContext.WiX, $"Found control without id at ({line}, {column})!");
                return null;
            }
            var type = node.Attribute("Type")?.Value!;
            if (string.IsNullOrWhiteSpace(type))
            {
                var (line, column) = node.GetPosition();
                Logger.Log(DebugContext.Margin | DebugContext.WiX, $"Found control without type at ({line}, {column})!");
                return null;
            }

            return type switch
            {
                //"Billboard" => UpdateBillboardControl(id, parentControl, node, selectedElement),
                //"Bitmap" => UpdateBitmapControl(id, parentControl, node, selectedElement),
                //"CheckBox" => UpdateCheckBoxControl(id, parentControl, node, selectedElement),
                //"ComboBox" => UpdateComboBoxControl(id, parentControl, node, selectedElement),
                //"DirectoryCombo" => UpdateDirectoryComboControl(id, parentControl, node, selectedElement),
                //"DirectoryList" => UpdateDirectoryListControl(id, parentControl, node, selectedElement),
                "Edit" => UpdateEditControl(id, parentControl, node, selectedElement),
                //"GroupBox" => UpdateGroupBoxControl(id, parentControl, node, selectedElement),
                //"Hyperlink" => UpdateHyperlinkControl(id, parentControl, node, selectedElement),
                //"Icon" => UpdateIconControl(id, parentControl, node, selectedElement),
                "Line" => UpdateLineControl(id, parentControl, node, selectedElement),
                //"ListBox" => UpdateListBoxControl(id, parentControl, node, selectedElement),
                //"ListView" => UpdateListViewControl(id, parentControl, node, selectedElement),
                "MaskedEdit" => UpdateMaskedEditControl(id, parentControl, node, selectedElement),
                //"PathEdit" => UpdatePathEditControl(id, parentControl, node, selectedElement),
                //"ProgressBar" => UpdateProgressBarControl(id, parentControl, node, selectedElement),
                "PushButton" => UpdatePushButtonControl(id, parentControl, node, selectedElement),
                //"RadioButtonGroup" => UpdateRadioButtonGroupControl(id, parentControl, node, selectedElement),
                //"ScrollableText" => UpdateScrollableTextControl(id, parentControl, node, selectedElement),
                //"SelectionTree" => UpdateSelectionTreeControl(id, parentControl, node, selectedElement),
                "Text" => UpdateTextControl(id, parentControl, node, selectedElement),
                //"VolumnCostList" => UpdateVolumnCostListControl(id, parentControl, node, selectedElement),
                //"VolumnSelectCombo" => UpdateVolumnSelectComboControl(id, parentControl, node, selectedElement),
                _ => HandleUnknownControlType(id, type, node)
            };
        }
        static Control? UpdateEditControl(string id, Grid parentControl, XElement node, XElement? selectedElement)
        {
            try
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Updating edit control {id}.");
                var textBox = parentControl.Children.OfType<TextBox>().FirstOrDefault(l => l.Name == id) ?? new TextBox
                {
                    Name = id,
                    Padding = default,
                    Margin = default
                };
                textBox.Text = node.EvaluateTextValue();
                if (node.IsMultiLine())
                {
                    textBox.AcceptsReturn = true;
                    textBox.TextWrapping = TextWrapping.Wrap;
                }
                else
                {
                    textBox.AcceptsReturn = false;
                    textBox.TextWrapping = TextWrapping.NoWrap;
                }
                LayoutControl(textBox, node);
                CheckAdornment(textBox, node, selectedElement);
                return textBox;
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin | DebugContext.Exceptions, $"Failed to update edit control {id}: {exception}");
                return null;
            }
        }
        static Control? UpdateLineControl(string id, Grid parentControl, XElement node, XElement? selectedElement)
        {
            try
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Updating line control {id}.");

                var line = parentControl.Children.OfType<Separator>().FirstOrDefault(l => l.Name == id) ?? new Separator
                {
                    Name = id,
                    Padding = default,
                    Margin = default
                };
                LayoutControl(line, node);
                line.Height = Math.Max(line.Height, 1);
                line.Width = Math.Max(line.Width, 1);
                CheckAdornment(line, node, selectedElement);
                return line;
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin | DebugContext.Exceptions, $"Failed to update button control {id}: {exception}");
                return null;
            }
        }
        static Control? UpdateMaskedEditControl(string id, Grid parentControl, XElement node, XElement? selectedElement) =>
            UpdateEditControl(id, parentControl, node, selectedElement);

        static Control? UpdatePushButtonControl(string id, Grid parentControl, XElement node, XElement? selectedElement)
        {
            try
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Updating button control {id}.");
                if (node.IsBitmap())
                    throw Errors.BitmapButtonNotSupported();
                if (node.IsIcon())
                    throw Errors.IconButtonNotSupported();
                if (node.IsImage())
                    throw Errors.BitmapButtonNotSupported();

                var button = parentControl.Children.OfType<Button>().FirstOrDefault(l => l.Name == id) ?? new Button
                {
                    Name = id,
                    Padding = default,
                    Margin = default
                };
                button.Content = node.EvaluateTextValue();
                LayoutControl(button, node);
                CheckAdornment(button, node, selectedElement);
                return button;
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin | DebugContext.Exceptions, $"Failed to update button control {id}: {exception}");
                return null;
            }
        }
        static Control? UpdateTextControl(string id, Grid parentControl, XElement node, XElement? selectedElement)
        {
            try
            {
                Logger.Log(DebugContext.WiX, $"Updating text control {id}.");
                var label = parentControl.Children.OfType<Label>().FirstOrDefault(l => l.Name == id) ?? new Label
                {
                    Name = id,
                    Padding = default,
                    Margin = default
                };
                label.Content = node.EvaluateTextValue();
                LayoutControl(label, node);
                CheckAdornment(label, node, selectedElement);
                return label;
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.WiX|DebugContext.Margin|DebugContext.Exceptions, $"Failed to update text control {id}: {exception}");
                return null;
            }
        }
        static Control? HandleUnknownControlType(string id, string type, XElement node)
        {
            var (line, column) = node.GetPosition();
            Logger.Log(DebugContext.WiX, $"Control {id} at ({line}, {column}) is of type {type} which is not supported!");
            return null;
        }
        #endregion

        void OnViewPortChanged(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Logger.Log(DebugContext.Margin, $"Viewport size of {document.FileName} changed: ({document.WpfTextView.ViewportWidth}, {document.WpfTextView.ViewportHeight}).");
            document.WpfTextView.ViewportHeightChanged -= OnViewPortChanged;
            document.WpfTextView.ViewportWidthChanged -= OnViewPortChanged;
            CreateFramework();
            OnUpdateRequired(this, EventArgs.Empty);
        }

        static void LayoutControl(Control control, XElement node)
        {
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment = VerticalAlignment.Top;

            var margin = control.Margin;
            margin.Left = node.EvaluateDoubleAttribute("X", margin.Left);
            margin.Top = node.EvaluateDoubleAttribute("Y", margin.Top);
            control.Margin = margin;
            control.Width = node.EvaluateDoubleAttribute("Width", control.Width);
            control.Height = node.EvaluateDoubleAttribute("Height", control.Height);

            control.IsEnabled = node.IsEnabledControl();
            control.Visibility = node.GetControlVisibility();
        }
        static void CheckAdornment(Control control, XElement node, XElement? selectedElement)
        {
            var layer = AdornerLayer.GetAdornerLayer(control);
            if (layer is null) return;
            var adorner = layer.GetAdorners(control)?.OfType<SelectedElementAdorner>().SingleOrDefault();
            if (node == selectedElement && adorner is null)
                layer.Add(new SelectedElementAdorner(control));
            if (node != selectedElement && adorner is not null)
                layer.Remove(adorner);
        }
    }
}
