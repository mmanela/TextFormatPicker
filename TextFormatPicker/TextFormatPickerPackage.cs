using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = EnvDTE.Constants;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using SelectionContainer = Microsoft.VisualStudio.Shell.SelectionContainer;

namespace MattManela.TextFormatPicker
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [Guid(GuidList.guidTextFormatPickerPkgString)]
    public sealed class TextFormatPickerPackage : Package, IVsSelectionEvents
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public TextFormatPickerPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        private TextInformationManager textInformationManager;

        private uint cookieForSelection;
        private SelectionContainer selContainer;
        private IVsWindowFrame activeWindowFrame;

        private ITrackSelection TrackSelection
        {
            get
            {
                if (activeWindowFrame != null)
                {
                    object frameSP = null;
                    activeWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_SPFrame, out frameSP);
                    var sp = new ServiceProvider((IOleServiceProvider)frameSP);
                    //get the trackselection service and return its interface
                    return (ITrackSelection)sp.GetService(typeof(STrackSelection));
                }
                return null;
            }
        }

        private IComponentModel componentModel;

        public IComponentModel ComponentModel
        {
            get
            {
                if (componentModel == null)
                    componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
                return componentModel;
            }
        }

        private DTE2 dte;

        public DTE2 Dte
        {
            get
            {
                if (dte == null)
                    dte = (DTE2)GetService(typeof(DTE));
                return dte;
            }
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidTextFormatPickerCmdSet, (int)PkgCmdIDList.cmdidGetTextFormat);
                MenuCommand menuItem = new MenuCommand(ShowTextFormat, menuCommandID);
                mcs.AddCommand(menuItem);
            }


            //Set up Selection Events so that I can tell when a new window in VS has become active.

            IVsMonitorSelection selMonitor = GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            if (selMonitor != null)
                selMonitor.AdviseSelectionEvents(this, out cookieForSelection);

            textInformationManager = new TextInformationManager(this, ComponentModel);
            textInformationManager.RefreshSelection += UpdateSelection;
        }

        public void UpdateSelection(object sender, EventArgs eventArgs)
        {
            RefreshPropertiesWindow();
            ShowPropertiesWindow();
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowTextFormat(object sender, EventArgs e)
        {
            try
            {
            }
            catch (ArgumentException)
            {
                throw;
            }
        }


        private void RefreshPropertiesWindow()
        {
            if (activeWindowFrame != null)
            {
                SetSelectionContainer();
                TrackSelection.OnSelectChange(selContainer);
            }
        }

        private void SetSelectionContainer()
        {
            // Create an ArrayList to store the objects that can be selected
            ArrayList listObjects = new ArrayList();


            //Add a custom type provider to filter the properties
            TypeDescriptor.AddProvider(new SelectedTextSymbolTypeDescriptorProvider(typeof(SelectedTextSymbol)), typeof(SelectedTextSymbol));

            // Create the object that will show the document's properties
            // on the properties window.
            listObjects.Add(textInformationManager.SelectedSymbol);

            // Create the SelectionContainer object.
            selContainer = new SelectionContainer(true, false);
            selContainer.SelectableObjects = listObjects;
            selContainer.SelectedObjects = listObjects;
        }

        private void ShowPropertiesWindow()
        {
            //show the properties window
            IVsUIShell vsShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
            Guid propWinGuid = new Guid(Constants.vsWindowKindProperties);
            IVsWindowFrame propFrame = null;
            vsShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref propWinGuid, out propFrame);
            if (propFrame != null)
            {
                propFrame.Show();
            }
        }


        private void WriteLineToPane(OutputWindowPane pane, string text)
        {
            pane.OutputString(text + Environment.NewLine);
        }

        private OutputWindowPane CreatePane(string title)
        {
            OutputWindowPanes panes = Dte.ToolWindows.OutputWindow.OutputWindowPanes;

            OutputWindowPane pane;
            try
            {
                // If the pane exists already, return it.
                pane = panes.Item(title);
            }
            catch (ArgumentException)
            {
                // Create a new pane.
                pane = panes.Add(title);
            }

            pane.Activate();
            return pane;
        }

        private void ShowMessageBox(string title, string body)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                0,
                ref clsid,
                title,
                body,
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0, // false
                out result));
        }

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        //not used - needed to satify IVsSelectionEvents interface.  Only OnCmdUIContextChanged used
        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            try
            {
                if (elementid == (uint)VSConstants.VSSELELEMID.SEID_WindowFrame)
                {
                    object prop = null;
                    var frame = (IVsWindowFrame)varValueNew;
                    frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out prop);
                    if (prop != null)
                    {
                        activeWindowFrame = frame;
                        textInformationManager.ManageActiveView((string)prop, VsShellUtilities.GetTextView(activeWindowFrame));
                    }
                }
                return VSConstants.S_OK;
            }
            catch (NullReferenceException)
            {
                return VSConstants.S_FALSE;
            }
        }

        //not used - needed to satify IVsSelectionEvents interface.  Only OnCmdUIContextChanged used
        public int OnSelectionChanged(IVsHierarchy pHierOld,
                                      uint itemidOld,
                                      IVsMultiItemSelect pMISOld,
                                      ISelectionContainer pSCOld,
                                      IVsHierarchy pHierNew,
                                      uint itemidNew,
                                      IVsMultiItemSelect pMISNew,
                                      ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }
    }
}