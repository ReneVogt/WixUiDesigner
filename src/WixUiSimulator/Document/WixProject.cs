/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using WixUiSimulator.Logging;

#nullable enable

namespace WixUiSimulator.Document
{
    sealed class WixProject
    {
        static readonly Dictionary<Project, WixProject> projects = new ();

        public Project Project { get; }

        //public event EventHandler? ResourcesChanged;

        WixProject(Project project) => Project = project;

        public string GetTextForControl(XElement node, out WixFont font) => ParseFormatted(node.GetTextValue() ?? string.Empty, out font);
        public string ParseFormatted(string formatted, out WixFont font)
        {
            font = WixFont.DefaultWixFont;
            return formatted;
        }

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
