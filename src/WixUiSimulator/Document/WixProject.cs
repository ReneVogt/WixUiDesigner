/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using WixUiSimulator.Logging;

#nullable enable

namespace WixUiSimulator.Document
{
    sealed class WixProject
    {
        static readonly Dictionary<Project, WixProject> projects = new ();

        internal Project Project { get; }

        //internal event EventHandler? ResourcesChanged;

        internal WixProject(Project project) => Project = project;

        internal static WixProject? Get(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var project = projectItem.ContainingProject;
            if (project is null) return null;
            if (!Guid.TryParse(project.Kind, out var guid) || guid != Defines.WixProjectKindGuid)
            {
                Logger.Log(DebugContext.Document, $"{projectItem.Document.FullName} is part of project {project.Name} which is not a WiX project (kind: {project.Kind}).");
                return null;
            }
            if (project.CodeModel is {CodeElements: var elements})
            {
                Logger.Log(DebugContext.Document, $"Code elements: {string.Join(", ", elements)}");
            }
            else
            {
                Logger.Log(DebugContext.Document, "NO CODE MODEL!");
            }
            
            if (projects.TryGetValue(project, out var wixProject)) return wixProject;
            wixProject = new(project);
            projects[project] = wixProject;
            return wixProject;
        }
    }
}
