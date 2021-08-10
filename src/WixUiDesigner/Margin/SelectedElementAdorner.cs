/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

#nullable enable

namespace WixUiDesigner.Margin
{
    sealed class SelectedElementAdorner : Adorner
    {
        readonly Pen pen = new (SystemColors.HighlightBrush, 1);
        public SelectedElementAdorner(UIElement adornedElement)
            : base(adornedElement) { }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (AdornedElement is not FrameworkElement c) return;
            drawingContext.DrawRectangle(null, pen, new(0, 0, c.Width, c.Height));
        }
    }
}
