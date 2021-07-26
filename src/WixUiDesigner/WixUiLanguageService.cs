/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

#nullable enable

namespace WixUiDesigner
{
    [Guid("8FAA0CCA-8FBE-466E-AB7B-413DB90BBC61")]
    public sealed class WixUiLanguageService : LanguageService
    {
        public const string LanguageName = "WiX UI";
        LanguagePreferences? preferences;

        /// <inheritdoc />
        public override LanguagePreferences GetLanguagePreferences()
        {
            if (preferences is not null) return preferences;

            preferences = new(Site, typeof(WixUiLanguageService).GUID, Name);
            preferences.Init();

            preferences.EnableCodeSense = true;
            preferences.EnableMatchBraces = true;
            preferences.EnableMatchBracesAtCaret = true;
            preferences.EnableShowMatchingBrace = true;
            preferences.EnableCommenting = true;
            preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
            preferences.LineNumbers = false;
            preferences.MaxErrorMessages = 100;
            preferences.AutoOutlining = false;
            preferences.MaxRegionTime = 2000;
            preferences.InsertTabs = false;
            preferences.IndentSize = 2;
            preferences.IndentStyle = IndentingStyle.Smart;

            preferences.WordWrap = true;
            preferences.WordWrapGlyphs = true;

            preferences.AutoListMembers = true;
            preferences.EnableQuickInfo = true;
            preferences.ParameterInformation = true;

            return preferences;
        }
        public override IScanner? GetScanner(IVsTextLines buffer) => null;
        public override AuthoringScope ParseSource(ParseRequest req) => throw new NotImplementedException();
        public override string GetFormatFilterList() => "WiX UI fragment (*.wxs)|*.wxs";
        public override string Name => LanguageName;
        public override void Dispose()
        {
            try
            {
                preferences?.Dispose();
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
