/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

#pragma warning disable 618

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;
using IComponentModel = Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
using IConnectionPoint = Microsoft.VisualStudio.OLE.Interop.IConnectionPoint;
using IConnectionPointContainer = Microsoft.VisualStudio.OLE.Interop.IConnectionPointContainer;
using IObjectWithSite = Microsoft.VisualStudio.OLE.Interop.IObjectWithSite;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IVsCodeWindow = Microsoft.VisualStudio.TextManager.Interop.IVsCodeWindow;
using IVsEditorAdaptersFactoryService = Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService;
using IVsEditorFactory = Microsoft.VisualStudio.Shell.Interop.IVsEditorFactory;
using IVsHierarchy = Microsoft.VisualStudio.Shell.Interop.IVsHierarchy;
using IVsTextBufferDataEvents = Microsoft.VisualStudio.TextManager.Interop.IVsTextBufferDataEvents;
using IVsTextBufferProvider = Microsoft.VisualStudio.Shell.Interop.IVsTextBufferProvider;
using IVsTextLines = Microsoft.VisualStudio.TextManager.Interop.IVsTextLines;
//using IVsUserData = Microsoft.VisualStudio.TextManager.Interop.IVsUserData;
using Marshal = System.Runtime.InteropServices.Marshal;
using Package = Microsoft.VisualStudio.Shell.Package;
using READONLYSTATUS = Microsoft.VisualStudio.TextManager.Interop.READONLYSTATUS;
using SComponentModel = Microsoft.VisualStudio.ComponentModelHost.SComponentModel;
using ServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;
//using VsCodeWindowClass = Microsoft.VisualStudio.TextManager.Interop.VsCodeWindowClass;
using VSConstants = Microsoft.VisualStudio.VSConstants;
using VsServiceProviderWrapper = Microsoft.VisualStudio.Shell.VsServiceProviderWrapper;
using VsTextBufferClass = Microsoft.VisualStudio.TextManager.Interop.VsTextBufferClass;
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable NotAccessedField.Local

#nullable enable

namespace WixUiDesigner
{
    [Guid(Defines.EditorGuidString)]
    public sealed class EditorFactory : IVsEditorFactory
    {
        readonly Package package;
        ServiceProvider? serviceProvider;

