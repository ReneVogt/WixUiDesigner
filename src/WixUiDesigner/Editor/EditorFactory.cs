/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using WixUiDesigner.Logging;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

#nullable enable

namespace WixUiDesigner.Editor
{
    /// <summary>
    /// Factory for creating our editors.
    /// </summary>
    [Guid(Defines.EditorGuidString)]
    public sealed class EditorFactory : IVsEditorFactory, IDisposable
    {
        readonly Logger logger;
        ServiceProvider? vsServiceProvider;

        internal EditorFactory(Logger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            vsServiceProvider?.Dispose();
        }
        public int SetSite(IOleServiceProvider psp)
        {
            vsServiceProvider = new (psp);
            return VSConstants.S_OK;
        }
        public int MapLogicalView(ref Guid logicalViewGuid, out string? physicalView)
        {
            physicalView = null;
            return VSConstants.LOGVIEWID_Primary == logicalViewGuid ? VSConstants.S_OK : VSConstants.E_NOTIMPL;
        }
        public int Close() => VSConstants.S_OK;

        /// <summary>
        /// Used by the editor factory to create an editor instance. the environment first determines the 
        /// editor factory with the highest priority for opening the file and then calls 
        /// IVsEditorFactory.CreateEditorInstance. If the environment is unable to instantiate the document data 
        /// in that editor, it will find the editor with the next highest priority and attempt to so that same 
        /// thing. 
        /// NOTE: The priority of our editor is 32 as mentioned in the attributes on the package class.
        /// 
        /// Since our editor supports opening only a single view for an instance of the document data, if we 
        /// are requested to open document data that is already instantiated in another editor, or even our 
        /// editor, we return a value VS_E_INCOMPATIBLEDOCDATA.
        /// </summary>
        /// <param name="grfCreateDoc">Flags determining when to create the editor. Only open and silent flags 
        /// are valid.
        /// </param>
        /// <param name="pszMkDocument">path to the file to be opened.</param>
        /// <param name="pszPhysicalView">name of the physical view.</param>
        /// <param name="pvHier">pointer to the IVsHierarchy interface.</param>
        /// <param name="itemid">Item identifier of this editor instance.</param>
        /// <param name="punkDocDataExisting">This parameter is used to determine if a document buffer 
        /// (DocData object) has already been created.
        /// </param>
        /// <param name="ppunkDocView">Pointer to the IUnknown interface for the DocView object.</param>
        /// <param name="ppunkDocData">Pointer to the IUnknown interface for the DocData object.</param>
        /// <param name="pbstrEditorCaption">Caption mentioned by the editor for the doc window.</param>
        /// <param name="pguidCmdUI">the Command UI Guid. Any UI element that is visible in the editor has 
        /// to use this GUID.
        /// </param>
        /// <param name="pgrfCDW">Flags for CreateDocumentWindow.</param>
        /// <returns>HRESULT result code. S_OK if the method succeeds.</returns>
        /// <remarks>
        /// Attribute usage according to FxCop rule regarding SecurityAction requirements (LinkDemand).
        /// This method do use SecurityAction.Demand action instead of LinkDemand because it overrides method without LinkDemand
        /// see "Demand vs. LinkDemand" article in MSDN for more details.
        /// </remarks>
        [EnvironmentPermission(SecurityAction.Demand, Unrestricted = true)]
        public int CreateEditorInstance(
                        uint grfCreateDoc,
                        string pszMkDocument,
                        string pszPhysicalView,
                        IVsHierarchy pvHier,
                        uint itemid,
                        IntPtr punkDocDataExisting,
                        out IntPtr ppunkDocView,
                        out IntPtr ppunkDocData,
                        out string? pbstrEditorCaption,
                        out Guid pguidCmdUI,
                        out int pgrfCDW)
        {
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = Defines.EditorGuid;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
                return VSConstants.E_INVALIDARG;

            if (punkDocDataExisting != IntPtr.Zero)
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;

            // Create the Document (editor)
            EditorPane newEditor = new EditorPane(logger);
            ppunkDocView = Marshal.GetIUnknownForObject(newEditor);
            ppunkDocData = Marshal.GetIUnknownForObject(newEditor);
            pbstrEditorCaption = "testcaption";

            return VSConstants.S_OK;
        }

        public object? GetService(Type serviceType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return vsServiceProvider?.GetService(serviceType);
        }
    }
}
