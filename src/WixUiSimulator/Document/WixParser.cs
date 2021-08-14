/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using WixUiSimulator.Logging;
using WixUiSimulator.Properties;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace WixUiSimulator.Document
{
    static class WixParser
    {
        static readonly ImageSource MissingImage = Resources.MissingImage.ToImageSource();
        
        public static Dictionary<string, string> InternalLocalization { get; }

        static DTE2? dte;

        public static XmlNamespaceManager WixNamespaceManager { get; }
        static WixParser()
        {
            WixNamespaceManager = new(new NameTable());
            WixNamespaceManager.AddNamespace("wix", "http://schemas.microsoft.com/wix/2006/wi");
            WixNamespaceManager.AddNamespace("wxl", "http://schemas.microsoft.com/wix/2006/localization");
            InternalLocalization = XDocument.Parse(Resources.WixUI_en_us).GetLocalizedStrings();
        }

        internal static async Task InitializeAsync(IServiceProvider serviceProvider, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte = (DTE2?)serviceProvider?.GetService(typeof(SDTE));
        }

        internal static ProjectItem? GetProjectItem(string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return dte?.Solution?.FindProjectItem(fileName);
        }

        public static XDocument Load(string xml)
        {
            using var textReader = new StringReader(xml);
            return XDocument.Load(textReader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        public static bool IsWixUiDocument(this XDocument xml) => xml.XPathSelectElements("/wix:Wix/wix:Fragment/wix:UI/wix:Dialog", WixNamespaceManager).Count() == 1;

        public static XElement GetDialogNode(this XDocument xml) =>
            xml.XPathSelectElements("/wix:Wix/wix:Fragment/wix:UI/wix:Dialog", WixNamespaceManager).Single();
        public static XElement? GetControlAt(this XDocument xml, int line, int column) =>
            (XElement?)xml.GetAllControls()
                          .OfType<IXmlLineInfo>()
                          .Where(lineInfo => lineInfo.HasLineInfo())
                          .TakeWhile(lineInfo => lineInfo.LineNumber <= line)
                          .GroupBy(lineInfo => lineInfo.LineNumber)
                          .LastOrDefault()?
                          .TakeWhile(lineInfo => lineInfo.LinePosition <= column)
                          .LastOrDefault();
        public static IEnumerable<XElement> GetAllControls(this XDocument xml) =>
            xml.XPathSelectElements(
                "/wix:Wix/wix:Fragment/wix:UI/wix:Dialog//wix:Control|/wix:Wix/wix:Fragment/wix:UI/wix:Dialog//wix:Control/wix:RadioButtonGroup|/wix:Wix/wix:Fragment/wix:UI/wix:Dialog//wix:Control/wix:RadioButtonGroup/wix:RadioButton",
                WixNamespaceManager);
        public static IEnumerable<XElement> GetControlNodes(this XElement parentNode) =>
            parentNode.XPathSelectElements(
                "wix:Control",
                WixNamespaceManager);

        public static string GetId(this XElement element) => element.Attribute("Id")?.Value ?? string.Empty;
        public static string? GetAttributeValue(this XElement? element, string attributeName) =>
            element?.Attribute(attributeName)?.Value;
        public static double GetAttributeDoubleValue(this XElement? element, string attributeName, double defaultValue = default) =>
            double.TryParse(element.GetAttributeValue(attributeName) ?? string.Empty,
                            NumberStyles.Any, CultureInfo.InstalledUICulture, out var d)
                ? d
                : defaultValue;
        public static string? GetTextValue(this XElement? element) => element?.Attribute("Text")?.Value ??
                                                                      element?.XPathSelectElements("wix:Text", WixNamespaceManager)
                                                                             .SingleOrDefault()
                                                                             ?.Value;
        public static Visibility GetControlVisibility(this XElement element) =>
            element.HasYesAttribute("Hidden") ? Visibility.Hidden : Visibility.Visible;
        public static bool IsBitmap(this XElement element) => element.HasYesAttribute("Bitmap");
        public static bool IsComboList(this XElement element) => element.HasYesAttribute("ComboList");
        public static bool IsIcon(this XElement element) => element.HasYesAttribute("Icon");
        public static bool IsImage(this XElement element) => element.HasYesAttribute("Image");
        public static bool IsMultiLine(this XElement element) => element.HasYesAttribute("Multiline");
        public static bool IsRightAligned(this XElement element) => element.HasYesAttribute("RightAligned");
        public static bool IsRightToLeft(this XElement element) => element.HasYesAttribute("RightToLeft");
        public static bool IsPushLike(this XElement element) => element.HasYesAttribute("PushLike");
        public static bool IsSorted(this XElement element) => element.HasYesAttribute("Sorted");

        public static string[] GetComboBoxItems(this XElement element)
        {
            var keys = element.XPathSelectElements("wix:ComboBox/wix:ListItem", WixNamespaceManager).Select(item => (item.Attribute("Text") ?? item.Attribute("Value"))?.Value ?? string.Empty);
            if (element.IsSorted()) keys = keys.OrderBy(k => k, StringComparer.InvariantCultureIgnoreCase);
            return keys.ToArray();
        }

        public static ImageSource GetImageSource(this XElement element) => GetImageSource(element.GetTextValue());
        public static ImageSource GetImageSource(string? source)
        {
            Logger.Log(DebugContext.WiX, $"Trying to locate image {source}.");
            return MissingImage;
        }

        public static bool HasYesAttribute(this XElement element, string attributeName) =>
            element.Attribute(attributeName)?.Value.ToLowerInvariant() == "yes";

        public static (int line, int column) GetPosition(this IXmlLineInfo? element) =>
            element?.HasLineInfo() == true ? (element.LineNumber, element.LinePosition) : (-1, -1);

        static ImageSource ToImageSource(this Bitmap bitmap) => Imaging.CreateBitmapSourceFromHBitmap(
            bitmap.GetHbitmap(),
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        public static Dictionary<string, string> GetLocalizedStrings(this XDocument document) => document
            .XPathSelectElements("/wxl:WixLocalization/wxl:String", WixNamespaceManager)
            .Select(node => (key: node.Attribute("Id")?.Value, value: node.Value))
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.key))
            .GroupBy(kvp => kvp.key)
            .ToDictionary(kvp => kvp.Key!, kvp => kvp.Last().value);
    }
}
