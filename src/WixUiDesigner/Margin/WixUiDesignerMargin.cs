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
        void OnClosed(object sender, EventArgs e) => Dispose();

        void CreateFramework()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var size = 0.5 * (Horizontal ? document.WpfTextView.ViewportHeight : document.WpfTextView.ViewportWidth);

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
            //splitter.DragCompleted += OnSplitterDragged;
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

                UpdateControls(dialog, dialogNode, selectedElement);
            }
            catch (Exception exception)
            {
                Logger.LogError($"Failed to render WiX UI document: {exception}");
            }
        }
        static void UpdateControls(Grid parentControl, XElement parentNode, XElement? selectedElement)
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

        #region Control renderer
        static Control? UpdateControl(Grid parentControl, XElement node, XElement? selectedElement) => node.Attribute("Type")?.Value switch
        {
            "Billboard" => UpdateBillboardControl(parentControl, node, selectedElement),
            "Bitmap" => UpdateBitmapControl(parentControl, node, selectedElement),
            "CheckBox" => UpdateCheckBoxControl(parentControl, node, selectedElement),
            "ComboBox" => UpdateComboBoxControl(parentControl, node, selectedElement),
            "DirectoryCombo" => UpdateDirectoryComboControl(parentControl, node, selectedElement),
            "DirectoryList" => UpdateDirectoryListControl(parentControl, node, selectedElement),
            "Edit" => UpdateEditControl(parentControl, node, selectedElement),
            "GroupBox" => UpdateGroupBoxControl(parentControl, node, selectedElement),
            "Hyperlink" => UpdateHyperlinkControl(parentControl, node, selectedElement),
            "Icon" => UpdateIconControl(parentControl, node, selectedElement),
            "Line" => UpdateLineControl(parentControl, node, selectedElement),
            "ListBox" => UpdateListBoxControl(parentControl, node, selectedElement),
            "ListView" => UpdateListViewControl(parentControl, node, selectedElement),
            "MaskedEdit" => UpdateMaskedEditControl(parentControl, node, selectedElement),
            "PathEdit" => UpdatePathEditControl(parentControl, node, selectedElement),
            "ProgressBar" => UpdateProgressBarControl(parentControl, node, selectedElement),
            "PushButton" => UpdatePushButtonControl(parentControl, node, selectedElement),
            "RadioButtonGroup" => UpdateRadioButtonGroupControl(parentControl, node, selectedElement),
            "ScrollableText" => UpdateScrollableTextControl(parentControl, node, selectedElement),
            "SelectionTree" => UpdateSelectionTreeControl(parentControl, node, selectedElement),
            "Text" => UpdateTextControl(parentControl, node, selectedElement),
            "VolumnCostList" => UpdateVolumnCostListControl(parentControl, node, selectedElement),
            "VolumnSelectCombo" => UpdateVolumnSelectComboControl(parentControl, node, selectedElement),
            _ => HandleUnknownControlType(node)
        };
        static Control? UpdateBillboardControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateBitmapControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateCheckBoxControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateComboBoxControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateDirectoryComboControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateDirectoryListControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateEditControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateGroupBoxControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateHyperlinkControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateIconControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateLineControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateListBoxControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateListViewControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateMaskedEditControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdatePathEditControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateProgressBarControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdatePushButtonControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateRadioButtonGroupControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateScrollableTextControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateSelectionTreeControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateTextControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            var id = node.Attribute("Id")?.Value;
            if (string.IsNullOrWhiteSpace(id))
            {
                Logger.Log(DebugContext.Margin | DebugContext.WiX, "Found label without id!");
                return null;
            }

            Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Updating text control {id}.");
            var label = parentControl.Children.OfType<Label>().FirstOrDefault(l => l.Name == id) ?? new Label
            {
                Name = id,
                Content = node.EvaluateAttribute("Text"),
                Padding = default,
                Margin = default
            };
            LayoutControl(label, node);
            CheckAdornment(label, node, selectedElement);
            return label;
        }
        static Control? UpdateVolumnCostListControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? UpdateVolumnSelectComboControl(Grid parentControl, XElement node, XElement? selectedElement)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of type {node.Attribute("Type")?.Value ?? "<null>"} which is not yet supported!");
            return null;
        }
        static Control? HandleUnknownControlType(XElement node)
        {
            Logger.LogError($"Control {node.Attribute("Id")?.Value ?? "<null>"}] is of unknown type {node.Attribute("Type")?.Value ?? "<null>"}!");
            return null;
        }
        #endregion

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
