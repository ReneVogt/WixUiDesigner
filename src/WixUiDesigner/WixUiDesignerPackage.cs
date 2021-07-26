/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using WixUiDesigner.Logging;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace WixUiDesigner
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(Defines.PackageGuidString)]
    [InstalledProductRegistration("#110", "#112", "0.1.0.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(Options), Defines.ProductName, Defines.ProductName + " Options", 0, 0, true)]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideLanguageService(typeof(WixUiLanguageService), WixUiLanguageService.LanguageName, 100, ShowDropDownOptions = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true, ShowSmartIndent = true)]
    [ProvideLanguageExtension(typeof(WixUiLanguageService), ".wxs")]

    [ProvideEditorExtension(typeof(EditorFactory), ".wxs", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".*", 2, NameResourceID = 110)]

    public sealed class WixUiDesignerPackage : AsyncPackage
    {
        readonly object sync = new object();
        readonly Logger logger;
        Options? options;

        public Options? Options
        {
            get => options;
            private set
            {
                lock (sync)
                {
                    if (ReferenceEquals(options, value)) return;
                    if (options is { }) options.PropertyChanged -= OnOptionChanged;
                    options = value;
                    if (options is { }) options.PropertyChanged += OnOptionChanged;
                    OnOptionsChanged();
                }
            }
        }

        public WixUiDesignerPackage()
        {
            logger = new Logger(this, JoinableTaskFactory);
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            RegisterEditorFactory(new EditorFactory(this));

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Options = (Options)GetDialogPage(typeof(Options));
            await logger.LogAsync(DebugContext.Package, "Package initialized.", cancellationToken);
        }

        void OnOptionChanged(object sender, PropertyChangedEventArgs e) => OnOptionsChanged(e.PropertyName);
        void OnOptionsChanged(string? optionName = null)
        {
            if (optionName is not null && optionName != nameof(Options.DebugContext)) return;
            logger.DebugContext = Options?.DebugContext ?? DebugContext.None;
        }
    }
}
