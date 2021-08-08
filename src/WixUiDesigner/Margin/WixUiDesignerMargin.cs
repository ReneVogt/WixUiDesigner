/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
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
        readonly Grid dialog;
        readonly WixUiDocument document;
        readonly DispatcherTimer updateTimer;

        bool isDisposed;

        XElement? selectedElement;

        bool Horizontal => position is Dock.Top or Dock.Bottom;
        double ViewportSize => Horizontal ? document.WpfTextView.ViewportHeight : document.WpfTextView.ViewportWidth;
        public double MarginSize => Horizontal ? ActualHeight : ActualWidth;

        public FrameworkElement VisualElement => this;
        public bool Enabled => !isDisposed;

        public WixUiDesignerMargin(WixUiDocument document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.document = document ?? throw new ArgumentNullException(nameof(document));
            position = WixUiDesignerPackage.Options?.DesignerPosition ?? Options.DefaultDesignerPosition;

            updateTimer = new() { Interval = TimeSpan.FromSeconds(WixUiDesignerPackage.Options?.UpdateInterval ?? Options.DefaultUpdateInterval) };
            updateTimer.Tick += OnUpdateTimerTicked;

            dialog = new() {Background = SystemColors.ControlBrush, Margin = new(20)};
            dialog.PreviewMouseLeftButtonUp += OnControlClicked;

            this.document.WpfTextView.VisualElement.Loaded += OnEditorLoaded;
            
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
            document.WpfTextView.VisualElement.Loaded -= OnEditorLoaded;
            document.UpdateRequired -= OnUpdateRequired;
            document.Closed -= OnClosed;
            document.Dispose();
        }

        public ITextViewMargin? GetTextViewMargin(string marginName) => marginName switch
        {
            nameof(WixUiDesignerLeftMarginFactory) or nameof(WixUiDesignerRightMarginFactory) or nameof(WixUiDesignerTopMarginFactory) or nameof(WixUiDesignerBottomMarginFactory) => this,
            _ => null
        };

        void OnEditorLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CreateFramework();
            UpdateControls();
        }
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
        void OnControlClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement {Tag: IXmlLineInfo lineInfo} element || !lineInfo.HasLineInfo()) return;

            Logger.Log(DebugContext.Margin, $"Control {element.Name} clicked, setting caret to ({lineInfo.LineNumber}, {lineInfo.LinePosition}).");

            ITextSnapshotLine line = document.WpfTextView.TextSnapshot.GetLineFromLineNumber(lineInfo.LineNumber-1);
            SnapshotPoint point = new (line.Snapshot, line.Start.Position + lineInfo.LinePosition-1);
            document.WpfTextView.Caret.MoveTo(point);
            document.WpfTextView.VisualElement.Focus();
            document.WpfTextView.ViewScroller.EnsureSpanVisible(new (document.WpfTextView.Caret.Position.BufferPosition, 0));
        }

        void CreateFramework()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var size = (WixUiDesignerPackage.Options?.DesignerSize ?? Options.DefaultDesignerSize) * (ViewportSize + MarginSize);
            if (dialog.Parent is ScrollViewer oldContainer)
            {
                size = Horizontal ? oldContainer.ActualHeight : oldContainer.ActualWidth;
                oldContainer.Content = null;
            }

            Logger.Log(DebugContext.Margin, $"Creating framework for {document.FileName} (viewport: {ViewportSize}, old margin: {MarginSize}, new margin: {size}).");
            
            Children.Clear();

            GridSplitter splitter = new ();
            Grid grid = new();
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

            ScrollViewer displayContainer = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = dialog
            };
            grid.Children.Add(displayContainer);

            switch (position)
            {
                case Dock.Top:
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel)});
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    Grid.SetColumn(displayContainer, 0);
                    Grid.SetRow(displayContainer, 0);
                    break;
                case Dock.Bottom:
                    grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new() { Height = new(5, GridUnitType.Pixel) });
                    grid.RowDefinitions.Add(new() { Height = new(size, GridUnitType.Pixel)});
                    Grid.SetColumn(displayContainer, 0);
                    Grid.SetRow(displayContainer, 2);
                    break;
                case Dock.Left:
                    grid.ColumnDefinitions.Add(new() { Width = new(size, GridUnitType.Pixel)});
                    grid.ColumnDefinitions.Add(new() { Width = new(5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Star) });
                    Grid.SetRow(displayContainer, 0);
                    Grid.SetColumn(displayContainer, 0);
                    break;
                case Dock.Right:
                    grid.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new() { Width = new(5, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new() { Width = new(size, GridUnitType.Pixel)});
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
                dialog.Tag = dialogNode;

                if (!(int.TryParse(dialogNode.Attribute("Width")?.Value ?? throw Errors.InvalidDialogSize(), out var width) &&
                      int.TryParse(dialogNode.Attribute("Height")?.Value ?? throw Errors.InvalidDialogSize(), out var height)))
                    throw Errors.InvalidDialogSize();

                dialog.Name = dialogNode.GetId();
                dialog.Width = width;
                dialog.Height = height;

                var bufferPosition = document.WpfTextView.Caret.Position.BufferPosition;
                var containingLine = bufferPosition.GetContainingLine();
                int column = containingLine.Start.Difference(bufferPosition) + 1;
                int line = containingLine.LineNumber + 1;
                selectedElement = xml.GetControlAt(line, column);
                Logger.Log(DebugContext.Margin, $"Selected control: {selectedElement?.GetId() ?? "<null>"}");

                UpdateControls(dialog, dialogNode);
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.Margin|DebugContext.WiX|DebugContext.Exceptions,$"Failed to render WiX UI document: {exception}");
            }
        }
        void UpdateControls(Grid parentControl, XElement parentNode)
        {
            try
            {
                var usedControls = parentNode.GetControlNodes()
                                             .Select(node => UpdateControl(parentControl, node))
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
        Control? UpdateControl(Grid parentControl, XElement node)
        {
            var id = node.GetId();
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
                //"Billboard" => UpdateBillboardControl(id, parentControl, node),
                //"Bitmap" => UpdateBitmapControl(id, parentControl, node),
                "CheckBox" => UpdateCheckBoxControl(id, parentControl, node),
                //"ComboBox" => UpdateComboBoxControl(id, parentControl, node),
                //"DirectoryCombo" => UpdateDirectoryComboControl(id, parentControl, node),
                //"DirectoryList" => UpdateDirectoryListControl(id, parentControl, node),
                "Edit" => UpdateEditControl(id, parentControl, node),
                //"GroupBox" => UpdateGroupBoxControl(id, parentControl, node),
                //"Hyperlink" => UpdateHyperlinkControl(id, parentControl, node),
                //"Icon" => UpdateIconControl(id, parentControl, node),
                "Line" => UpdateLineControl(id, parentControl, node),
                //"ListBox" => UpdateListBoxControl(id, parentControl, node),
                //"ListView" => UpdateListViewControl(id, parentControl, node),
                "MaskedEdit" => UpdateMaskedEditControl(id, parentControl, node),
                "PathEdit" => UpdatePathEditControl(id, parentControl, node),
                //"ProgressBar" => UpdateProgressBarControl(id, parentControl, node),
                "PushButton" => UpdatePushButtonControl(id, parentControl, node),
                //"RadioButtonGroup" => UpdateRadioButtonGroupControl(id, parentControl, node),
                "ScrollableText" => UpdateScrollableTextControl(id, parentControl, node),
                //"SelectionTree" => UpdateSelectionTreeControl(id, parentControl, node),
                "Text" => UpdateTextControl(id, parentControl, node),
                //"VolumnCostList" => UpdateVolumnCostListControl(id, parentControl, node),
                //"VolumnSelectCombo" => UpdateVolumnSelectComboControl(id, parentControl, node),
                _ => HandleUnknownControlType(id, type, node)
            };
        }
        Control? UpdateCheckBoxControl(string id, Grid parentControl, XElement node)
        {
            try
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Updating checkbox control {id}.");
                var checkBox = node.IsPushLike()
                                   ? parentControl.Children.OfType<ToggleButton>().FirstOrDefault(l => l.Name == id && l.GetType() == typeof(ToggleButton)) ?? new ToggleButton()
                                   : parentControl.Children.OfType<CheckBox>().FirstOrDefault(l => l.Name == id) ?? new CheckBox();
                checkBox.Name = id;
                checkBox.Padding = default;
                checkBox.Margin = default;
                checkBox.Content = node.EvaluateTextValue();
                LayoutControl(checkBox, node);
                CheckAdornment(checkBox, node, selectedElement);
                return checkBox;
            }
            catch (Exception exception)
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin | DebugContext.Exceptions, $"Failed to update checkbox control {id}: {exception}");
                return null;
            }
        }
        Control? UpdateEditControl(string id, Grid parentControl, XElement node)
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
        Control? UpdateLineControl(string id, Grid parentControl, XElement node)
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
        Control? UpdateMaskedEditControl(string id, Grid parentControl, XElement node) =>
            UpdateEditControl(id, parentControl, node);
        Control? UpdatePathEditControl(string id, Grid parentControl, XElement node) =>
            UpdateEditControl(id, parentControl, node);
        Control? UpdatePushButtonControl(string id, Grid parentControl, XElement node)
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
        Control? UpdateScrollableTextControl(string id, Grid parentControl, XElement node)
        {
            if (UpdateEditControl(id, parentControl, node) is not TextBox textBox) return null;
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.NoWrap;
            ScrollViewer.SetHorizontalScrollBarVisibility(textBox, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(textBox, ScrollBarVisibility.Auto);
            return textBox;
        }

        Control? UpdateTextControl(string id, Grid parentControl, XElement node)
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

        void LayoutControl(Control control, XElement node)
        {
            if (control.Tag is null)
                control.PreviewMouseLeftButtonUp += OnControlClicked;
            control.Tag = node;

            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment = VerticalAlignment.Top;

            var margin = control.Margin;
            margin.Left = node.EvaluateDoubleAttribute("X", margin.Left);
            margin.Top = node.EvaluateDoubleAttribute("Y", margin.Top);
            control.Margin = margin;
            control.Width = node.EvaluateDoubleAttribute("Width", control.Width);
            control.Height = node.EvaluateDoubleAttribute("Height", control.Height);

            control.Visibility = node.GetControlVisibility();

            control.HorizontalContentAlignment = node.IsRightAligned() ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            control.FlowDirection = node.IsRightToLeft() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
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
