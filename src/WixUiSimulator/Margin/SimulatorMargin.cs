/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using WixUiSimulator.Document;
using WixUiSimulator.Exceptions;
using WixUiSimulator.Logging;

#nullable enable

namespace WixUiSimulator.Margin
{
    sealed class SimulatorMargin : DockPanel, IWpfTextViewMargin
    {
        readonly Dock position;
        readonly WixUiDocument document;
        readonly DispatcherTimer updateTimer;

        readonly DispatcherTimer scalingAdornerTimer;
        readonly ScalingAdorner scalingAdorner;

        readonly Grid dialogGrid;
        readonly Grid dialog;
        readonly Label caption;
        readonly ScaleTransform scaleTransform;

        bool isDisposed;
        bool documentChanged, selectionChanged, wixChanged;

        XElement? selectedElement;
        FrameworkElement? selectedControl;
        SelectedElementAdorner? selectionAdorner;

        bool Horizontal => position is Dock.Top or Dock.Bottom;
        double ViewportSize => Horizontal ? document.WpfTextView.ViewportHeight : document.WpfTextView.ViewportWidth;
        public double MarginSize => Horizontal ? ActualHeight : ActualWidth;
        double Scaling
        {
            get => scaleTransform.ScaleX;
            set
            {
                if (value is <0.1 or >4) return;
                scaleTransform.ScaleX = scaleTransform.ScaleY = value;
            }
        }

        public FrameworkElement VisualElement => this;
        public bool Enabled => !isDisposed;

        public SimulatorMargin(WixUiDocument document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.document = document;
            position = WixUiSimulatorPackage.Options?.SimulatorPosition ?? Options.DefaultSimulatorPosition;

            updateTimer = new() { Interval = TimeSpan.FromSeconds(WixUiSimulatorPackage.Options?.UpdateInterval ?? Options.DefaultUpdateInterval) };
            updateTimer.Tick += OnUpdateTimerTicked;

            dialog = new() {Background = SystemColors.ControlBrush};
            dialog.PreviewMouseLeftButtonUp += OnControlClicked;
            caption = new()
            {
                Background = SystemColors.ActiveCaptionBrush, 
                Foreground = SystemColors.ActiveCaptionTextBrush, 
                Height = 25,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            caption.PreviewMouseLeftButtonUp += OnControlClicked;
            scaleTransform = new(1, 1);
            dialogGrid = new()
            {
                Margin = new(20),
                RowDefinitions =
                {
                    new() {Height = new(0, GridUnitType.Auto)},
                    new() {Height = new(0, GridUnitType.Auto)}
                },
                LayoutTransform = scaleTransform
            };
            var decorator = new AdornerDecorator {Child = dialog};
            dialogGrid.Children.Add(caption);
            dialogGrid.Children.Add(decorator);
            Grid.SetRow(caption, 0);
            Grid.SetRow(decorator, 1);

            scalingAdorner = new (dialog);
            scalingAdornerTimer = new() {Interval = TimeSpan.FromMilliseconds(500)};
            scalingAdornerTimer.Tick += (_, _) =>
            {
                scalingAdornerTimer.Stop();
                scalingAdorner.Visible = false;
            };

            this.document.WpfTextView.VisualElement.Loaded += OnEditorLoaded;
            
            this.document.UpdateRequired += OnUpdateRequired;
            this.document.Closed += OnClosed;
        }
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            Logger.Log(DebugContext.Margin, $"Closing margin for {document.FileName}.");
            updateTimer.Tick -= OnUpdateTimerTicked;
            updateTimer.Stop();
            document.WpfTextView.VisualElement.Loaded -= OnEditorLoaded;
            document.UpdateRequired -= OnUpdateRequired;
            document.Closed -= OnClosed;
            document.Dispose();
        }

        public ITextViewMargin? GetTextViewMargin(string marginName) => marginName switch
        {
            nameof(LeftMarginFactory) or nameof(RightMarginFactory) or nameof(TopMarginFactory) or nameof(BottomMarginFactory) => this,
            _ => null
        };

