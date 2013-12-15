using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SLaks.Rebracer.Services {
	///<summary>Handles Visual Studio events to save and load settings files when appropriate.</summary>
	[Export]
	class SolutionListener {
		private readonly DTE dte;
		private readonly SettingsLocator locator;
		private readonly ILogger logger;
		private readonly SettingsPersister persister;

		[ImportingConstructor]
		public SolutionListener(SVsServiceProvider sp, ILogger logger, SettingsPersister persister, SettingsLocator locator) {
			this.logger = logger;
			this.locator = locator;
			this.persister = persister;

			dte = (DTE)sp.GetService(typeof(DTE));

			dte.Events.DTEEvents.OnStartupComplete += DTEEvents_OnStartupComplete;
			dte.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
			dte.Events.SolutionEvents.Opened += SolutionEvents_Opened;
		}

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
	}
}
