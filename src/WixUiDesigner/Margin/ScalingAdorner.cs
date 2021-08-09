/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

#nullable enable
#pragma warning disable 618

namespace WixUiDesigner.Margin
{
    sealed class ScalingAdorner : Adorner
    {
        readonly Brush backgroundBrush = new SolidColorBrush(SystemColors.ControlColor) {Opacity = 0.6};
        readonly Typeface typeFace = new (
            (FontFamily)Control.FontFamilyProperty.DefaultMetadata.DefaultValue,
            (FontStyle)Control.FontStyleProperty.DefaultMetadata.DefaultValue,
            (FontWeight)Control.FontWeightProperty.DefaultMetadata.DefaultValue,
            (FontStretch)Control.FontStretchProperty.DefaultMetadata.DefaultValue);

        int percentage;
        bool visible;

        public int Percentage
        {
            get => percentage;
            set
            {
                if (value == percentage) return;
                percentage = value;
                InvalidateVisual();
            }
        }
        public bool Visible
        {
            get => visible;
            set
            {
                if (value == visible) return;
                visible = value;
                InvalidateVisual();
            }
        }

        public ScalingAdorner(UIElement adornedElement)
            : base(adornedElement) { }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (!visible) return;
            if (AdornedElement is not Grid grid) return;
            drawingContext.DrawRectangle(backgroundBrush, null, new(0, 0, grid.ActualWidth, grid.ActualHeight));
            var formattedText = new FormattedText($"{percentage}%", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeFace, 36,
                                                  SystemColors.ControlTextBrush)
            {
                TextAlignment = TextAlignment.Center
            };
            var point = new Point(grid.ActualWidth / 2, grid.ActualHeight / 2 - formattedText.Height / 2);
            drawingContext.DrawText(formattedText, point);
        }
    }
}
