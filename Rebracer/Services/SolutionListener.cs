using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace SLaks.Rebracer.Services {
	///<summary>Handles Visual Studio events to save and load settings files when appropriate.</summary>
	[Export]
	class SolutionListener {
		private readonly DTE dte;
		private readonly SettingsLocator locator;
		private readonly ILogger logger;
		private readonly SettingsPersister persister;

		static readonly VSConstants.VSStd97CmdID[] optionsCommands =
		{
			VSConstants.VSStd97CmdID.ToolsOptions,
			VSConstants.VSStd97CmdID.DebugOptions,
			VSConstants.VSStd97CmdID.CustomizeKeyboard
		};

		[ImportingConstructor]
		public SolutionListener(SVsServiceProvider sp, ILogger logger, SettingsPersister persister, SettingsLocator locator) {
			this.logger = logger;
			this.locator = locator;
			this.persister = persister;

			dte = (DTE)sp.GetService(typeof(DTE));

			dte.Events.DTEEvents.OnStartupComplete += DTEEvents_OnStartupComplete;
			dte.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
			dte.Events.SolutionEvents.Opened += SolutionEvents_Opened;

			foreach (var optionCmdId in optionsCommands) {
				dte.Events.CommandEvents[VSConstants.CMDSETID.StandardCommandSet97_string, (int)optionCmdId].AfterExecute += ToolsOptionsCommand_AfterExecute;
			}
			dte.Events.CommandEvents[VSConstants.CMDSETID.StandardCommandSet97_string, (int)VSConstants.VSStd97CmdID.SaveSolution].AfterExecute += SaveAllCommand_AfterExecute;

			dte.Events.DTEEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
			dte.Events.SolutionEvents.BeforeClosing += SolutionEvents_BeforeClosing;

			var pct = (IVsRegisterPriorityCommandTarget)sp.GetService(typeof(SVsRegisterPriorityCommandTarget));

			// We never unregister, so we don't need the cookie.
			uint ignored;
			var optionsCommandTarget = new OptionsCommandTarget(sp);
			ErrorHandler.ThrowOnFailure(pct.RegisterPriorityCommandTarget(0, optionsCommandTarget, out ignored));
			optionsCommandTarget.OptionsDialogClosed += OptionsCommandTarget_OptionsDialogClosed;
			optionsCommandTarget.BeforeSaveSolution += OptionsCommandTarget_BeforeSaveSolution;
		}


		#region Events to read settings
		private void SolutionEvents_Opened() {
			// When the user opens a solution, activate its
			// settings file, if any.
			persister.ActivateSettingsFile(locator.GetActiveFile(dte.Solution));
		}

		private void SolutionEvents_AfterClosing() {
			// If the user closed a solution, switch back
			// to the global (or new solution's) settings
			persister.ActivateSettingsFile(locator.GetActiveFile(dte.Solution));
		}

		private void DTEEvents_OnStartupComplete() {
			// When VS is launched, wait until we know
			// whether the used opened a solution, and
			// activate the solution or global file.
			persister.ActivateSettingsFile(locator.GetActiveFile(dte.Solution));
		}
		#endregion

		#region Events to save settings
		private void ToolsOptionsCommand_AfterExecute(string Guid, int ID, object CustomIn, object CustomOut) {
			// After the user changes any options, save them.
			persister.SaveSettings();
		}

		private void OptionsCommandTarget_OptionsDialogClosed(object sender, EventArgs e) {
			// After the user changes any options, save them.
			persister.SaveSettings();
		}
		private void DTEEvents_OnBeginShutdown() {
			// In case settings changed without the options dialog, save on exit
			persister.SaveSettings();
		}

		private void SolutionEvents_BeforeClosing() {
			// In case settings changed without the options dialog, save solution settings before closing it
			if (persister.SettingsPath != locator.UserSettingsFile)
				persister.SaveSettings();
		}
		private void SaveAllCommand_AfterExecute(string Guid, int ID, object CustomIn, object CustomOut) {
			// In case settings changed without the options dialog, save solution settings on Save All
			if (persister.SettingsPath != locator.UserSettingsFile)
				persister.SaveSettings();
		}
		private void OptionsCommandTarget_BeforeSaveSolution(object sender, EventArgs e) {
			// In case settings changed without the options dialog, save solution settings on Save All
			if (persister.SettingsPath != locator.UserSettingsFile)
				persister.SaveSettings();
		}
		#endregion

		sealed class OptionsCommandTarget : IOleCommandTarget {
			readonly IOleCommandTarget globalTarget;
			public OptionsCommandTarget(SVsServiceProvider sp) {
				globalTarget = (IOleCommandTarget)sp.GetService(typeof(SUIHostCommandDispatcher));
			}

			// We swallow the options command, then re-invoke it and raise our event
			// after the blocking dialog is finished. This variable prevents us from
			// catching the re-invoked command.
			bool isReentering;

			public int Exec(ref Guid pguidCmdGroup, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint nCmdID, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
				if (pguidCmdGroup != VSConstants.GUID_VSStandardCommandSet97)
					return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;  // Skip filter
				if (nCmdID == (uint)VSConstants.VSStd97CmdID.SaveSolution) {
					OnBeforeSaveSolution();
					return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;  // Skip filter
				}

				if (!optionsCommands.Contains((VSConstants.VSStd97CmdID)nCmdID))
					return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;  // Skip filter

				if (isReentering) {
					isReentering = false;
					return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;  // Skip filter
				}

				isReentering = true;
				var retVal = globalTarget.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				OnOptionsDialogClosed();
				return retVal;
			}

			public int QueryStatus(ref Guid pguidCmdGroup, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint cCmds, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.OLECMD")]OLECMD[] prgCmds, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.OLECMDTEXT")]IntPtr pCmdText) {
				return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;  // Skip filter
			}

			///<summary>Occurs when the options dialog is shown.</summary>
			public event EventHandler OptionsDialogClosed;
			///<summary>Raises the OptionsDialogClosed event.</summary>
			void OnOptionsDialogClosed() { OnOptionsDialogClosed(EventArgs.Empty); }
			///<summary>Raises the OptionsDialogClosed event.</summary>
			///<param name="e">An EventArgs object that provides the event data.</param>
			void OnOptionsDialogClosed(EventArgs e) {
				if (OptionsDialogClosed != null)
					OptionsDialogClosed(this, e);
			}
			///<summary>Occurs when the SaveSolution command is executed.</summary>
			public event EventHandler BeforeSaveSolution;
			///<summary>Raises the BeforeSaveSolution event.</summary>
			void OnBeforeSaveSolution() { OnBeforeSaveSolution(EventArgs.Empty); }
			///<summary>Raises the BeforeSaveSolution event.</summary>
			///<param name="e">An EventArgs object that provides the event data.</param>
			void OnBeforeSaveSolution(EventArgs e) {
				if (BeforeSaveSolution != null)
					BeforeSaveSolution(this, e);
			}
		}
	}
}
