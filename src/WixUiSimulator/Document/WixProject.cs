/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using WixUiSimulator.Logging;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace WixUiSimulator.Document
{
    sealed class WixProject : IDisposable
    {
        static readonly Dictionary<Project, WixProject> projects = new ();

        public static JoinableTaskFactory? JoinableTaskFactory { get; set; }

        readonly BlockingCollection<string> filesToAnalyze = new();
        readonly CancellationTokenSource cancellationTokenSource = new();

        readonly string projectName;

        public Project Project { get; }

        //public event EventHandler? ResourcesChanged;

        WixProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Project = project;
            projectName = Project.Name;
            var sb = new StringBuilder();
            sb.AppendLine($"Analyzing project {Project.Name}:");
            foreach (var file in project.FindAnalysableFiles())
            {
                sb.AppendLine(file);
                filesToAnalyze.Add(file);
            }
            Logger.Log(DebugContext.WiX | DebugContext.Document, sb.ToString());

            _ = Task.Run(async () => await AnalyzeAsync(cancellationTokenSource.Token));
        }
        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            filesToAnalyze.Dispose();
        }

        public string GetTextForControl(XElement node, out WixFont font) => ParseFormatted(node.GetTextValue() ?? string.Empty, out font);
        public string ParseFormatted(string formatted, out WixFont font)
        {
            font = WixFont.DefaultWixFont;
            return formatted;
        }

        async Task AnalyzeAsync(CancellationToken cancellationToken)
        {
            while (!filesToAnalyze.IsCompleted)
            {
                try
                {
                    string file = filesToAnalyze.Take(cancellationToken);
                    await JoinableTaskFactory!.RunAsync(
                        async () => await Logger.LogAsync(DebugContext.WiX, $"Analyzer of {projectName} analyzing {file}.", cancellationToken));

                }
                catch (InvalidOperationException ioe) when (ioe.TargetSite ==
                                                            filesToAnalyze.GetType()
                                                                         .GetGenericTypeDefinition()
                                                                         .GetMethod(nameof(filesToAnalyze.Take), new[] { typeof(CancellationToken) }))
                {
                    // completed
                }
                catch (OperationCanceledException)
                {
                    // cancelled
                }
                catch (Exception e)
                {
                    await Logger.LogErrorAsync($"Analyzer thread of {projectName} encountered an error: {e}", cancellationToken);
                }
            }
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
            
            if (projects.TryGetValue(project, out var wixProject)) return wixProject;
            wixProject = new(project);
            projects[project] = wixProject;
            return wixProject;
        }
    }
}