        void OnEditorLoaded(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Logger.Log(DebugContext.Margin, $"Editor loaded for {document.FileName}.");
            CreateFramework();
            UpdateControls();
        }
        void OnUpdateTimerTicked(object sender, EventArgs e)
        {
            updateTimer.Stop();
            ThreadHelper.ThrowIfNotOnUIThread();

            bool updateControls = wixChanged || documentChanged;
            bool updateSelection = selectionChanged || documentChanged;
            wixChanged = documentChanged = selectionChanged = false;

            if (updateControls)
                UpdateControls();
            if (updateSelection)
                UpdateSelection();
        }
        void OnUpdateRequired(object sender, UpdateRequiredEventArgs e)
        {
            if (isDisposed) return;
            ThreadHelper.ThrowIfNotOnUIThread();
            documentChanged |= e.DocumentChanged;
            selectionChanged |= e.SelectionChanged;
            wixChanged |= e.WixChanged;
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
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
            if (e.Delta == 0 || !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return;
            int change = e.Delta / 120;
            var delta = 0.1 * change;
            Scaling += delta;
            if (Scaling is >0.9 and <1.1) Scaling = 1; // fix rounding problems
            scalingAdornerTimer.Stop();
            scalingAdorner.Visible = true;
            scalingAdorner.Percentage = (int)(100 * Scaling);
            scalingAdornerTimer.Start();
            Logger.Log(DebugContext.Margin, $"Rescaling by {delta} to {Scaling}.");
        }

        void CreateFramework()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var size = (WixUiSimulatorPackage.Options?.SimulatorSize ?? Options.DefaultSimulatorSize) * (ViewportSize + MarginSize);
            if (dialogGrid.Parent is ScrollViewer oldContainer)
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
                Content = dialogGrid
            };
            grid.Children.Add(displayContainer);
            var adornerLayer = AdornerLayer.GetAdornerLayer(dialog);
            if (adornerLayer?.GetAdorners(dialog)?.Contains(scalingAdorner) != true)
                adornerLayer?.Add(scalingAdorner);

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
                dialog.Tag = caption.Tag = dialogNode;

                if (!(int.TryParse(dialogNode.Attribute("Width")?.Value ?? throw Errors.InvalidDialogSize(), out var width) &&
                      int.TryParse(dialogNode.Attribute("Height")?.Value ?? throw Errors.InvalidDialogSize(), out var height)))
                    throw Errors.InvalidDialogSize();

                dialog.Name = dialogNode.GetId();
                dialog.Width = width;
                dialog.Height = height;

                caption.Name = dialog.Name + "caption";
                caption.Width = width;
                caption.Content = dialogNode.EvaluateAttribute("Title");

                scaleTransform.CenterX = dialogGrid.ActualWidth / 2;
                scaleTransform.CenterY = dialogGrid.ActualHeight / 2;

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

                var controlsToRemove = parentControl.Children.Cast<FrameworkElement>()
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
        FrameworkElement? UpdateControl(Grid parentControl, XElement node)
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

