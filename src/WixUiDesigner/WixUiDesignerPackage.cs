﻿/*
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
    [InstalledProductRegistration("#110", "#112", "0.1.0.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]

//    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(Options), Defines.ProductName, Defines.ProductName + " Options", 0, 0, true)]
    public sealed class WixUiDesignerPackage : AsyncPackage
    {
        static readonly object sync = new object();

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
            OnOptionsChanged(e.PropertyName);
        }
        static void OnOptionsChanged(string? optionName = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (optionName is not null && optionName != nameof(Options.DebugContext)) return;
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
