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

#nullable enable

namespace WixUiDesigner.Logging
{
    sealed class Logger
    {
        readonly IServiceProvider serviceProvider;
        readonly JoinableTaskFactory joinableTaskFactory;

        IVsOutputWindow? window;
        IVsOutputWindowPane? pane;
        DebugContext debugContext;

        internal DebugContext DebugContext
        {
            get => debugContext;
            set
            {
                if (debugContext == value) return;
                var oldContext = debugContext;
                debugContext = value;
                _ = LogAsync(DebugContext.Package, $"Changed debug context from {oldContext} to {debugContext}.");
            }
        }

        internal Logger(IServiceProvider provider, JoinableTaskFactory joinableTaskFactory)
        {
            serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
        }

        public async Task LogAsync(DebugContext context, string message, CancellationToken cancellationToken = default)
        {
            if ((context & DebugContext) == 0) return;
            await LogAsync(message, false, cancellationToken);
        }
        public async Task LogErrorAsync(string message, CancellationToken cancellationToken = default) =>
            await LogAsync(message, true, cancellationToken);

        async Task LogAsync(string message, bool forceVisible, CancellationToken cancellationToken)
        {
            try
            {
                if (!(await PaneIsAccessibleAsync(cancellationToken))) return;
                await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                pane?.OutputString(DateTime.Now + ": " + message + Environment.NewLine);
                if (forceVisible) pane?.Activate();
            }
            catch
            {
                // can't help it
            }
        }

        [SuppressMessage("Reliability", "VSSDK006:Check services exist", Justification = "I do!")]
        async Task<bool> PaneIsAccessibleAsync(CancellationToken cancellationToken = default)
        {
            if (pane is not null) return true;

            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            window ??= (IVsOutputWindow)serviceProvider.GetService(typeof(SVsOutputWindow));
            if (window is null) return false;

            var guid = Guid.NewGuid();
            window.CreatePane(ref guid, Defines.ProductName, 1, 1);
            window.GetPane(ref guid, out pane);
            return pane != null;
        }
    }
}