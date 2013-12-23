using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SLaks.Rebracer.Services {
	[Export(typeof(IAutoActivatingService))]
	class SettingsFileUserNotifier : IAutoActivatingService {
		private readonly DTE dte;
		private readonly SettingsLocator locator;
		private readonly SettingsPersister persister;
		private readonly INotificationService notifier;

		[ImportingConstructor]
		public SettingsFileUserNotifier(SVsServiceProvider sp, INotificationService notifier, SettingsPersister persister, SettingsLocator locator) {
			this.locator = locator;
			this.notifier = notifier;
			this.persister = persister;
			dte = (DTE)sp.GetService(typeof(DTE));
		}

		private string LocationDisplayName {
			get
			{
				if (persister.SettingsPath == locator.UserSettingsFile)
					return "global settings file";
				else if (persister.SettingsPath == locator.SolutionPath(dte.Solution))
					return "settings file for " + Path.GetFileName(dte.Solution.FileName);
				else    // Should never happen
					return "settings file in " + Path.GetDirectoryName(persister.SettingsPath);
			}
		}

		public void Activate() {
			persister.SettingsSaved += Persister_SettingsSaved;
			persister.SettingsLoaded += Persister_SettingsLoaded;
			persister.SettingsFileCreated += Persister_SettingsFileCreated;
		}

		private void Persister_SettingsFileCreated(object sender, EventArgs e) {
			notifier.ShowNotification("Rebracer Settings File Created", "Created new " + LocationDisplayName + ", initialized from current settings");
		}

		private void Persister_SettingsLoaded(object sender, SettingsFileLoadedEventArgs e) {
			// Don't notify when applying global settings at launch
			if (string.IsNullOrEmpty(e.OldPath) && e.NewPath == locator.UserSettingsFile)
				return;
			notifier.ShowNotification("Rebracer Settings Loaded", "Applied " + LocationDisplayName);
		}

		private void Persister_SettingsSaved(object sender, EventArgs e) {
			notifier.ShowNotification("Rebracer Settings Saved", "Saved changes to " + LocationDisplayName);
		}
	}
}
