﻿/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.Threading;

#nullable enable

namespace WixUiDesigner.Document
{
    static class WixParser
    {
#pragma warning disable IDE0052 // Ungelesene private Member entfernen
        // ReSharper disable once NotAccessedField.Local
        static IServiceProvider? serviceProvider;
#pragma warning restore IDE0052 // Ungelesene private Member entfernen
        static JoinableTaskFactory? joinableTaskFactory;

        public static XmlNamespaceManager WixNamespaceManager { get; }
        static WixParser()
        {
            WixNamespaceManager = new(new NameTable());
            WixNamespaceManager.AddNamespace("wix", "http://schemas.microsoft.com/wix/2006/wi");
        }

        public static async Task InitializeAsync(IServiceProvider provider, JoinableTaskFactory jtf, CancellationToken cancellationToken)
        {
            serviceProvider = provider;
            joinableTaskFactory = jtf;

            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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

        public static string? EvaluateTextValue(this XElement? element) =>
            EvaluateString(element.GetTextValue());
        public static string? EvaluateAttribute(this XElement? element, string attributeName) =>
            EvaluateString(element?.Attribute(attributeName)?.Value);
        public static double EvaluateDoubleAttribute(this XElement? element, string attributeName, double defaultValue = default) =>
            double.TryParse(element.EvaluateAttribute(attributeName) ?? string.Empty,
                            NumberStyles.Any, CultureInfo.InstalledUICulture, out var d)
                ? d
                : defaultValue;
        public static string? GetTextValue(this XElement? element) => element?.Attribute("Text")?.Value ??
                                                                      element?.XPathSelectElements("wix:Text", WixNamespaceManager)
                                                                             .SingleOrDefault()
                                                                             ?.Value;
        public static string? EvaluateString(string? s) => s;

        public static bool IsEnabledControl(this XElement element) => !element.HasYesAttribute("Disabled");
        public static Visibility GetControlVisibility(this XElement element) =>
            element.HasYesAttribute("Hidden") ? Visibility.Hidden : Visibility.Visible;
        public static bool IsBitmap(this XElement element) => element.HasYesAttribute("Bitmap");
        public static bool IsIcon(this XElement element) => element.HasYesAttribute("Icon");
        public static bool IsImage(this XElement element) => element.HasYesAttribute("Image");
        public static bool IsMultiLine(this XElement element) => element.HasYesAttribute("Multiline");
        public static bool IsRightAligned(this XElement element) => element.HasYesAttribute("RightAligned");
        public static bool IsRightToLeft(this XElement element) => element.HasYesAttribute("RightToLeft");

        public static bool HasYesAttribute(this XElement element, string attributeName) =>
            element.Attribute(attributeName)?.Value.ToLowerInvariant() == "yes";

        public static (int line, int column) GetPosition(this IXmlLineInfo? element) =>
            element?.HasLineInfo() == true ? (element.LineNumber, element.LinePosition) : (-1, -1);
    }
}