        public EditorFactory(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public int SetSite(IOleServiceProvider psp)
        {
            serviceProvider = new ServiceProvider(psp);
            return VSConstants.S_OK;
        }
        public object? GetService(Type serviceType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return serviceProvider?.GetService(serviceType);
        }

        // This method is called by the Environment (inside IVsUIShellOpenDocument::
        // OpenStandardEditor and OpenSpecificEditor) to map a LOGICAL view to a
        // PHYSICAL view. A LOGICAL view identifies the purpose of the view that is
        // desired (e.g. a view appropriate for Debugging [LOGVIEWID_Debugging], or a
        // view appropriate for text view manipulation as by navigating to a find
        // result [LOGVIEWID_TextView]). A PHYSICAL view identifies an actual type
        // of view implementation that an IVsEditorFactory can create.
        //
        // NOTE: Physical views are identified by a string of your choice with the
        // one constraint that the default/primary physical view for an editor
        // *MUST* use a NULL string as its physical view name (*pbstrPhysicalView = NULL).
        //
        // NOTE: It is essential that the implementation of MapLogicalView properly
        // validates that the LogicalView desired is actually supported by the editor.
        // If an unsupported LogicalView is requested then E_NOTIMPL must be returned.
        //
        // NOTE: The special Logical Views supported by an Editor Factory must also
        // be registered in the local registry hive. LOGVIEWID_Primary is implicitly
        // supported by all editor types and does not need to be registered.
        // For example, an editor that supports a ViewCode/ViewDesigner scenario
        // might register something like the following:
        //        HKLM\Software\Microsoft\VisualStudio\9.0\Editors\
        //            {...guidEditor...}\
        //                LogicalViews\
        //                    {...LOGVIEWID_TextView...} = s ''
        //                    {...LOGVIEWID_Code...} = s ''
        //                    {...LOGVIEWID_Debugging...} = s ''
        //                    {...LOGVIEWID_Designer...} = s 'Form'
        //
        public int MapLogicalView(ref Guid logicalView, out string? physicalView)
        {
            // initialize out parameter
            physicalView = null;

            // Determine the physical view
            if (VSConstants.LOGVIEWID_Primary == logicalView ||
                VSConstants.LOGVIEWID_Debugging == logicalView ||
                VSConstants.LOGVIEWID_Code == logicalView ||
                VSConstants.LOGVIEWID_TextView == logicalView)
                return VSConstants.S_OK;

            if (VSConstants.LOGVIEWID_Designer == logicalView)
            {
                physicalView = "Design";
                return VSConstants.S_OK;
            }

            return VSConstants.E_NOTIMPL;
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }

        public int CreateEditorInstance(
                        uint createEditorFlags,
                        string documentMoniker,
                        string physicalView,
                        IVsHierarchy hierarchy,
                        uint itemid,
                        IntPtr docDataExisting,
                        out IntPtr docView,
                        out IntPtr docData,
                        out string? editorCaption,
                        out Guid commandUIGuid,
                        out int createDocumentWindowFlags)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Initialize output parameters
            docView = IntPtr.Zero;
            docData = IntPtr.Zero;
            commandUIGuid = Guid.Empty;
            createDocumentWindowFlags = 0;
            editorCaption = null;

            // Validate inputs
            if ((createEditorFlags & (uint)(VSConstants.CEF.OpenFile | VSConstants.CEF.Silent)) == 0)
                return VSConstants.E_INVALIDARG;

            // Get a text buffer
            IVsTextLines? textLines = GetTextBuffer(docDataExisting, documentMoniker);

            // Assign docData IntPtr to either existing docData or the new text buffer
            if (docDataExisting != IntPtr.Zero)
            {
                docData = docDataExisting;
                Marshal.AddRef(docData);
            }
            else
                docData = Marshal.GetIUnknownForObject(textLines);

            try
            {
                object docViewObject = CreateDocumentView(documentMoniker, physicalView, hierarchy, itemid, textLines!, docDataExisting == IntPtr.Zero, out editorCaption, out commandUIGuid);
                docView = Marshal.GetIUnknownForObject(docViewObject);
            }
            finally
            {
                if (docView == IntPtr.Zero)
                {
                    if (docDataExisting != docData && docData != IntPtr.Zero)
                    {
                        // Cleanup the instance of the docData that we have addref'ed
                        Marshal.Release(docData);
                        docData = IntPtr.Zero;
                    }
                }
            }
            return VSConstants.S_OK;
        }

        IVsTextLines? GetTextBuffer(IntPtr docDataExisting, string filename)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsTextLines? textLines;
            if (docDataExisting == IntPtr.Zero)
            {
                // Create a new IVsTextLines buffer.
                Type textLinesType = typeof(IVsTextLines);
                Guid riid = textLinesType.GUID;
                Guid clsid = typeof(VsTextBufferClass).GUID;
                textLines = package.CreateInstance(ref clsid, ref riid, textLinesType) as IVsTextLines;

                // set the buffer's site
                if (textLines is IObjectWithSite ows)
                    ows.SetSite(serviceProvider?.GetService(typeof(IOleServiceProvider)));
            }
            else
            {
                // Use the existing text buffer
                object dataObject = Marshal.GetObjectForIUnknown(docDataExisting);
                textLines = dataObject as IVsTextLines;
                if (textLines == null)
                {
                    // Try get the text buffer from textbuffer provider
                    if (dataObject is IVsTextBufferProvider tbp)
                        tbp.GetTextBuffer(out textLines);
                }
                if (textLines == null) // Unknown docData type then, so we have to force VS to close the other editor.
                    throw Marshal.GetExceptionForHR(VSConstants.VS_E_INCOMPATIBLEDOCDATA);

            }
            return textLines;
        }

