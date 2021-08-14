/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.Windows.Controls;
using FontFamily = System.Windows.Media.FontFamily;
using System.Windows.Media;

#nullable enable

namespace WixUiSimulator.Document
{
    record WixFont(string Face, double Size, bool Bold = false, bool Italic = false, bool Strike = false, bool Underline = false, byte Red = 0, byte Green = 0, byte Blue = 0)
    {
        public static WixFont DefaultWixFont { get; }
        static WixFont()
        {
            string fontFaceName = ((FontFamily)Control.FontFamilyProperty.DefaultMetadata.DefaultValue).Source;
            double fontSize = (double)Control.FontSizeProperty.DefaultMetadata.DefaultValue;

            DefaultWixFont = new(fontFaceName, fontSize);
        }

        public void SetToFrameworkElement(Control element)
        {
            element.Foreground = new SolidColorBrush(Color.FromRgb(Red, Green, Blue));
            element.FontFamily = new(Face);
            element.FontSize = Size;
}
    }
}
