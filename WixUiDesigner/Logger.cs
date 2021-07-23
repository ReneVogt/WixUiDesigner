/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace Com.Revo.WixUiDesigner
{
    sealed class Logger
    {
        readonly IServiceProvider serviceProvider;
        readonly JoinableTaskFactory jtf;

        IVsOutputWindowPane? outputPane;
        IVsOutputWindow? outputWindow;

        internal Logger(IServiceProvider provider, JoinableTaskFactory jtf)
        {
            serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.jtf = jtf ?? throw new ArgumentNullException(nameof(jtf));
        }

        internal async Task LogAsync(string message, bool forceVisible = false, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!(await PaneExistsAsync(cancellationToken))) return;
                await jtf.SwitchToMainThreadAsync(cancellationToken);
                outputPane!.OutputString(DateTime.Now + ": " + message + Environment.NewLine);
                if (forceVisible)
                    outputPane.Activate();
            }
            catch
            {
                // can't help it
            }
        }

        [SuppressMessage("Reliability", "VSSDK006:Check services exist", Justification = "I do!")]
        async Task<bool> PaneExistsAsync(CancellationToken cancellationToken = default)
        {
            if (outputPane is not null) return true;
            await jtf.SwitchToMainThreadAsync(cancellationToken);
            outputWindow ??= (IVsOutputWindow)serviceProvider.GetService(typeof(SVsOutputWindow));
            if (outputWindow is null) return false;

            var guid = Guid.NewGuid();
            outputWindow.CreatePane(ref guid, nameof(WixUiDesigner), 1, 1);
            outputWindow.GetPane(ref guid, out outputPane);

            return outputPane is not null;
        }
    }
}