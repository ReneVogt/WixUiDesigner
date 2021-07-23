/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */
using System;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace Com.Revo.WixUiDesigner
{
    [Guid(PackageGuids.PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "0.1.0")]
    //[ProvideOptionPage(typeof(Options), "Markdown Editor", "Markdown Editor Options", 0, 0, true)]
    //[ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideLanguageService(typeof(WixUiLanguage), WixUiLanguage.LanguageName, 100, ShowDropDownOptions = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true, ShowSmartIndent = true)]
    //[ProvideLanguageEditorOptionPage(typeof(Options), MarkdownLanguage.LanguageName, null, "Advanced", "#101", new[] { "markdown", "md" })]
    [ProvideLanguageExtension(typeof(WixUiLanguage), ".wxs")]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideEditorExtension(typeof(EditorFactory), ".wxs", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".*", 2, NameResourceID = 110)]

    public sealed class WixUiDesignerPackage : AsyncPackage
    {
        Logger? logger;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var editorFactory = new EditorFactory();
            RegisterEditorFactory(editorFactory);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

//            _options = (Options)GetDialogPage(typeof(Options));

            logger = new (this, JoinableTaskFactory);
            await logger.LogAsync("Yippieyayeah", true, cancellationToken);
        }
    }
}
