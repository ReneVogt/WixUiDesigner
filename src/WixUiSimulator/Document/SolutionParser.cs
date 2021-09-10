/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Constants = EnvDTE.Constants;
using Task = System.Threading.Tasks.Task;
#nullable enable

namespace WixUiSimulator.Document
{
    static class SolutionParser
    {
        const int E_MEMBERNOTFOUND = -2147352573; // 0x80020003

        static readonly Guid ProjectItemKindPhysicalFolder = new(Constants.vsProjectItemKindPhysicalFolder);
        static readonly Guid ProjectItemKindVirtualFolder = new(Constants.vsProjectItemKindVirtualFolder);
        static readonly Guid ProjectItemKindPhysicalFile = new(Constants.vsProjectItemKindPhysicalFile);

        static readonly Regex AnalysableFilePattern = new(@"\.wxs$|\.wxl$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        static DTE2? dte;

        internal static async Task InitializeAsync(IServiceProvider serviceProvider, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte = (DTE2?)serviceProvider?.GetService(typeof(SDTE));
        }

        internal static ProjectItem? GetProjectItem(string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return dte?.Solution?.FindProjectItem(fileName);
        }
        internal static IEnumerable<string> FindAnalysableFiles(this Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return project.ProjectItems.FindAnalysableFiles();
        }

        [SuppressMessage("Usage", "VSTHRD010", Justification = "Lambdas are executed on same thread.")]
        static IEnumerable<string> FindAnalysableFiles(this ProjectItems projectItems)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var items = projectItems.OfType<ProjectItem>().Select(item => (item, guid: item.GetKind())).ToArray();
            var files = items.Where(item => item.guid == ProjectItemKindPhysicalFile)
                             .Select(item => item.item.FileNames[0])
                             .Where(file => AnalysableFilePattern.IsMatch(file));
            var subfiles = items.Where(item => item.guid == ProjectItemKindPhysicalFolder || item.guid == ProjectItemKindVirtualFolder)
                                .SelectMany(item => item.item.ProjectItems.FindAnalysableFiles());
            return files.Concat(subfiles);
        }

        internal static Guid GetKind(this ProjectItem projectItem)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return Guid.TryParse(projectItem.Kind, out var guid) ? guid : Guid.Empty;
            }
            catch (COMException exception) when (exception.HResult == E_MEMBERNOTFOUND)
            {
                return Guid.Empty;
            }
        }
    }
}
