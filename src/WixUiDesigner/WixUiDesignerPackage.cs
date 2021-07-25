/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */
#nullable enable

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace WixUiDesigner
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(Options), "WiX UI Designer", "WiX UI Designer Options", 0, 0, true)]
    public sealed class WixUiDesignerPackage : AsyncPackage
    {
        public const string PackageGuidString = "13c4f662-6ebc-4dbd-9e57-165c8f7dbcbf";

        readonly object sync = new object();

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

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Options = (Options)GetDialogPage(typeof(Options));
        }

        void OnOptionChanged(object sender, PropertyChangedEventArgs e) => OnOptionsChanged(e.PropertyName);
        void OnOptionsChanged(string? optionName = null)
        {
            if (optionName is not null && optionName != nameof(Options.DebugContext)) return;
            Debug.WriteLine($"DebugContext: {Options?.DebugContext.ToString() ?? "null"}");
        }
    }
}
