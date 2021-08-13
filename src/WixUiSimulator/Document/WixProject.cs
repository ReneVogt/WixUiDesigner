/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Generic;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using WixUiSimulator.Logging;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace WixUiSimulator.Document
{
    sealed class WixProject
    {
        static readonly Dictionary<Project, WixProject> projects = new Dictionary<Project, WixProject>();
        static DTE2? dte;

        internal Project Project { get; }

        internal WixProject(Project project) => Project = project;

        internal static async Task InitializeAsync(IServiceProvider serviceProvider, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte = (DTE2?)serviceProvider?.GetService(typeof(SDTE));
        }
        internal static WixProject? Get(string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var project = dte?.Solution?.FindProjectItem(fileName)?.ContainingProject;
            if (project is null) return null;
            if (!Guid.TryParse(project.Kind, out var guid) || guid != Defines.WixProjectKindGuid)
            {
                Logger.Log(DebugContext.Document, $"{fileName} is part of project {project.Name} which is not a WiX project (kind: {project.Kind}).");
                return null;
            }
            if (projects.TryGetValue(project, out var wixProject)) return wixProject;
            wixProject = new(project);
            projects[project] = wixProject;
            return wixProject;
        }
    }
}
