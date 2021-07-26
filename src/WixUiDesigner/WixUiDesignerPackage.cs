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
using WixUiDesigner.Editor;
using WixUiDesigner.Logging;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace WixUiDesigner
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(Defines.PackageGuidString)]
    [InstalledProductRegistration("#110", "#112", "0.1.0.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(Options), Defines.ProductName, Defines.ProductName + " Options", 0, 0, true)]

    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.Designer_string)]
    [ProvideEditorExtension(typeof(EditorFactory), ".wxs", 32, NameResourceID = 110)]
    public sealed class WixUiDesignerPackage : AsyncPackage
    {
        readonly object sync = new object();
        readonly Logger logger;

        Options? options;
        EditorFactory? editorFactory;

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
            logger = new (this, JoinableTaskFactory);
        }
        protected override void Dispose(bool disposing)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (disposing)
                    editorFactory?.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Options = (Options)GetDialogPage(typeof(Options));

            try
            {
                editorFactory = new(logger);
                RegisterEditorFactory(editorFactory);
                await logger.LogAsync(DebugContext.Package, "Package initialized.", cancellationToken);
            }
            catch (Exception ex)
            {
                await logger.LogErrorAsync($"ERROR while initializing package: {ex}", cancellationToken);
            }
        }
        void OnOptionChanged(object sender, PropertyChangedEventArgs e) => OnOptionsChanged(e.PropertyName);
        void OnOptionsChanged(string? optionName = null)
        {
            if (optionName is not null && optionName != nameof(Options.DebugContext)) return;
            logger.DebugContext = Options?.DebugContext ?? DebugContext.None;
        }
    }
}
