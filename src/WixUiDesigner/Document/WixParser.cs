/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

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

        public static bool IsWixUiDocument(this XDocument xml) => xml.XPathSelectElements("/wix:Wix/wix:Fragment/wix:UI/wix:Dialog", WixNamespaceManager).Count() == 1;
        public static XElement GetDialogNode(this XDocument xml) =>
            xml.XPathSelectElements("/wix:Wix/wix:Fragment/wix:UI/wix:Dialog", WixNamespaceManager).Single();
    }
}
