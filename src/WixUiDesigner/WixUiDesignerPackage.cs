/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */
#nullable enable

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using WixUiDesigner.Logging;
using Task = System.Threading.Tasks.Task;

namespace WixUiDesigner
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(Defines.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(Options), Defines.ProductName, Defines.ProductName + " Options", 0, 0, true)]
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
