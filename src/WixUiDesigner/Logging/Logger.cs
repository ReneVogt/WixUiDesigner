/*
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
    static class Logger
    {
        static IServiceProvider? serviceProvider;
        static JoinableTaskFactory? joinableTaskFactory;
        static IVsOutputWindow? window;
        static IVsOutputWindowPane? pane;

        internal static void Initialize(IServiceProvider provider, JoinableTaskFactory jtf)
        {
            serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            joinableTaskFactory = jtf ?? throw new ArgumentNullException(nameof(jtf));
        }
        internal static void Log(DebugContext context, string message) => _ = LogAsync(context, message);
        internal static void LogError(string message) => _ = LogErrorAsync(message);

        internal static async Task LogAsync(DebugContext context, string message, CancellationToken cancellationToken = default)
        {
            if (joinableTaskFactory is null) return;
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            if ((context & WixUiDesignerPackage.Options?.DebugContext) == 0) return;
            await LogAsync(context.ToString(), message, false, cancellationToken);
        }
        internal static async Task LogErrorAsync(string message, CancellationToken cancellationToken = default) =>
            await LogAsync("ERROR", message, true, cancellationToken);

        static async Task LogAsync(string header, string message, bool forceVisible, CancellationToken cancellationToken)
        {
            try
            {
                if (!await PaneIsAccessibleAsync(cancellationToken)) return;
                await joinableTaskFactory!.SwitchToMainThreadAsync(cancellationToken);
                pane?.OutputString($"{DateTime.Now} [{header}]: {message}{Environment.NewLine}");
                if (forceVisible) pane?.Activate();
            }
            catch
            {
                // can't help it
            }
        }

        [SuppressMessage("Reliability", "VSSDK006:Check services exist", Justification = "I do!")]
        static async Task<bool> PaneIsAccessibleAsync(CancellationToken cancellationToken = default)
        {
            if (pane is not null) return true;
            if (joinableTaskFactory is null || serviceProvider is null) return false;
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