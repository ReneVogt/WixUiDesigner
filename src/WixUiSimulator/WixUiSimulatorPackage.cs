/*
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
using WixUiSimulator.Document;
using WixUiSimulator.Logging;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace WixUiSimulator
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(Defines.PackageGuidString)]
    [InstalledProductRegistration("#110", "#112", "0.2.0.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Options), Defines.ProductName, "General", 0, 0, true)]
    public sealed class WixUiSimulatorPackage : AsyncPackage
    {
        static readonly object sync = new();

        static Options? options;

        public static Options? Options
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                lock (sync)
                {
                    if (options is null)
                        LoadPackage();
                    
                    return options;
                }

            }
            private set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
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

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Options = (Options)GetDialogPage(typeof(Options));

            Logger.Initialize(this, JoinableTaskFactory);

            try
            {
                await WixParser.InitializeAsync(JoinableTaskFactory, cancellationToken);
                await Logger.LogAsync(DebugContext.Package, "Package initialized.", cancellationToken);
            }
            catch (Exception ex)
            {
                await Logger.LogErrorAsync($"Failed to initialize package: {ex}", cancellationToken);
            }
        }
        static void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OnOptionsChanged();
        }
        static void OnOptionsChanged()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Logger.Log(DebugContext.Package, $"Changed options:{Environment.NewLine}{Options}");
        }

        static void LoadPackage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var shell = (IVsShell)GetGlobalService(typeof(SVsShell));
            var guid = Defines.PackageGuid;
            if (shell.IsPackageLoaded(ref guid, out _) != VSConstants.S_OK)
                ErrorHandler.Succeeded(shell.LoadPackage(ref guid, out _));
        }

    }
}
