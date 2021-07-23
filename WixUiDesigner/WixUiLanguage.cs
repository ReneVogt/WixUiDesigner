/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

#nullable enable

namespace Com.Revo.WixUiDesigner
{
    public sealed class WixUiLanguage : LanguageService
    {
        public const string LanguageName = "WiX UI fragment";

        /// <inheritdoc />
        public override LanguagePreferences GetLanguagePreferences() => throw new System.NotImplementedException();
        /// <inheritdoc />
        public override IScanner GetScanner(IVsTextLines buffer) => throw new System.NotImplementedException();
        /// <inheritdoc />
        public override AuthoringScope ParseSource(ParseRequest req) => throw new System.NotImplementedException();
        /// <inheritdoc />
        public override string GetFormatFilterList() => throw new System.NotImplementedException();
        /// <inheritdoc />
        public override string Name => LanguageName;
    }
}
