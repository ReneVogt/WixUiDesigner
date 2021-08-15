/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.Windows;
using System.Windows.Controls;
using FontFamily = System.Windows.Media.FontFamily;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using SystemColors = System.Drawing.SystemColors;

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
            var color = SystemColors.ControlText;

            DefaultWixFont = new(fontFaceName, fontSize, Red: color.R, Green: color.G, Blue: color.B);
        }

        public void ApplyToControl(Control element)
        {
            element.Foreground = new SolidColorBrush(Color.FromRgb(Red, Green, Blue));
            element.FontFamily = new(Face);
            element.FontSize = Size;
            element.FontWeight = Bold ? FontWeights.Bold : (FontWeight)Control.FontWeightProperty.DefaultMetadata.DefaultValue;
            element.FontStyle = Italic ? FontStyles.Italic : FontStyles.Normal;

            if (element is not TextBox textBox) return;
            textBox.TextDecorations.Clear();
            if (Strike)
                textBox.TextDecorations.Add(TextDecorations.Strikethrough);
            if (Underline)
                textBox.TextDecorations.Add(TextDecorations.Underline);
        }
        public void ApplyToTextBlock(TextBlock textBlock)
        {
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(Red, Green, Blue));
            textBlock.FontFamily = new(Face);
            textBlock.FontSize = Size;
            textBlock.FontWeight = Bold ? FontWeights.Bold : (FontWeight)Control.FontWeightProperty.DefaultMetadata.DefaultValue;
            textBlock.FontStyle = Italic ? FontStyles.Italic : FontStyles.Normal;
            textBlock.TextDecorations.Clear();
            if (Strike)
                textBlock.TextDecorations.Add(TextDecorations.Strikethrough);
            if (Underline)
                textBlock.TextDecorations.Add(TextDecorations.Underline);
        }
    }
}
