﻿//------------------------------------------------------------------------------
// <copyright file="Command.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Xsd2Code.Library;
using Xsd2Code.Addin;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using Xsd2Code.Library.Helpers;

namespace Xsd2Code.Vsix
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("76fdcbd3-880d-414c-a6dd-76db7c0d485c");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private Command(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID); // OleMenuCommand => can use BeforeQueryStatus
                menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                // start by assuming that the menu will not be shown
                menuCommand.Visible = false;
                menuCommand.Enabled = false;

                IVsHierarchy hierarchy = null;
                uint itemid = VSConstants.VSITEMID_NIL;

                if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
                // Get the file path
                string itemFullPath = null;
                ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
                var transformFileInfo = new FileInfo(itemFullPath);

                // then check if the file is named '*.xsd'
                bool isWebConfig = string.Compare(".xsd", transformFileInfo.Extension, StringComparison.OrdinalIgnoreCase) == 0;

                // if not leave the menu hidden
                if (!isWebConfig) return;

                menuCommand.Visible = true;
                menuCommand.Enabled = true;
            }
        }
        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }

                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new Command(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            if (dte == null)
                return;

            ProjectItem proitem = dte.SelectedItems.Item(1).ProjectItem;
            Project proj = proitem.ContainingProject;

            // Try to get default nameSpace
            string defaultNamespace = string.Empty;
            uint? targetFramework = 0;
            bool? isSilverlightApp = false;
            try
            {
                defaultNamespace = proj.Properties.Item("DefaultNamespace").Value as string;
                targetFramework = proj.Properties.Item("TargetFramework").Value as uint?;
                isSilverlightApp = proj.Properties.Item("SilverlightProject.IsSilverlightApplication").Value as bool?;
            }
            catch
            {
            }

            CodeModel codeModel = proitem.ContainingProject.CodeModel;
            string fileName = proitem.FileNames[0];
            try
            {
                proitem.Save(fileName);
            }
            catch (Exception)
            {
            }

            TargetFramework framework = TargetFramework.Net20;
            if (targetFramework.HasValue)
            {
                uint target = targetFramework.Value;
                switch (target)
                {
                    case 196608:
                        framework = TargetFramework.Net30;
                        break;
                    case 196613:
                        framework = TargetFramework.Net35;
                        break;
                    case 262144:
                        framework = TargetFramework.Net40;
                        break;
                }
            }
            if (isSilverlightApp.HasValue)
            {
                if (isSilverlightApp.Value)
                {
                    framework = TargetFramework.Silverlight;
                }
            }

            var frm = new FormOption();
            frm.Init(fileName, proj.CodeModel.Language, defaultNamespace, framework);

            DialogResult result = frm.ShowDialog();

            GeneratorParams generatorParams = frm.GeneratorParams.Clone();
            generatorParams.InputFilePath = fileName;

            var gen = new GeneratorFacade(generatorParams);

            // Close file if open in IDE
            ProjectItem projElmts;
            bool found = FindInProject(proj.ProjectItems, gen.GeneratorParams.OutputFilePath, out projElmts);
            if (found)
            {
                Window window = projElmts.Open(EnvDTE.Constants.vsViewKindCode);
                window.Close(vsSaveChanges.vsSaveChangesNo);
            }

            if (fileName.Length > 0)
            {
                if (result == DialogResult.OK)
                {
                    Result<string> generateResult = gen.Generate();
                    string outputFileName = generateResult.Entity;

                    if (!generateResult.Success)
                        MessageBox.Show(generateResult.Messages.ToString(), "XSD2Code", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        if (!found)
                        {
                            projElmts = proitem.Collection.AddFromFile(outputFileName);
                        }

                        if (frm.OpenAfterGeneration)
                        {
                            Window window = projElmts.Open(EnvDTE.Constants.vsViewKindCode);
                            window.Activate();
                            window.SetFocus();

                            try
                            {
                                // this.applicationObjectField.DTE.ExecuteCommand("Edit.RemoveAndSort", "");
                                dte.ExecuteCommand("Edit.FormatDocument", string.Empty);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recursive search in project solution.
        /// </summary>
        /// <param name="projectItems">
        /// Root projectItems
        /// </param>
        /// <param name="filename">
        /// Full path of search element
        /// </param>
        /// <param name="item">
        /// output ProjectItem
        /// </param>
        /// <returns>
        /// true if found else false
        /// </returns>
        private bool FindInProject(ProjectItems projectItems, string filename, out ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            item = null;
            if (projectItems == null)
                return false;

            foreach (ProjectItem projElmts in projectItems)
            {
                if (projElmts.get_FileNames(0) == filename)
                {
                    item = projElmts;
                    return true;
                }

                if (FindInProject(projElmts.ProjectItems, filename, out item))
                    return true;
            }

            return false;
        }
    }
}