            try
            {
                return type switch
                {
                    //"Billboard" => UpdateBillboardControl(id, parentControl, node),
                    "Bitmap" => UpdateBitmapControl(id, parentControl, node),
                    "CheckBox" => UpdateCheckBoxControl(id, parentControl, node),
                    "ComboBox" => UpdateComboBoxControl(id, parentControl, node),
                    //"DirectoryCombo" => UpdateDirectoryComboControl(id, parentControl, node),
                    //"DirectoryList" => UpdateDirectoryListControl(id, parentControl, node),
                    "Edit" => UpdateEditControl(id, parentControl, node),
                    //"GroupBox" => UpdateGroupBoxControl(id, parentControl, node),
                    //"Hyperlink" => UpdateHyperlinkControl(id, parentControl, node),
                    "Icon" => UpdateIconControl(id, parentControl, node),
                    "Line" => UpdateLineControl(id, parentControl, node),
                    //"ListBox" => UpdateListBoxControl(id, parentControl, node),
                    //"ListView" => UpdateListViewControl(id, parentControl, node),
                    "MaskedEdit" => UpdateMaskedEditControl(id, parentControl, node),
                    "PathEdit" => UpdatePathEditControl(id, parentControl, node),
                    "ProgressBar" => UpdateProgressBarControl(id, parentControl, node),
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
            catch (Exception exception)
            {
                Logger.Log(DebugContext.WiX | DebugContext.Margin | DebugContext.Exceptions, $"Failed to update control {id} of type {type}: {exception}");
                return null;
            }
        }
        FrameworkElement UpdateBitmapControl(string id, Grid parentControl, XElement node)
        {
            var image = parentControl.Children.OfType<Image>().FirstOrDefault(l => l.Name == id) ?? new Image
            {
                Name = id,
                Margin = default,
                Stretch = Stretch.Fill
            };
            image.Source = node.GetImageSource();
            LayoutControl(image, node);
            return image;
        }
        Control UpdateCheckBoxControl(string id, Grid parentControl, XElement node)
        {
            var checkBox = node.IsPushLike()
                               ? parentControl.Children.OfType<ToggleButton>().FirstOrDefault(l => l.Name == id && l.GetType() == typeof(ToggleButton)) ?? new ToggleButton()
                               : parentControl.Children.OfType<CheckBox>().FirstOrDefault(l => l.Name == id) ?? new CheckBox();
            checkBox.Name = id;
            checkBox.Padding = default;
            checkBox.Margin = default;
            checkBox.Content = node.EvaluateTextValue();
            LayoutControl(checkBox, node);
            return checkBox;
        }
        FrameworkElement UpdateComboBoxControl(string id, Grid parentControl, XElement node)
        {
            var comboBox = parentControl.Children.OfType<ComboBox>().FirstOrDefault(l => l.Name == id) ?? new ComboBox
            {
                Name = id
            };
            comboBox.IsEditable = !node.IsComboList();
            string[] items = node.GetComboBoxItems();
            if (items.Length != comboBox.Items.Count || items.Select((s, i) => (s, i)).Any(x => x.s != (string)comboBox.Items[x.i]))
            {
                comboBox.Items.Clear();
                foreach(var s in items) comboBox.Items.Add(s);
            }

            if (comboBox.Items.Count > 0 && comboBox.SelectedIndex < 0) comboBox.SelectedIndex = 0;

            LayoutControl(comboBox, node);
            return comboBox;
        }
        RichTextBox UpdateEditControl(string id, Grid parentControl, XElement node)
        {
            var textBox = parentControl.Children.OfType<RichTextBox>().FirstOrDefault(l => l.Name == id) ?? new RichTextBox
            {
                Name = id,
                Padding = default,
                Margin = default
            };
            string content = node.EvaluateTextValue() ?? Environment.NewLine;
            if (content != textBox.Document.Tag?.ToString())
            {
                textBox.Document.Tag = content;
                var contentBytes = Encoding.UTF8.GetBytes(content);
                using var stream = new MemoryStream(contentBytes);
                stream.Position = 0;
                textBox.SelectAll();
                try
                {
                    textBox.Selection.Load(stream, DataFormats.Rtf);
                }
                catch (Exception exception)
                {
                    Logger.Log(DebugContext.Margin | DebugContext.Exceptions, $"Failed to parse RTF for control {id} in {document.FileName}: {exception}.");
                    try
                    {
                        textBox.Selection.Load(stream, DataFormats.UnicodeText);
                    }
                    catch (Exception e2)
                    {
                        Logger.Log(DebugContext.Margin | DebugContext.Exceptions, $"Failed to parse unicode text for control {id} in {document.FileName}: {e2}.");
                        try
                        {
                            textBox.Selection.Load(stream, DataFormats.Text);
                        }
                        catch (Exception e3)
                        {
                            Logger.Log(DebugContext.Margin | DebugContext.Exceptions, $"Failed to parse text for control {id} in {document.FileName}: {e3}.");
                        }
                    }
                }
            }
            ScrollViewer.SetHorizontalScrollBarVisibility(textBox, ScrollBarVisibility.Hidden);
            ScrollViewer.SetVerticalScrollBarVisibility(textBox, node.IsMultiLine() ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden);
            textBox.AcceptsReturn = true;
            LayoutControl(textBox, node);
            return textBox;
        }
        FrameworkElement UpdateIconControl(string id, Grid parentControl, XElement node) => UpdateBitmapControl(id, parentControl, node);
        Control UpdateLineControl(string id, Grid parentControl, XElement node)
        {
            var line = parentControl.Children.OfType<Separator>().FirstOrDefault(l => l.Name == id) ?? new Separator
            {
                Name = id,
                Padding = default,
                Margin = default
            };
            LayoutControl(line, node);
            line.Height = Math.Max(line.Height, 1);
            line.Width = Math.Max(line.Width, 1);
            return line;
        }
        Control UpdateMaskedEditControl(string id, Grid parentControl, XElement node) =>
            UpdateEditControl(id, parentControl, node);
        Control UpdatePathEditControl(string id, Grid parentControl, XElement node) =>
            UpdateEditControl(id, parentControl, node);
        Control UpdateProgressBarControl(string id, Grid parentControl, XElement node)
        {
            var progressBar = parentControl.Children.OfType<ProgressBar>().FirstOrDefault(l => l.Name == id) ?? new ProgressBar
            {
                Name = id,
                Padding = default,
                Margin = default
            };
            progressBar.Value = 50;
            LayoutControl(progressBar, node);
            return progressBar;
        }
        Control UpdatePushButtonControl(string id, Grid parentControl, XElement node)
        {
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
            return button;
        }
        RichTextBox UpdateScrollableTextControl(string id, Grid parentControl, XElement node)
        {
            var textBox = UpdateEditControl(id, parentControl, node);
            ScrollViewer.SetVerticalScrollBarVisibility(textBox, ScrollBarVisibility.Auto);
            return textBox;
        }
        FrameworkElement? UpdateTextControl(string id, Grid parentControl, XElement node)
        {
            var label = parentControl.Children.OfType<TextBlock>().FirstOrDefault(l => l.Name == id) ?? new TextBlock
            {
                Name = id,
                Padding = default,
                Margin = default,
                TextWrapping = TextWrapping.Wrap
            };
            label.Text = node.EvaluateTextValue();
            LayoutControl(label, node);
            return label;
        }
        static Control? HandleUnknownControlType(string id, string type, XElement node)
        {
            var (line, column) = node.GetPosition();
            Logger.Log(DebugContext.WiX | DebugContext.Margin, $"Control {id} at ({line}, {column}) is of type {type} which is not supported!");
            return null;
        }
        #endregion

        void LayoutControl(FrameworkElement control, XElement node)
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

            if (control is Control c)
                c.HorizontalContentAlignment = node.IsRightAligned() ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            control.FlowDirection = node.IsRightToLeft() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        void UpdateSelection()
        {
            var bufferPosition = document.WpfTextView.Caret.Position.BufferPosition;
            var containingLine = bufferPosition.GetContainingLine();
            int column = containingLine.Start.Difference(bufferPosition) + 1;
            int line = containingLine.LineNumber + 1;
            var nextSelectedElement = document.Xml.GetControlAt(line, column);
            if (nextSelectedElement == selectedElement) return;

            string id = nextSelectedElement?.GetId() ?? string.Empty;
            Logger.Log(DebugContext.Margin, $"Selected control in {document.FileName} changed to ({id}).");

            selectedElement = nextSelectedElement;

            var control = selectedElement is null ? null : FindByTag(dialog, selectedElement);
            if (control == selectedControl) return;

            if (selectedControl is not null && AdornerLayer.GetAdornerLayer(selectedControl) is {} oldLayer)
                oldLayer.Remove(selectionAdorner!);
            selectedControl = control;
            selectionAdorner = null;
            if (selectedControl is null || AdornerLayer.GetAdornerLayer(selectedControl) is not {} nextLayer) return;
            selectionAdorner = new(selectedControl);
            nextLayer.Add(selectionAdorner);
        }
        static FrameworkElement? FindByTag(FrameworkElement parent, object tag) => parent.Tag == tag
                                                                                ? parent
                                                                                : Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(parent))
                                                                                            .Select(i => VisualTreeHelper.GetChild(parent, i))
                                                                                            .OfType<FrameworkElement>()
                                                                                            .Select(child => FindByTag(child, tag))
                                                                                            .FirstOrDefault(child => child is not null);
    }
}