        object CreateDocumentView(string documentMoniker, string physicalView, IVsHierarchy hierarchy, uint itemid, IVsTextLines textLines, bool createdDocData, out string editorCaption, out Guid cmdUI)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //Init out params
            editorCaption = string.Empty;
            cmdUI = Guid.Empty;

            if (string.IsNullOrEmpty(physicalView))
            {
                // create code window as default physical view
                return CreateCodeView(documentMoniker, textLines, createdDocData, ref editorCaption, ref cmdUI);
            }

            // We couldn't create the view
            // Return special error code so VS can try another editor factory.
            throw Marshal.GetExceptionForHR(VSConstants.VS_E_UNSUPPORTEDFORMAT);
        }

        [SuppressMessage("Style", "IDE0060:Nicht verwendete Parameter entfernen", Justification = "infrastructure")]
        IVsCodeWindow CreateCodeView(string documentMoniker, IVsTextLines textLines, bool createdDocData, ref string editorCaption, ref Guid cmdUI)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //Type codeWindowType = typeof(IVsCodeWindow);
            //Guid riid = codeWindowType.GUID;
            //Guid clsid = typeof(VsCodeWindowClass).GUID;
            var compModel = (IComponentModel)new VsServiceProviderWrapper(package).GetService(typeof(SComponentModel));
            var adapterService = compModel.GetService<IVsEditorAdaptersFactoryService>();

            var window = adapterService.CreateVsCodeWindowAdapter((IOleServiceProvider)serviceProvider?.GetService(typeof(IOleServiceProvider))!);
            ErrorHandler.ThrowOnFailure(window.SetBuffer(textLines));
            ErrorHandler.ThrowOnFailure(window.SetBaseEditorCaption(null));
            ErrorHandler.ThrowOnFailure(window.GetEditorCaption(READONLYSTATUS.ROSTATUS_Unknown, out editorCaption));

            //IVsUserData? userData = textLines as IVsUserData;
            //if (userData != null)
            //{
            //    if (PromptEncodingOnLoad)
            //    {
            //        var guid = VSConstants.VsTextBufferUserDataGuid.VsBufferEncodingPromptOnLoad_guid;
            //        userData.SetData(ref guid, (uint)1);
            //    }
            //}

            cmdUI = VSConstants.GUID_TextEditorFactory;

            var componentModel = (IComponentModel)new VsServiceProviderWrapper(package).GetService(typeof(SComponentModel));
            var bufferEventListener = new TextBufferEventListener(componentModel, textLines, Defines.LanguageServiceGuid);
            if (!createdDocData)
            {
                // we have a pre-created buffer, go ahead and initialize now as the buffer already
                // exists and is initialized
                bufferEventListener.OnLoadCompleted(0);
            }

            return window;
        }

        sealed class TextBufferEventListener : IVsTextBufferDataEvents
        {
            readonly IComponentModel componentModel;
            readonly IVsTextLines textLines;
            readonly IConnectionPoint? connectionPoint;
            readonly uint cookie;

            Guid languageServiceId;

            internal TextBufferEventListener(IComponentModel componentModel, IVsTextLines textLines, Guid languageServiceId)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                this.componentModel = componentModel;
                this.textLines = textLines;
                this.languageServiceId = languageServiceId;

                var connectionPointContainer = textLines as IConnectionPointContainer;
                var bufferEventsGuid = typeof(IVsTextBufferDataEvents).GUID;
                connectionPointContainer?.FindConnectionPoint(ref bufferEventsGuid, out connectionPoint);
                connectionPoint?.Advise(this, out cookie);
            }

            public void OnFileChanged(uint grfChange, uint dwFileAttrs)
            {
            }

            public int OnLoadCompleted(int fReload)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                connectionPoint?.Unadvise(cookie);
                textLines.SetLanguageServiceID(ref languageServiceId);

                return VSConstants.S_OK;
            }
        }
    }
}