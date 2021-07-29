/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

#nullable enable

namespace WixUiDesigner.Document
{
    static class WixParser
    {
        public static XmlNamespaceManager WixNamespaceManager { get; }
        static WixParser()
        {
            WixNamespaceManager = new(new NameTable());
            WixNamespaceManager.AddNamespace("wix", "http://schemas.microsoft.com/wix/2006/wi");
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
    }
}
